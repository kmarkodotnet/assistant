namespace FamilyOs.Workers.Services;

/// <summary>
/// Configuration for <see cref="DailyDigestJob"/> (contract §8).
/// Bound from the "DailyDigest" appsettings section; overridable via
/// <c>DailyDigest__*</c> env vars per the project convention.
/// </summary>
public sealed class DailyDigestOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>Daily digest time as a local "HH:mm" string.</summary>
    public string RunAtLocal { get; set; } = "07:00";

    /// <summary>
    /// Forward-looking only (v2 timezone-correct handling); the MVP follows the
    /// existing server-time/UTC convention (contract §7) and does not convert.
    /// </summary>
    public string TimeZone { get; set; } = "Europe/Budapest";

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(15);

    public int DeadlineLookaheadDays { get; set; } = 7;

    public int DocumentLookbackHours { get; set; } = 24;
}
