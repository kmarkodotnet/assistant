using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FamilyOs.Application.Abstractions.Email;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Abstractions;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Ai.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FamilyOs.Infrastructure.Ai.Email;

public sealed partial class GmailIngestionService(
    IFamilyOsDbContext db,
    IAuditLogger auditLogger,
    IHttpClientFactory httpClientFactory,
    IOptions<GmailOptions> gmailOptions,
    ILogger<GmailIngestionService> logger)
    : IEmailIngestionService
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // ----- Structured logging -------------------------------------------------

    [LoggerMessage(Level = LogLevel.Warning, Message = "Source {SourceId} not found for Gmail sync.")]
    private static partial void LogSourceNotFound(ILogger logger, Guid sourceId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting Gmail sync for source {SourceId} ({Name}).")]
    private static partial void LogGmailSyncStarted(ILogger logger, Guid sourceId, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gmail token refresh failed for source {SourceId}: {Status}.")]
    private static partial void LogTokenRefreshFailed(ILogger logger, Guid sourceId, int status);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gmail message list fetch failed for source {SourceId}: {Status}.")]
    private static partial void LogMessageListFailed(ILogger logger, Guid sourceId, int status);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gmail message {MessageId} fetch failed: {Status}. Skipping.")]
    private static partial void LogMessageFetchFailed(ILogger logger, string messageId, int status);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Gmail sync completed for source {SourceId}: fetched={Fetched}, inserted={Inserted}, skipped={Skipped}.")]
    private static partial void LogGmailSyncCompleted(ILogger logger, Guid sourceId, int fetched, int inserted, int skipped);

    // ----- Public API ---------------------------------------------------------

    public async Task<EmailIngestionReport> SyncAsync(Guid sourceId, CancellationToken ct)
    {
        // Track the source so we can persist LastSyncUtc at the end.
        var source = await db.Sources
            .FirstOrDefaultAsync(s => s.Id == sourceId, ct);

        if (source is null)
        {
            LogSourceNotFound(logger, sourceId);
            return new EmailIngestionReport(0, 0, 0, $"Source {sourceId} not found.");
        }

        if (source.Kind != SourceKind.GmailAccount)
        {
            return new EmailIngestionReport(0, 0, 0,
                $"Source {sourceId} is not a Gmail account (Kind={source.Kind}).");
        }

        LogGmailSyncStarted(logger, sourceId, source.Name);

        // 1. Parse refresh token from ConfigJson
        var refreshToken = ParseRefreshToken(source.ConfigJson);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return new EmailIngestionReport(0, 0, 0,
                "No refresh_token in source ConfigJson. Re-connect Gmail account.");
        }

        var opts = gmailOptions.Value;
        if (string.IsNullOrWhiteSpace(opts.ClientId) || string.IsNullOrWhiteSpace(opts.ClientSecret))
        {
            return new EmailIngestionReport(0, 0, 0,
                "Gmail:ClientId / Gmail:ClientSecret not configured.");
        }

        using var http = httpClientFactory.CreateClient("gmail");

        // 2. Exchange refresh token for access token
        var accessToken = await RefreshAccessTokenAsync(http, refreshToken, opts, sourceId, ct);
        if (accessToken is null)
        {
            return new EmailIngestionReport(0, 0, 0, "Failed to obtain Gmail access token.");
        }

        // 3. Load already-known Gmail message IDs to avoid N+1 duplicate checks
        var knownIds = await db.EmailMessages
            .AsNoTracking()
            .Where(m => m.SourceId == sourceId)
            .Select(m => m.GmailMessageId)
            .ToHashSetAsync(ct);

        // 4. Fetch message list (last 50 unread or since last sync)
        var messageIds = await FetchMessageIdsAsync(http, accessToken, source.LastSyncUtc, sourceId, ct);
        if (messageIds is null)
        {
            return new EmailIngestionReport(0, 0, 0, "Failed to retrieve Gmail message list.");
        }

        int fetched = messageIds.Count, inserted = 0, skipped = 0;

        // 5. Process each message
        foreach (var gmailId in messageIds)
        {
            ct.ThrowIfCancellationRequested();

            if (knownIds.Contains(gmailId))
            {
                skipped++;
                continue;
            }

            var raw = await FetchMessageAsync(http, accessToken, gmailId, ct);
            if (raw is null)
            {
                skipped++;
                continue;
            }

            var parsed = ParseMessage(gmailId, sourceId, raw);
            if (parsed is null)
            {
                skipped++;
                continue;
            }

            db.EmailMessages.Add(parsed);

            // Queue AI processing job so Ollama workers can analyse the email
            db.AiProcessingJobs.Add(AiProcessingJob.CreateForEmailMessage(AiJobType.ExtractText, parsed.Id));

            inserted++;
        }

        // Update last sync timestamp and persist together with any new EmailMessage rows.
        source.UpdateLastSync();
        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync(
            AuditAction.ExternalApiCall,
            null,
            "Source",
            sourceId,
            detailsJson: $"{{\"fetched\":{fetched},\"inserted\":{inserted},\"skipped\":{skipped}}}",
            ct: ct);

        var report = new EmailIngestionReport(fetched, inserted, skipped, null);
        LogGmailSyncCompleted(logger, sourceId, report.Fetched, report.Inserted, report.Skipped);
        return report;
    }

    // ----- Helpers ------------------------------------------------------------

    private static string? ParseRefreshToken(string configJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(configJson);
            if (doc.RootElement.TryGetProperty("refresh_token", out var prop))
                return prop.GetString();
        }
        catch (JsonException)
        {
            // malformed JSON — fall through
        }

        return null;
    }

    private async Task<string?> RefreshAccessTokenAsync(
        HttpClient http, string refreshToken, GmailOptions opts, Guid sourceId, CancellationToken ct)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = opts.ClientId,
            ["client_secret"] = opts.ClientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token",
        });

        var resp = await http.PostAsync("https://oauth2.googleapis.com/token", form, ct);

        if (!resp.IsSuccessStatusCode)
        {
            LogTokenRefreshFailed(logger, sourceId, (int)resp.StatusCode);
            return null;
        }

        var token = await resp.Content.ReadFromJsonAsync<TokenResponse>(JsonOpts, ct);
        return token?.AccessToken;
    }

    private async Task<List<string>?> FetchMessageIdsAsync(
        HttpClient http, string accessToken, DateTime? lastSyncUtc, Guid sourceId, CancellationToken ct)
    {
        var query = BuildGmailQuery(lastSyncUtc);
        var url = $"https://gmail.googleapis.com/gmail/v1/users/me/messages" +
                  $"?q={Uri.EscapeDataString(query)}&maxResults=50";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var resp = await http.SendAsync(req, ct);

        if (!resp.IsSuccessStatusCode)
        {
            LogMessageListFailed(logger, sourceId, (int)resp.StatusCode);
            return null;
        }

        var list = await resp.Content.ReadFromJsonAsync<GmailMessageList>(JsonOpts, ct);
        return list?.Messages?.Select(m => m.Id).ToList() ?? [];
    }

    private static string BuildGmailQuery(DateTime? lastSyncUtc)
    {
        if (lastSyncUtc.HasValue)
        {
            // Gmail after: filter uses epoch seconds
            var epoch = (long)(lastSyncUtc.Value - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            return $"after:{epoch}";
        }

        return "is:unread";
    }

    private async Task<GmailMessageDetail?> FetchMessageAsync(
        HttpClient http, string accessToken, string gmailId, CancellationToken ct)
    {
        var url = $"https://gmail.googleapis.com/gmail/v1/users/me/messages/{gmailId}?format=full";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var resp = await http.SendAsync(req, ct);

        if (!resp.IsSuccessStatusCode)
        {
            LogMessageFetchFailed(logger, gmailId, (int)resp.StatusCode);
            return null;
        }

        return await resp.Content.ReadFromJsonAsync<GmailMessageDetail>(JsonOpts, ct);
    }

    private static EmailMessage? ParseMessage(string gmailId, Guid sourceId, GmailMessageDetail raw)
    {
        var headers = raw.Payload?.Headers ?? [];
        var subject = GetHeader(headers, "Subject") ?? "(no subject)";
        var from = GetHeader(headers, "From") ?? string.Empty;
        var to = GetHeader(headers, "To") ?? string.Empty;
        var dateHeader = GetHeader(headers, "Date");

        DateTime receivedUtc = raw.InternalDate.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(raw.InternalDate.Value).UtcDateTime
            : TryParseRfc2822(dateHeader) ?? DateTime.UtcNow;

        var hasAttachments = HasAttachmentParts(raw.Payload);

        var msg = EmailMessage.Create(sourceId, gmailId, from, to, subject, receivedUtc, hasAttachments);

        var (bodyText, bodyHtml) = ExtractBody(raw.Payload);
        var snippet = raw.Snippet;

        msg.SetBody(bodyText, bodyHtml, snippet);

        return msg;
    }

    private static string? GetHeader(List<GmailHeader> headers, string name)
        => headers.FirstOrDefault(h => string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;

    private static bool HasAttachmentParts(GmailMessagePart? part)
    {
        if (part is null) return false;
        if (!string.IsNullOrEmpty(part.Filename)) return true;
        if (part.Parts is { Count: > 0 })
            return part.Parts.Any(HasAttachmentParts);
        return false;
    }

    private static (string? text, string? html) ExtractBody(GmailMessagePart? part)
    {
        if (part is null) return (null, null);

        string? text = null;
        string? html = null;

        CollectBodyParts(part, ref text, ref html);

        // If only HTML available, strip tags to produce plain text
        if (text is null && html is not null)
            text = HtmlToPlainText(html);

        return (text, html);
    }

    private static void CollectBodyParts(GmailMessagePart part, ref string? text, ref string? html)
    {
        var mime = part.MimeType ?? string.Empty;

        if (mime == "text/plain" && part.Body?.Data is { Length: > 0 } textData)
        {
            text ??= DecodeBase64Url(textData);
            return;
        }

        if (mime == "text/html" && part.Body?.Data is { Length: > 0 } htmlData)
        {
            html ??= DecodeBase64Url(htmlData);
            return;
        }

        if (part.Parts is { Count: > 0 })
        {
            foreach (var child in part.Parts)
                CollectBodyParts(child, ref text, ref html);
        }
    }

    private static string DecodeBase64Url(string base64Url)
    {
        // Gmail uses URL-safe base64 without padding
        var padded = base64Url.Replace('-', '+').Replace('_', '/');
        var mod = padded.Length % 4;
        if (mod > 0) padded += new string('=', 4 - mod);
        var bytes = Convert.FromBase64String(padded);
        return Encoding.UTF8.GetString(bytes);
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    private static string HtmlToPlainText(string html)
    {
        // Minimal HTML-to-text: strip tags, decode common entities
        var text = HtmlTagRegex().Replace(html, " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        // Collapse whitespace
        text = Regex.Replace(text, @"\s{2,}", " ").Trim();
        return text;
    }

    private static DateTime? TryParseRfc2822(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr)) return null;
        if (DateTimeOffset.TryParse(dateStr,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var dto))
            return dto.UtcDateTime;
        return null;
    }

    // ----- Gmail API DTOs (internal) ------------------------------------------

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken);

    private sealed record GmailMessageList(
        [property: JsonPropertyName("messages")] List<GmailMessageRef>? Messages);

    private sealed record GmailMessageRef(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("threadId")] string ThreadId);

    private sealed record GmailMessageDetail(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("threadId")] string? ThreadId,
        [property: JsonPropertyName("snippet")] string? Snippet,
        [property: JsonPropertyName("internalDate")] long? InternalDate,
        [property: JsonPropertyName("payload")] GmailMessagePart? Payload);

    private sealed record GmailMessagePart(
        [property: JsonPropertyName("mimeType")] string? MimeType,
        [property: JsonPropertyName("filename")] string? Filename,
        [property: JsonPropertyName("headers")] List<GmailHeader>? Headers,
        [property: JsonPropertyName("body")] GmailPartBody? Body,
        [property: JsonPropertyName("parts")] List<GmailMessagePart>? Parts);

    private sealed record GmailHeader(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("value")] string Value);

    private sealed record GmailPartBody(
        [property: JsonPropertyName("data")] string? Data,
        [property: JsonPropertyName("size")] int Size);
}
