using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Workers.Services;

namespace FamilyOs.Workers.Tests;

/// <summary>
/// Unit tests for the daily-digest business rules (contract:
/// docs/contracts/daily-digest-contract.md). The job's DB orchestration
/// (DailyDigestJob.RunOnceAsync/ProcessUserAsync) requires EF Core against a real
/// database and is intentionally kept thin; everything with meaningful branching
/// (RBAC filtering, eligibility/idempotency/quiet-hours decision, empty-digest
/// detection, body template) is extracted into pure, DB-independent types so it
/// can be tested directly here — mirrors the DueReminderDispatcherTests approach.
/// </summary>
public sealed class DailyDigestJobTests
{
    // ---------------------------------------------------------------
    // §5 / ADR-0011 — empty digest -> no send
    // ---------------------------------------------------------------

    [Fact]
    public void DailyDigestContent_WhenAllSectionsEmpty_IsEmptyIsTrue()
    {
        var content = new DailyDigestContent([], [], []);

        Assert.True(content.IsEmpty);
    }

    [Fact]
    public void DailyDigestContent_WhenAnySectionHasItems_IsEmptyIsFalse()
    {
        var content = new DailyDigestContent(
            reminders: [new DailyDigestReminderItem(DateTime.UtcNow, "Emlékeztető")],
            deadlines: [],
            documents: []);

        Assert.False(content.IsEmpty);
    }

    [Fact]
    public void BuildBody_OmitsSectionsWithNoItems_IncludingHeader()
    {
        var content = new DailyDigestContent(
            reminders: [],
            deadlines: [new DailyDigestDeadlineItem(DateTime.UtcNow.AddDays(2), "Biztosítás megújítás", DeadlineCategory.Insurance)],
            documents: []);

        var body = content.BuildBody();

        Assert.DoesNotContain("emlékeztető", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Közelgő határidők", body);
        Assert.Contains("Biztosítás megújítás", body);
    }

    [Fact]
    public void BuildBody_WithAllSections_ListsEachSectionWithCount()
    {
        var content = new DailyDigestContent(
            reminders: [new DailyDigestReminderItem(new DateTime(2026, 7, 11, 9, 30, 0, DateTimeKind.Utc), "Fogorvos")],
            deadlines: [new DailyDigestDeadlineItem(new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc), "Vizsga", DeadlineCategory.Inspection)],
            documents: [new DailyDigestDocumentItem("Számla.pdf")],
            deadlineLookaheadDays: 7,
            documentLookbackHours: 24);

        var body = content.BuildBody();

        Assert.Contains("Mai és holnapi emlékeztetők (1):", body);
        Assert.Contains("09:30", body);
        Assert.Contains("Fogorvos", body);
        Assert.Contains("Közelgő határidők (7 nap, 1):", body);
        Assert.Contains("Vizsga", body);
        Assert.Contains("Új dokumentumok az elmúlt 24 órában: 1", body);
        Assert.Contains("Számla.pdf", body);
    }

    // ---------------------------------------------------------------
    // §6 idempotencia + §1.1 quiet-hours postponement
    // ---------------------------------------------------------------

    [Fact]
    public void ShouldProcessUser_WhenAlreadyHasDigestToday_ReturnsFalse()
    {
        // Simulates the job running twice for the same user/day: the second run
        // must not build/send a digest again.
        var now = new DateTime(2026, 7, 11, 8, 0, 0, DateTimeKind.Utc); // past 07:00 run time

        var result = DailyDigestEligibility.ShouldProcessUser(
            now, runAtLocal: "07:00", quietHoursStart: null, quietHoursEnd: null, alreadyHasDigestToday: true);

        Assert.False(result);
    }

    [Fact]
    public void ShouldProcessUser_WhenInQuietHours_ReturnsFalse_SoTheUserIsPostponed()
    {
        var now = new DateTime(2026, 7, 11, 23, 0, 0, DateTimeKind.Utc); // 23:00, quiet 22:00-07:00

        var result = DailyDigestEligibility.ShouldProcessUser(
            now, runAtLocal: "07:00", quietHoursStart: "22:00", quietHoursEnd: "07:00", alreadyHasDigestToday: false);

        Assert.False(result);
    }

    [Fact]
    public void ShouldProcessUser_BeforeRunAtLocal_ReturnsFalse()
    {
        var now = new DateTime(2026, 7, 11, 6, 0, 0, DateTimeKind.Utc); // before 07:00

        var result = DailyDigestEligibility.ShouldProcessUser(
            now, runAtLocal: "07:00", quietHoursStart: null, quietHoursEnd: null, alreadyHasDigestToday: false);

        Assert.False(result);
    }

    [Fact]
    public void ShouldProcessUser_WhenRunTimePassed_NotQuiet_NoExistingDigest_ReturnsTrue()
    {
        var now = new DateTime(2026, 7, 11, 8, 0, 0, DateTimeKind.Utc);

        var result = DailyDigestEligibility.ShouldProcessUser(
            now, runAtLocal: "07:00", quietHoursStart: "22:00", quietHoursEnd: "07:00", alreadyHasDigestToday: false);

        Assert.True(result);
    }

    // ---------------------------------------------------------------
    // §3 / ADR-0007 — Child RBAC filtering
    // ---------------------------------------------------------------

    [Fact]
    public void DeadlineVisibleTo_Child_OnlySeesOwnNonPrivateRecords()
    {
        var child = MakeUser(UserRole.Child, out var childFamilyMemberId);
        var otherMemberId = Guid.NewGuid();

        var deadlines = new List<Deadline>
        {
            Deadline.Create("Sajátja, nem privát", DateTime.UtcNow.AddDays(1), Guid.NewGuid(), relatedFamilyMemberId: childFamilyMemberId, isPrivate: false),
            Deadline.Create("Sajátja, privát", DateTime.UtcNow.AddDays(1), Guid.NewGuid(), relatedFamilyMemberId: childFamilyMemberId, isPrivate: true),
            Deadline.Create("Másé, nem privát", DateTime.UtcNow.AddDays(1), Guid.NewGuid(), relatedFamilyMemberId: otherMemberId, isPrivate: false),
        };

        var visible = deadlines.AsQueryable().VisibleTo(child).ToList();

        var titles = visible.Select(d => d.Title).ToList();
        Assert.Single(visible);
        Assert.Contains("Sajátja, nem privát", titles);
    }

    [Fact]
    public void DeadlineVisibleTo_Adult_SeesFamilyNonPrivatePlusOwnPrivate()
    {
        var adult = MakeUser(UserRole.Adult, out _);
        var otherAdultId = Guid.NewGuid();

        var deadlines = new List<Deadline>
        {
            Deadline.Create("Család, nem privát", DateTime.UtcNow.AddDays(1), otherAdultId, isPrivate: false),
            Deadline.Create("Saját, privát", DateTime.UtcNow.AddDays(1), adult.Id, isPrivate: true),
            Deadline.Create("Másé, privát", DateTime.UtcNow.AddDays(1), otherAdultId, isPrivate: true),
        };

        var visible = deadlines.AsQueryable().VisibleTo(adult).Select(d => d.Title).ToList();

        Assert.Contains("Család, nem privát", visible);
        Assert.Contains("Saját, privát", visible);
        Assert.DoesNotContain("Másé, privát", visible);
    }

    [Fact]
    public void DocumentVisibleTo_Child_ExcludesPrivateAndUnrelatedRecords()
    {
        var child = MakeUser(UserRole.Child, out var childFamilyMemberId);

        var documents = new List<Document>
        {
            Document.Create("sajat.pdf", "sajat.pdf", "application/pdf", 10, "path", "sha", SourceType.Upload, Origin.Manual, Guid.NewGuid(), relatedFamilyMemberId: childFamilyMemberId, isPrivate: false),
            Document.Create("sajat-privat.pdf", "sajat-privat.pdf", "application/pdf", 10, "path", "sha2", SourceType.Upload, Origin.Manual, Guid.NewGuid(), relatedFamilyMemberId: childFamilyMemberId, isPrivate: true),
            Document.Create("idegen.pdf", "idegen.pdf", "application/pdf", 10, "path", "sha3", SourceType.Upload, Origin.Manual, Guid.NewGuid(), relatedFamilyMemberId: Guid.NewGuid(), isPrivate: false),
        };

        var visible = documents.AsQueryable().VisibleTo(child).Select(d => d.Title).ToList();

        Assert.Equal(["sajat.pdf"], visible);
    }

    private static UserAccount MakeUser(UserRole role, out Guid familyMemberId)
    {
        var user = UserAccount.Create(Guid.NewGuid(), $"sub-{Guid.NewGuid()}", $"{Guid.NewGuid()}@test.local", "Teszt Felhasználó", role);
        familyMemberId = user.FamilyMemberId;
        return user;
    }
}
