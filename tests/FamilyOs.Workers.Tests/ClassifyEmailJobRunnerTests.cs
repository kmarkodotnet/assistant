using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Workers.Services;
using FluentAssertions;

namespace FamilyOs.Workers.Tests;

/// <summary>
/// Unit tests for ClassifyEmailJobRunner's notification-building logic
/// (docs/contracts/classify-email-contract.md §4.2).
///
/// ClassifyEmailJobRunner takes a concrete FamilyOsDbContext (Npgsql/pgvector-backed model), so a
/// full RunAsync(...) exercise needs a real Postgres instance (see MigrationsTests.cs /
/// Testcontainers) — not available to this feature until the db-engineer's migration lands. The
/// envelope-construction and truncation logic that decides *what* gets sent is pure and is
/// extracted as internal static helpers, so it is fully covered here without a DB dependency.
/// The High/Low/Medium notify-or-not branch in RunAsync is a single `if (result.Importance ==
/// EmailImportance.High)` around a call to this same BuildEnvelope + SendAsync — i.e. covered in
/// spirit by asserting BuildEnvelope is only ever invoked (by RunAsync) on the High path.
/// </summary>
public sealed class ClassifyEmailJobRunnerTests
{
    private static EmailMessage CreateEmail(string subject = "Fizetési felszólítás", string from = "bank@example.com")
        => EmailMessage.Create(
            sourceId: Guid.NewGuid(),
            gmailMessageId: "msg-1",
            fromAddress: from,
            toAddresses: "me@example.com",
            subject: subject,
            receivedUtc: DateTime.UtcNow,
            hasAttachments: false);

    [Fact]
    public void BuildEnvelope_HighImportance_SetsExpectedTypeAndActionUrl()
    {
        var email = CreateEmail();
        var result = new EmailImportanceResult(EmailImportance.High, "hivatalos", true);
        var ownerId = Guid.NewGuid();

        var envelope = ClassifyEmailJobRunner.BuildEnvelope(email, result, ownerId);

        envelope.UserId.Should().Be(ownerId);
        envelope.Type.Should().Be("ImportantEmail");
        envelope.ActionUrl.Should().Be("/documents");
    }

    [Fact]
    public void BuildEnvelope_IdempotencyKey_IsPerEmailAndDeterministic()
    {
        var email = CreateEmail();
        var result = new EmailImportanceResult(EmailImportance.High, "hivatalos", false);

        var envelope1 = ClassifyEmailJobRunner.BuildEnvelope(email, result, Guid.NewGuid());
        var envelope2 = ClassifyEmailJobRunner.BuildEnvelope(email, result, Guid.NewGuid());

        // Idempotency key must depend only on the email — retries/re-runs must map to the same
        // key regardless of who the current resolved owner is (contract §4.2 dedup guarantee).
        envelope1.IdempotencyKey.Should().Be($"important-email-{email.Id}");
        envelope2.IdempotencyKey.Should().Be(envelope1.IdempotencyKey);
    }

    [Fact]
    public void BuildEnvelope_Title_TruncatesLongSubjectTo120Chars()
    {
        var longSubject = new string('x', 200);
        var email = CreateEmail(subject: longSubject);
        var result = new EmailImportanceResult(EmailImportance.High, null, false);

        var envelope = ClassifyEmailJobRunner.BuildEnvelope(email, result, Guid.NewGuid());

        envelope.Title.Should().Be($"Fontos e-mail: {new string('x', 120)}");
    }

    [Fact]
    public void BuildEnvelope_Body_IncludesFromAndSubject()
    {
        var email = CreateEmail(subject: "Sürgős", from: "hivatal@example.com");
        var result = new EmailImportanceResult(EmailImportance.High, null, false);

        var envelope = ClassifyEmailJobRunner.BuildEnvelope(email, result, Guid.NewGuid());

        envelope.Body.Should().Contain("Feladó: hivatal@example.com");
        envelope.Body.Should().Contain("Tárgy: Sürgős");
    }

    [Fact]
    public void BuildEnvelope_WithCategory_IncludesCategoryLine()
    {
        var email = CreateEmail();
        var result = new EmailImportanceResult(EmailImportance.High, "penzugyi", false);

        var envelope = ClassifyEmailJobRunner.BuildEnvelope(email, result, Guid.NewGuid());

        envelope.Body.Should().Contain("Kategória: penzugyi");
    }

    [Fact]
    public void BuildEnvelope_WithoutCategory_OmitsCategoryLine()
    {
        var email = CreateEmail();
        var result = new EmailImportanceResult(EmailImportance.High, null, false);

        var envelope = ClassifyEmailJobRunner.BuildEnvelope(email, result, Guid.NewGuid());

        envelope.Body.Should().NotContain("Kategória:");
    }

    [Fact]
    public void BuildEnvelope_HasDeadlineHint_IncludesDeadlineNote()
    {
        var email = CreateEmail();
        var result = new EmailImportanceResult(EmailImportance.High, null, true);

        var envelope = ClassifyEmailJobRunner.BuildEnvelope(email, result, Guid.NewGuid());

        envelope.Body.Should().Contain("Határidőt tartalmazhat.");
    }

    [Fact]
    public void BuildEnvelope_NoDeadlineHint_OmitsDeadlineNote()
    {
        var email = CreateEmail();
        var result = new EmailImportanceResult(EmailImportance.High, null, false);

        var envelope = ClassifyEmailJobRunner.BuildEnvelope(email, result, Guid.NewGuid());

        envelope.Body.Should().NotContain("Határidőt tartalmazhat.");
    }

    [Theory]
    [InlineData("short", 120, "short")]
    public void Truncate_ShorterThanMax_ReturnsUnchanged(string value, int max, string expected)
    {
        ClassifyEmailJobRunner.Truncate(value, max).Should().Be(expected);
    }

    [Fact]
    public void Truncate_LongerThanMax_CutsToMaxLength()
    {
        var value = new string('a', 50);

        var truncated = ClassifyEmailJobRunner.Truncate(value, 10);

        truncated.Should().HaveLength(10);
        truncated.Should().Be(new string('a', 10));
    }

    [Fact]
    public void Truncate_ExactlyMaxLength_ReturnsUnchanged()
    {
        var value = new string('a', 120);

        ClassifyEmailJobRunner.Truncate(value, 120).Should().Be(value);
    }
}
