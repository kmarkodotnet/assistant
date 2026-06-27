using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Tests.Reminders;

public sealed class ReminderXorTests
{
    private static readonly Guid SomeId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime FutureTrigger = DateTime.UtcNow.AddDays(1);

    [Fact]
    public void ForTask_SetsTaskId_DeadlineIdNull()
    {
        var reminder = Reminder.ForTask(SomeId, UserId, FutureTrigger, NotificationChannel.InApp, UserId);

        Assert.Equal(SomeId, reminder.TaskId);
        Assert.Null(reminder.DeadlineId);
        Assert.Equal(ReminderStatus.Scheduled, reminder.Status);
    }

    [Fact]
    public void ForDeadline_SetsDeadlineId_TaskIdNull()
    {
        var reminder = Reminder.ForDeadline(SomeId, UserId, FutureTrigger, NotificationChannel.InApp, UserId);

        Assert.Equal(SomeId, reminder.DeadlineId);
        Assert.Null(reminder.TaskId);
        Assert.Equal(ReminderStatus.Scheduled, reminder.Status);
    }

    [Fact]
    public void ForTask_WithEmptyGuid_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Reminder.ForTask(Guid.Empty, UserId, FutureTrigger, NotificationChannel.InApp, UserId));
    }

    [Fact]
    public void ForDeadline_WithEmptyGuid_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Reminder.ForDeadline(Guid.Empty, UserId, FutureTrigger, NotificationChannel.InApp, UserId));
    }

    [Fact]
    public void Fire_ChangesStatusToFired_SetsFiredUtc()
    {
        var reminder = Reminder.ForTask(SomeId, UserId, FutureTrigger, NotificationChannel.InApp, UserId);

        reminder.Fire();

        Assert.Equal(ReminderStatus.Fired, reminder.Status);
        Assert.NotNull(reminder.FiredUtc);
    }

    [Fact]
    public void Acknowledge_ChangesStatusToAcknowledged_SetsAcknowledgedUtc()
    {
        var reminder = Reminder.ForDeadline(SomeId, UserId, FutureTrigger, NotificationChannel.InApp, UserId);
        reminder.Fire();

        reminder.Acknowledge();

        Assert.Equal(ReminderStatus.Acknowledged, reminder.Status);
        Assert.NotNull(reminder.AcknowledgedUtc);
    }

    [Fact]
    public void Cancel_ChangesStatusToCancelled_SetsDeletedUtc()
    {
        var reminder = Reminder.ForTask(SomeId, UserId, FutureTrigger, NotificationChannel.InApp, UserId);

        reminder.Cancel();

        Assert.Equal(ReminderStatus.Cancelled, reminder.Status);
        Assert.NotNull(reminder.DeletedUtc);
    }

    [Fact]
    public void Skip_ChangesStatusToSkipped()
    {
        var reminder = Reminder.ForDeadline(SomeId, UserId, FutureTrigger, NotificationChannel.InApp, UserId);

        reminder.Skip();

        Assert.Equal(ReminderStatus.Skipped, reminder.Status);
    }

    [Fact]
    public void ForTask_WithRrule_SetsRruleExpression()
    {
        const string rrule = "FREQ=WEEKLY;BYDAY=MO";
        var reminder = Reminder.ForTask(SomeId, UserId, FutureTrigger, NotificationChannel.InApp, UserId, rrule);

        Assert.Equal(rrule, reminder.RruleExpression);
    }
}
