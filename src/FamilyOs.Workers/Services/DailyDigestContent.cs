using FamilyOs.Domain.Enums;

namespace FamilyOs.Workers.Services;

public sealed record DailyDigestReminderItem(DateTime TriggerUtc, string Title);

public sealed record DailyDigestDeadlineItem(DateTime DueDateUtc, string Title, DeadlineCategory Category);

public sealed record DailyDigestDocumentItem(string Title);

/// <summary>
/// Pure, side-effect-free digest content model: emptiness check (contract §5) and
/// the body template (contract §4.4.1). Deliberately independent of EF Core so it
/// is directly unit-testable with plain in-memory lists.
/// </summary>
public sealed class DailyDigestContent
{
    private const int MaxReminderLines = 10;
    private const int MaxDeadlineLines = 10;
    private const int MaxDocumentTitles = 3;

    private readonly int _deadlineLookaheadDays;
    private readonly int _documentLookbackHours;

    public IReadOnlyList<DailyDigestReminderItem> Reminders { get; }
    public IReadOnlyList<DailyDigestDeadlineItem> Deadlines { get; }
    public IReadOnlyList<DailyDigestDocumentItem> Documents { get; }

    public DailyDigestContent(
        IReadOnlyList<DailyDigestReminderItem> reminders,
        IReadOnlyList<DailyDigestDeadlineItem> deadlines,
        IReadOnlyList<DailyDigestDocumentItem> documents,
        int deadlineLookaheadDays = 7,
        int documentLookbackHours = 24)
    {
        Reminders = reminders;
        Deadlines = deadlines;
        Documents = documents;
        _deadlineLookaheadDays = deadlineLookaheadDays;
        _documentLookbackHours = documentLookbackHours;
    }

    /// <summary>Contract §5 / ADR-0011: "nincs mai teendő" → no digest is sent.</summary>
    public bool IsEmpty => Reminders.Count == 0 && Deadlines.Count == 0 && Documents.Count == 0;

    public string BuildBody()
    {
        var lines = new List<string> { "Jó reggelt! Íme a mai áttekintés.", string.Empty };

        if (Reminders.Count > 0)
        {
            lines.Add($"📅 Mai és holnapi emlékeztetők ({Reminders.Count}):");
            foreach (var r in Reminders.Take(MaxReminderLines))
                lines.Add($"- {r.TriggerUtc:HH:mm} · {r.Title}");
            if (Reminders.Count > MaxReminderLines)
                lines.Add($"… és további {Reminders.Count - MaxReminderLines} tétel");
            lines.Add(string.Empty);
        }

        if (Deadlines.Count > 0)
        {
            lines.Add($"⏳ Közelgő határidők ({_deadlineLookaheadDays} nap, {Deadlines.Count}):");
            foreach (var d in Deadlines.Take(MaxDeadlineLines))
                lines.Add($"- {d.DueDateUtc:yyyy. MM. dd.} · {d.Title} ({d.Category})");
            if (Deadlines.Count > MaxDeadlineLines)
                lines.Add($"… és további {Deadlines.Count - MaxDeadlineLines} tétel");
            lines.Add(string.Empty);
        }

        if (Documents.Count > 0)
        {
            lines.Add($"📄 Új dokumentumok az elmúlt {_documentLookbackHours} órában: {Documents.Count}");
            foreach (var d in Documents.Take(MaxDocumentTitles))
                lines.Add($"- {d.Title}");
        }

        while (lines.Count > 0 && lines[^1].Length == 0)
            lines.RemoveAt(lines.Count - 1);

        return string.Join('\n', lines);
    }
}
