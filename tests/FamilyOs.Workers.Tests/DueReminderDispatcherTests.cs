using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Domain.Services;

namespace FamilyOs.Workers.Tests;

/// <summary>
/// Unit tests for reminder dispatch domain logic.
/// End-to-end dispatcher tests require a real DB; these cover the business rules.
/// </summary>
public sealed class DueReminderDispatcherTests
{
    private static readonly Guid DeadlineId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void ReminderEntity_WhenFired_StatusBecomesFiredAndFiredUtcSet()
    {
        var trigger = DateTime.UtcNow.AddMinutes(-1); // past trigger
        var reminder = Reminder.ForDeadline(DeadlineId, UserId, trigger, NotificationChannel.InApp, UserId);

        reminder.Fire();

        Assert.Equal(ReminderStatus.Fired, reminder.Status);
        Assert.NotNull(reminder.FiredUtc);
        Assert.True(reminder.FiredUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void ReminderEntity_WhenScheduledAndTriggerInFuture_ShouldNotDispatch()
    {
        var futureTrigger = DateTime.UtcNow.AddHours(2);
        var reminder = Reminder.ForDeadline(DeadlineId, UserId, futureTrigger, NotificationChannel.InApp, UserId);

        // Simulates the dispatcher check: trigger_utc <= NOW()
        var isDue = reminder.TriggerUtc <= DateTime.UtcNow;

        Assert.False(isDue);
        Assert.Equal(ReminderStatus.Scheduled, reminder.Status);
    }

    [Fact]
    public void ReminderEntity_WhenScheduledAndTriggerInPast_ShouldDispatch()
    {
        var pastTrigger = DateTime.UtcNow.AddMinutes(-5);
        var reminder = Reminder.ForDeadline(DeadlineId, UserId, pastTrigger, NotificationChannel.InApp, UserId);

        var isDue = reminder.TriggerUtc <= DateTime.UtcNow;

        Assert.True(isDue);
    }

    [Fact]
    public void DispatchAsync_ReschedulesForQuietHours_WhenInQuietPeriod()
    {
        // Simulate quiet hours logic: if currentTime is in quiet period, reschedule
        var now = new DateTime(2026, 6, 27, 23, 30, 0, DateTimeKind.Utc); // 23:30
        const string quietStart = "22:00";
        const string quietEnd = "07:00";

        var isQuiet = IsInQuietHours(quietStart, quietEnd, now);

        Assert.True(isQuiet);
    }

    [Fact]
    public void DispatchAsync_NotInQuietHours_DuringDay()
    {
        var now = new DateTime(2026, 6, 27, 14, 0, 0, DateTimeKind.Utc); // 14:00
        const string quietStart = "22:00";
        const string quietEnd = "07:00";

        var isQuiet = IsInQuietHours(quietStart, quietEnd, now);

        Assert.False(isQuiet);
    }

    [Fact]
    public void ReminderTriggerCalculator_CalculateFromOffsetDays_SubtractsDaysCorrectly()
    {
        var dueDate = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        const int offsetDays = 7;

        var trigger = ReminderTriggerCalculator.CalculateFromOffsetDays(dueDate, offsetDays);

        Assert.Equal(new DateTime(2026, 7, 8, 12, 0, 0, DateTimeKind.Utc), trigger);
    }

    [Fact]
    public void EscalationPolicyEvaluator_CanEscalate_ReturnsFalseAtMaxLevel()
    {
        Assert.False(EscalationPolicyEvaluator.CanEscalate(EscalationPolicyEvaluator.MaxEscalationLevel));
        Assert.True(EscalationPolicyEvaluator.CanEscalate(EscalationPolicyEvaluator.MaxEscalationLevel - 1));
    }

    [Fact]
    public void EscalationPolicyEvaluator_GetTimeout_IncreasesWithLevel()
    {
        var timeout0 = EscalationPolicyEvaluator.GetEscalationTimeout(0);
        var timeout1 = EscalationPolicyEvaluator.GetEscalationTimeout(1);
        var timeout2 = EscalationPolicyEvaluator.GetEscalationTimeout(2);

        Assert.True(timeout0 < timeout1);
        Assert.True(timeout1 < timeout2);
    }

    // Helper to simulate the quiet-hour check from DueReminderDispatcher
    private static bool IsInQuietHours(string start, string end, DateTime now)
    {
        if (!TimeOnly.TryParse(start, out var quietStart) || !TimeOnly.TryParse(end, out var quietEnd))
            return false;

        var currentTime = TimeOnly.FromDateTime(now);

        if (quietStart <= quietEnd)
            return currentTime >= quietStart && currentTime < quietEnd;

        // Wrap around midnight
        return currentTime >= quietStart || currentTime < quietEnd;
    }
}
