namespace FamilyOs.Application.Admin.AiJobs;

public sealed record AiJobDto(
    Guid Id,
    string JobType,
    string TargetEntityType,
    Guid TargetEntityId,
    string Status,
    int AttemptCount,
    string? ErrorMessage,
    DateTime? StartedUtc,
    DateTime? FinishedUtc,
    DateTime CreatedUtc);
