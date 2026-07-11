using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Abstractions.Notifications;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Workers.Services;

/// <summary>
/// Runs the early, EmailMessage-scoped importance classification (§4 of
/// docs/contracts/classify-email-contract.md). This is a distinct, complementary mechanism to
/// ClassifyJobRunner (Document-scoped, late-pipeline, noise-discard) — see contract §7 / ADR-0012.
/// Never touches IngestStatus/ProcessedUtc (ExtractText's territory) and never creates or deletes
/// a Document.
/// </summary>
public sealed class ClassifyEmailJobRunner
{
    private readonly FamilyOsDbContext _db;
    private readonly IEmailImportanceClassifier _classifier;
    private readonly INotificationService _notifications;
    private readonly ILogger<ClassifyEmailJobRunner> _logger;

    private static readonly Action<ILogger, Guid, Exception?> LogEmailNotFound =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(1, nameof(LogEmailNotFound)),
            "ClassifyEmailJobRunner: EmailMessage {Id} not found — skipping.");

    private static readonly Action<ILogger, Guid, EmailImportance, Exception?> LogClassified =
        LoggerMessage.Define<Guid, EmailImportance>(LogLevel.Information, new EventId(2, nameof(LogClassified)),
            "ClassifyEmailJobRunner: EmailMessage {Id} classified as {Importance}.");

    private static readonly Action<ILogger, Guid, Exception?> LogNoAdminForNotification =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(3, nameof(LogNoAdminForNotification)),
            "ClassifyEmailJobRunner: no active Admin found — skipping High-importance notification for EmailMessage {Id}.");

    public ClassifyEmailJobRunner(
        FamilyOsDbContext db,
        IEmailImportanceClassifier classifier,
        INotificationService notifications,
        ILogger<ClassifyEmailJobRunner> logger)
    {
        _db = db;
        _classifier = classifier;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task RunAsync(AiProcessingJob job, CancellationToken ct)
    {
        var email = await _db.EmailMessages.FirstOrDefaultAsync(e => e.Id == job.TargetId, ct);
        if (email is null)
        {
            LogEmailNotFound(_logger, job.TargetId, null);
            return;
        }

        // Parse-hiba esetén a classifier már Low defaultot ad (kontrakt §2.5) — itt nincs try/catch.
        var result = await _classifier.ClassifyAsync(email.Subject, email.BodyText, ct);

        // Kizárólag a 3 fontosság-oszlopot + UpdatedUtc-t módosítja — nem IngestStatus/ProcessedUtc
        // (ExtractText felségterülete), lásd kontrakt §8 konkurencia-döntés.
        email.SetImportance(result.Importance, result.Category, result.HasDeadlineHint);
        await _db.SaveChangesAsync(ct);

        if (result.Importance == EmailImportance.High)
        {
            await SendImportantEmailNotificationAsync(email, result, ct);
        }

        LogClassified(_logger, email.Id, result.Importance, null);
    }

    private async Task SendImportantEmailNotificationAsync(EmailMessage email, EmailImportanceResult result, CancellationToken ct)
    {
        // Same recipient-resolution pattern as ExtractTextJobRunner.RunForEmailAsync: the oldest
        // active Admin — deterministic, single recipient, no fan-out (ADR-0012 §2).
        var ownerUserId = await _db.UserAccounts
            .Where(u => u.Role == UserRole.Admin && u.DeletedUtc == null)
            .OrderBy(u => u.CreatedUtc)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct);

        if (ownerUserId == Guid.Empty)
        {
            LogNoAdminForNotification(_logger, email.Id, null);
            return;
        }

        var envelope = BuildEnvelope(email, result, ownerUserId);
        await _notifications.SendAsync(envelope, NotificationChannel.InApp, ct);
    }

    public static NotificationEnvelope BuildEnvelope(EmailMessage email, EmailImportanceResult result, Guid ownerUserId)
    {
        var body = $"Feladó: {email.FromAddress}\nTárgy: {email.Subject}"
                   + (result.Category is null ? "" : $"\nKategória: {result.Category}")
                   + (result.HasDeadlineHint ? "\nHatáridőt tartalmazhat." : "");

        return new NotificationEnvelope(
            UserId: ownerUserId,
            Type: "ImportantEmail",
            Title: $"Fontos e-mail: {Truncate(email.Subject, 120)}",
            Body: body,
            ActionUrl: "/documents",
            IdempotencyKey: $"important-email-{email.Id}");
    }

    public static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
