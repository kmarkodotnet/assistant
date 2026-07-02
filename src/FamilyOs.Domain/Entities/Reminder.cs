using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Entities;

public sealed class Reminder
{
    private Reminder() { }

    public Guid Id { get; private set; }
    // XOR: either TaskId or DeadlineId, never both, never neither
    public Guid? TaskId { get; private set; }
    public Guid? DeadlineId { get; private set; }
    public Guid TargetUserAccountId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public ReminderStatus Status { get; private set; }
    public DateTime TriggerUtc { get; private set; }
    public DateTime? FiredUtc { get; private set; }
    public DateTime? AcknowledgedUtc { get; private set; }
    public string? RruleExpression { get; private set; }
    public int EscalationLevel { get; private set; }
    public string? SnoozeNote { get; private set; }
    public Guid CreatedByUserAccountId { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public DateTime? DeletedUtc { get; private set; }

    // Navigations
    public FamilyTask? Task { get; private set; }
    public Deadline? Deadline { get; private set; }

    public static Reminder ForTask(
        Guid taskId,
        Guid targetUserAccountId,
        DateTime triggerUtc,
        NotificationChannel channel,
        Guid createdBy,
        string? rrule = null)
    {
        if (taskId == Guid.Empty) throw new ArgumentException("TaskId required.");
        return new Reminder
        {
            Id = Guid.CreateVersion7(),
            TaskId = taskId,
            DeadlineId = null,
            TargetUserAccountId = targetUserAccountId,
            Channel = channel,
            Status = ReminderStatus.Scheduled,
            TriggerUtc = triggerUtc,
            EscalationLevel = 0,
            CreatedByUserAccountId = createdBy,
            RruleExpression = rrule,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };
    }

    public static Reminder ForDeadline(
        Guid deadlineId,
        Guid targetUserAccountId,
        DateTime triggerUtc,
        NotificationChannel channel,
        Guid createdBy,
        string? rrule = null)
    {
        if (deadlineId == Guid.Empty) throw new ArgumentException("DeadlineId required.");
        return new Reminder
        {
            Id = Guid.CreateVersion7(),
            TaskId = null,
            DeadlineId = deadlineId,
            TargetUserAccountId = targetUserAccountId,
            Channel = channel,
            Status = ReminderStatus.Scheduled,
            TriggerUtc = triggerUtc,
            EscalationLevel = 0,
            CreatedByUserAccountId = createdBy,
            RruleExpression = rrule,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };
    }

    public void Fire()
    {
        Status = ReminderStatus.Fired;
        FiredUtc = DateTime.UtcNow;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void Acknowledge()
    {
        Status = ReminderStatus.Acknowledged;
        AcknowledgedUtc = DateTime.UtcNow;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void Skip()
    {
        Status = ReminderStatus.Skipped;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = ReminderStatus.Cancelled;
        DeletedUtc = DateTime.UtcNow;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void SetEscalationLevel(int level)
    {
        EscalationLevel = level;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void UpdateTrigger(DateTime triggerUtc, NotificationChannel channel)
    {
        TriggerUtc = triggerUtc;
        Channel = channel;
        UpdatedUtc = DateTime.UtcNow;
    }
}
