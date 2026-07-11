using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FluentAssertions;

namespace FamilyOs.Domain.Tests.Ai;

/// <summary>
/// Unit tests for EmailMessage.SetImportance (docs/contracts/classify-email-contract.md §3).
/// Verifies the concurrency contract from §8/ADR-0012: SetImportance touches only Importance,
/// Category, HasDeadlineHint and UpdatedUtc — it must never modify IngestStatus/ProcessedUtc,
/// which is ExtractText's territory, so the two jobs never clobber each other's columns.
/// </summary>
public sealed class EmailMessageImportanceTests
{
    private static EmailMessage CreateEmail()
        => EmailMessage.Create(
            sourceId: Guid.NewGuid(),
            gmailMessageId: "msg-1",
            fromAddress: "sender@example.com",
            toAddresses: "me@example.com",
            subject: "Test subject",
            receivedUtc: DateTime.UtcNow,
            hasAttachments: false);

    [Fact]
    public void SetImportance_SetsAllThreeFields()
    {
        var email = CreateEmail();

        email.SetImportance(EmailImportance.High, "hivatalos", true);

        email.Importance.Should().Be(EmailImportance.High);
        email.Category.Should().Be("hivatalos");
        email.HasDeadlineHint.Should().BeTrue();
    }

    [Fact]
    public void SetImportance_UpdatesUpdatedUtc()
    {
        var email = CreateEmail();
        var before = email.UpdatedUtc;

        Thread.Sleep(5);
        email.SetImportance(EmailImportance.Low, null, false);

        email.UpdatedUtc.Should().BeAfter(before);
    }

    [Fact]
    public void SetImportance_DoesNotTouchIngestStatusOrProcessedUtc()
    {
        var email = CreateEmail();
        var statusBefore = email.IngestStatus;
        var processedBefore = email.ProcessedUtc;

        email.SetImportance(EmailImportance.High, "szamla", true);

        // SetImportance must be disjoint from ExtractText's columns (contract §8 concurrency decision) —
        // so the two AiProcessingJobs (ClassifyEmail / ExtractText) never overwrite each other's writes.
        email.IngestStatus.Should().Be(statusBefore);
        email.ProcessedUtc.Should().Be(processedBefore);
    }

    [Fact]
    public void SetImportance_DoesNotTouchOtherFields()
    {
        var email = CreateEmail();
        var subjectBefore = email.Subject;
        var fromBefore = email.FromAddress;
        var bodyBefore = email.BodyText;
        var createdBefore = email.CreatedUtc;

        email.SetImportance(EmailImportance.Medium, "egyeb", false);

        email.Subject.Should().Be(subjectBefore);
        email.FromAddress.Should().Be(fromBefore);
        email.BodyText.Should().Be(bodyBefore);
        email.CreatedUtc.Should().Be(createdBefore);
    }

    [Fact]
    public void SetImportance_CalledTwice_OverwritesPreviousValue_Idempotently()
    {
        var email = CreateEmail();

        email.SetImportance(EmailImportance.High, "hivatalos", true);
        email.SetImportance(EmailImportance.Low, null, false);

        email.Importance.Should().Be(EmailImportance.Low);
        email.Category.Should().BeNull();
        email.HasDeadlineHint.Should().BeFalse();
    }

    [Fact]
    public void NewEmail_ImportanceFieldsAreNullByDefault()
    {
        var email = CreateEmail();

        email.Importance.Should().BeNull();
        email.Category.Should().BeNull();
        email.HasDeadlineHint.Should().BeNull();
    }
}
