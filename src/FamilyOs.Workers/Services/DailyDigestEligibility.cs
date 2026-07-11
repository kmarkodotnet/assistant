using FamilyOs.Infrastructure.Notifications;

namespace FamilyOs.Workers.Services;

/// <summary>
/// Pure, DB-independent per-user eligibility check for the daily digest
/// (contract §1.1 step 4): the run time must have passed, the user must not be
/// in quiet hours, and the user must not already have received today's digest.
/// If any condition fails the user is skipped for *this* poll cycle only — the
/// next poll re-evaluates (this is how quiet-hours postponement and idempotency
/// naturally fall out of the loop, per contract §1.2).
/// </summary>
public static class DailyDigestEligibility
{
    public static bool ShouldProcessUser(
        DateTime now,
        string? runAtLocal,
        string? quietHoursStart,
        string? quietHoursEnd,
        bool alreadyHasDigestToday)
    {
        if (alreadyHasDigestToday)
            return false;

        if (QuietHours.IsQuietHour(quietHoursStart, quietHoursEnd, now))
            return false;

        return IsRunTimeReached(runAtLocal, now);
    }

    /// <summary>
    /// "localNow >= RunAtLocal" evaluated in the same time-of-day reference frame
    /// as <see cref="QuietHours.IsQuietHour"/> (contract §7 consistency requirement).
    /// </summary>
    public static bool IsRunTimeReached(string? runAtLocal, DateTime now)
    {
        if (!TimeOnly.TryParse(runAtLocal, out var runAt))
            return true; // misconfigured RunAtLocal must not block the digest indefinitely

        var currentTime = TimeOnly.FromDateTime(now);
        return currentTime >= runAt;
    }
}
