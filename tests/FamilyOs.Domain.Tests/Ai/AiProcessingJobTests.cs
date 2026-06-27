using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FluentAssertions;

namespace FamilyOs.Domain.Tests.Ai;

public sealed class AiProcessingJobTests
{
    [Fact]
    public void Create_SetsQueuedStatusAndDefaultAttempts()
    {
        var targetId = Guid.NewGuid();

        var job = AiProcessingJob.Create(AiJobType.ExtractText, targetId);

        job.Id.Should().NotBe(Guid.Empty);
        job.JobType.Should().Be(AiJobType.ExtractText);
        job.TargetId.Should().Be(targetId);
        job.TargetType.Should().Be(JobTargetType.Document);
        job.Status.Should().Be(JobStatus.Queued);
        job.Attempt.Should().Be(0);
        job.MaxAttempts.Should().Be(5);
        job.NextAttemptUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        job.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MarkRunning_SetsRunningStatus()
    {
        var job = AiProcessingJob.Create(AiJobType.DetectLanguage, Guid.NewGuid());

        job.MarkRunning();

        job.Status.Should().Be(JobStatus.Running);
    }

    [Fact]
    public void MarkDone_SetsDoneStatus()
    {
        var job = AiProcessingJob.Create(AiJobType.Summarize, Guid.NewGuid());
        job.MarkRunning();

        job.MarkDone();

        job.Status.Should().Be(JobStatus.Done);
    }

    [Fact]
    public void MarkFailed_IncrementsAttemptAndSetsErrorMessage()
    {
        var job = AiProcessingJob.Create(AiJobType.Classify, Guid.NewGuid());
        job.MarkRunning();

        job.MarkFailed("Something went wrong");

        job.Status.Should().Be(JobStatus.Failed);
        job.Attempt.Should().Be(1);
        job.ErrorMessage.Should().Be("Something went wrong");
        job.NextAttemptUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void MarkFailed_MultipleAttempts_ExponentialBackoffCapsAt6Hours()
    {
        var job = AiProcessingJob.Create(AiJobType.Embed, Guid.NewGuid());

        // Simulate 10 failures — backoff should cap well before 6h
        for (var i = 0; i < 10; i++)
        {
            job.MarkRunning();
            job.MarkFailed("error");
        }

        // Max backoff is 6 * 3600 seconds = 21600 seconds
        var maxDelay = TimeSpan.FromHours(6).Add(TimeSpan.FromSeconds(10)); // +10s tolerance
        job.NextAttemptUtc.Should().BeBefore(DateTime.UtcNow.Add(maxDelay));
    }

    [Theory]
    [InlineData(AiJobType.ExtractText)]
    [InlineData(AiJobType.DetectLanguage)]
    [InlineData(AiJobType.Summarize)]
    [InlineData(AiJobType.Classify)]
    [InlineData(AiJobType.ExtractDeadlines)]
    [InlineData(AiJobType.ExtractTasks)]
    [InlineData(AiJobType.Embed)]
    public void Create_AllJobTypes_SetCorrectType(AiJobType jobType)
    {
        var job = AiProcessingJob.Create(jobType, Guid.NewGuid());

        job.JobType.Should().Be(jobType);
    }
}
