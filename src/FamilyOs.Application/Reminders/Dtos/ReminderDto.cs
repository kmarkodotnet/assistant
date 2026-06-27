namespace FamilyOs.Application.Reminders.Dtos;

public sealed class ReminderDto
{
    public Guid Id { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? DeadlineId { get; set; }
    public Guid TargetUserAccountId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime TriggerUtc { get; set; }
    public DateTime? FiredUtc { get; set; }
    public DateTime? AcknowledgedUtc { get; set; }
    public string? RruleExpression { get; set; }
    public int EscalationLevel { get; set; }
    public string? SnoozeNote { get; set; }
    public Guid CreatedByUserAccountId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public sealed class ReminderGroupDto
{
    public List<ReminderDto> Now { get; set; } = [];
    public List<ReminderDto> Week { get; set; } = [];
    public List<ReminderDto> Later { get; set; } = [];
    public List<ReminderDto> Missed { get; set; } = [];
}
