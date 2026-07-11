namespace FamilyOs.Infrastructure.Notifications;

/// <summary>
/// Shared quiet-hours evaluation logic, extracted from <c>DueReminderDispatcher</c>
/// so it can be reused verbatim by <c>DailyDigestJob</c> (contract §7).
/// Convention: <paramref name="start"/>/<paramref name="end"/> are "HH:mm" strings
/// compared directly against server time (<see cref="DateTime.UtcNow"/>) — no
/// timezone conversion is performed (see contract §7 for the rationale).
/// </summary>
public static class QuietHours
{
    public static bool IsQuietHour(string? start, string? end, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end))
            return false;

        if (!TimeOnly.TryParse(start, out var quietStart) || !TimeOnly.TryParse(end, out var quietEnd))
            return false;

        var currentTime = TimeOnly.FromDateTime(now);

        if (quietStart <= quietEnd)
            return currentTime >= quietStart && currentTime < quietEnd;

        // Wrap around midnight
        return currentTime >= quietStart || currentTime < quietEnd;
    }

    public static DateTime GetQuietHoursEnd(string? end, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(end) || !TimeOnly.TryParse(end, out var quietEnd))
            return now.AddHours(8);

        var candidate = now.Date + quietEnd.ToTimeSpan();
        if (candidate <= now)
            candidate = candidate.AddDays(1);

        return candidate;
    }
}
