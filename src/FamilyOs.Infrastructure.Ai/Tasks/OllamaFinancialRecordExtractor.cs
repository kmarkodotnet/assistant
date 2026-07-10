using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Infrastructure.Ai.Prompts;
using FamilyOs.Infrastructure.Ai.Providers;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Infrastructure.Ai.Tasks;

public sealed class OllamaFinancialRecordExtractor : IFinancialRecordExtractor
{
    private readonly IAiProviderFactory _providerFactory;
    private readonly ILogger<OllamaFinancialRecordExtractor> _logger;

    private static readonly Action<ILogger, Exception?> LogExtractDone =
        LoggerMessage.Define(LogLevel.Debug, new EventId(1, nameof(LogExtractDone)),
            "OllamaFinancialRecordExtractor: extraction complete.");

    public OllamaFinancialRecordExtractor(IAiProviderFactory providerFactory, ILogger<OllamaFinancialRecordExtractor> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<FinancialRecordExtraction?> ExtractAsync(string text, CancellationToken ct = default)
    {
        var provider = _providerFactory.GetProvider();
        var template = PromptTemplate.Load(PromptCatalog.ExtractFinancial);
        var truncated = text.Length > 12000 ? text[..12000] : text;

        var userPrompt = PromptTemplate.Replace(template, new Dictionary<string, string>
        {
            ["document"] = truncated,
        });

        var prompt = new AiPrompt(
            SystemPrompt: "You are a helpful assistant that extracts financial information from documents.",
            UserPrompt: userPrompt,
            PromptId: PromptCatalog.ExtractFinancial,
            PromptVersion: PromptCatalog.GetVersion(PromptCatalog.ExtractFinancial));

        var completion = await provider.CompleteAsync(prompt, ct);

        LogExtractDone(_logger, null);
        return ParseResult(completion.Content);
    }

    private static FinancialRecordExtraction? ParseResult(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            decimal? amount = null;
            if (root.TryGetProperty("amount", out var amEl) && amEl.ValueKind == JsonValueKind.Number)
                amount = amEl.GetDecimal();

            var recordType = NullIfLiteralNull(root.TryGetProperty("recordType", out var rt) ? rt.GetString() : null);
            var vendor = NullIfLiteralNull(root.TryGetProperty("vendor", out var v) ? v.GetString() : null);
            var currency = NullIfLiteralNull(root.TryGetProperty("currency", out var cur) ? cur.GetString() : null);
            var recurrencePeriod = NullIfLiteralNull(root.TryGetProperty("recurrencePeriod", out var rp) ? rp.GetString() : null);

            bool? isPaid = root.TryGetProperty("isPaid", out var ip) && ip.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? ip.GetBoolean()
                : null;

            var issueDate = ParseDate(root, "issueDate");
            var dueDate = ParseDate(root, "dueDate");

            return new FinancialRecordExtraction(recordType, vendor, amount, currency, issueDate, dueDate, isPaid, recurrencePeriod);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static DateOnly? ParseDate(JsonElement root, string propertyName)
    {
        var raw = root.TryGetProperty(propertyName, out var el) ? el.GetString() : null;
        if (string.IsNullOrWhiteSpace(raw) || raw == "null")
            return null;

        return DateOnly.TryParseExact(raw, "yyyy-MM-dd", out var parsed) ? parsed : null;
    }

    private static string? NullIfLiteralNull(string? value)
        => value is null or "null" or "" ? null : value;
}
