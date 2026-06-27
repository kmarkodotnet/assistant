using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Entities;

public sealed class AiProcessingJob
{
    private AiProcessingJob() { }

    public Guid Id { get; private set; }
    public AiJobType JobType { get; private set; }
    public JobTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public JobStatus Status { get; private set; }
    public int Attempt { get; private set; }
    public int MaxAttempts { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime NextAttemptUtc { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static AiProcessingJob Create(AiJobType jobType, Guid targetId) => new()
    {
        Id = Guid.NewGuid(),
        JobType = jobType,
        TargetType = JobTargetType.Document,
        TargetId = targetId,
        Status = JobStatus.Queued,
        Attempt = 0,
        MaxAttempts = 5,
        NextAttemptUtc = DateTime.UtcNow,
        CreatedUtc = DateTime.UtcNow,
        UpdatedUtc = DateTime.UtcNow,
    };

    public static AiProcessingJob CreateForNote(AiJobType jobType, Guid noteId) => new()
    {
        Id = Guid.NewGuid(),
        JobType = jobType,
        TargetType = JobTargetType.Note,
        TargetId = noteId,
        Status = JobStatus.Queued,
        Attempt = 0,
        MaxAttempts = 5,
        NextAttemptUtc = DateTime.UtcNow,
        CreatedUtc = DateTime.UtcNow,
        UpdatedUtc = DateTime.UtcNow,
    };

    public void MarkRunning()
    {
        Status = JobStatus.Running;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void MarkDone()
    {
        Status = JobStatus.Done;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = JobStatus.Failed;
        Attempt++;
        ErrorMessage = error;
        var delaySeconds = Math.Min(60 * Math.Pow(2, Attempt), 6 * 3600);
        NextAttemptUtc = DateTime.UtcNow.AddSeconds(delaySeconds);
        UpdatedUtc = DateTime.UtcNow;
    }
}
