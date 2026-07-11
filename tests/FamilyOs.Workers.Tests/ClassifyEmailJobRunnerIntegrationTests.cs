using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Abstractions.Notifications;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Notifications;
using FamilyOs.Infrastructure.Persistence;
using FamilyOs.Workers.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Npgsql.NameTranslation;
using Testcontainers.PostgreSql;

namespace FamilyOs.Workers.Tests;

/// <summary>
/// End-to-end backend-integration tests for ClassifyEmailJobRunner against a real Postgres
/// instance (Testcontainers), following the MigrationsTests.cs / GmailIngestionServiceTests.cs
/// container-setup pattern. Covers the full acceptance-mapping table
/// (docs/contracts/classify-email-contract.md §12):
///
/// - High-importance email: fields persisted on EmailMessage + notification_feed row created
///   for the oldest active Admin, via the *real* InAppNotificationService (not a mock), so the
///   idempotency dedup guarantee is exercised end-to-end.
/// - Low/Medium: fields persisted, no notification_feed row.
/// - HasDeadlineHint: purely informative — no other entity (e.g. Deadline) created/modified.
/// - Retry / re-run of the same job: no duplicate notification_feed row (IdempotencyKey dedup).
/// - No active Admin in the household: guard — RunAsync must not throw.
///
/// ClassifyEmailJobRunnerTests.cs (unit tests) covers the pure BuildEnvelope/Truncate logic and
/// is not duplicated here.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ClassifyEmailJobRunnerIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .Build();

    private NpgsqlDataSource _dataSource = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Migrate first, using a plain (non-enum-mapped) connection — mirrors MigrationsTests.cs.
        // The enum-mapped NpgsqlDataSource built below caches Postgres's type catalog on its
        // first connection, so it must only be built/opened *after* `CREATE TYPE app.relation`
        // etc. have actually run, otherwise "app.relation was not found" errors follow.
        var migrationOptions = new DbContextOptionsBuilder<FamilyOsDbContext>()
            .UseNpgsql(
                _postgres.GetConnectionString(),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "app");
                    npgsql.UseVector();
                })
            .UseSnakeCaseNamingConvention()
            .Options;
        await using (var migrationDb = new FamilyOsDbContext(migrationOptions))
        {
            await migrationDb.Database.MigrateAsync();
        }

        // Native pg-enum columns (e.g. user_account.role, family_member.relation) require the
        // enum to be mapped on the NpgsqlDataSource itself, mirroring
        // src/FamilyOs.Infrastructure/DependencyInjection.cs AddInfrastructure().
        var pgNameTranslator = new NpgsqlNullNameTranslator();
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_postgres.GetConnectionString());
        dataSourceBuilder.MapEnum<UserRole>("app.user_role", nameTranslator: pgNameTranslator);
        dataSourceBuilder.MapEnum<Relation>("app.relation", nameTranslator: pgNameTranslator);
        dataSourceBuilder.UseVector();
        _dataSource = dataSourceBuilder.Build();
    }

    public async Task DisposeAsync()
    {
        await _dataSource.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private FamilyOsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FamilyOsDbContext>()
            .UseNpgsql(
                _dataSource,
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "app");
                    npgsql.UseVector();
                })
            .UseSnakeCaseNamingConvention()
            .Options;

        return new FamilyOsDbContext(options);
    }

    // ----- Fakes ------------------------------------------------------------

    private sealed class FakeEmailImportanceClassifier : IEmailImportanceClassifier
    {
        private readonly EmailImportanceResult _result;

        public FakeEmailImportanceClassifier(EmailImportanceResult result) => _result = result;

        public Task<EmailImportanceResult> ClassifyAsync(string subject, string? bodyText, CancellationToken ct = default)
            => Task.FromResult(_result);
    }

    private static ClassifyEmailJobRunner CreateRunner(FamilyOsDbContext db, EmailImportanceResult result)
    {
        var notifications = new InAppNotificationService(
            db,
            new NullNotificationPusher(),
            NullLogger<InAppNotificationService>.Instance);

        return new ClassifyEmailJobRunner(
            db,
            new FakeEmailImportanceClassifier(result),
            notifications,
            NullLogger<ClassifyEmailJobRunner>.Instance);
    }

    // ----- Seed helpers -------------------------------------------------------

    private static async Task<UserAccount> SeedAdminAsync(FamilyOsDbContext db, string label = "admin")
    {
        var member = FamilyMember.Create($"Admin {label}", Relation.Parent);
        db.FamilyMembers.Add(member);

        var admin = UserAccount.Create(
            member.Id,
            googleSubject: $"google-{Guid.NewGuid()}",
            email: $"{label}-{Guid.NewGuid():N}@example.com",
            displayName: $"Admin {label}",
            role: UserRole.Admin);
        db.UserAccounts.Add(admin);

        await db.SaveChangesAsync();
        return admin;
    }

    private static async Task<EmailMessage> SeedEmailAsync(
        FamilyOsDbContext db,
        string subject = "Fizetési felszólítás",
        string from = "bank@example.com",
        string? bodyText = "Kérjük, rendezze a tartozását 2026. augusztus 5-ig.")
    {
        // email_message.source_id has a DB-level FK to source.id — a real row is required.
        // Saved in its own round-trip first: EmailMessageConfiguration has no HasOne(Source)
        // navigation, so EF doesn't know about the FK and won't order a combined batch for us.
        var source = Source.Create("Gmail", SourceKind.GmailAccount, "{\"refresh_token\":\"rt-test\"}");
        db.Sources.Add(source);
        await db.SaveChangesAsync();

        var email = EmailMessage.Create(
            sourceId: source.Id,
            gmailMessageId: $"msg-{Guid.NewGuid():N}",
            fromAddress: from,
            toAddresses: "me@example.com",
            subject: subject,
            receivedUtc: DateTime.UtcNow,
            hasAttachments: false);

        if (bodyText is not null)
        {
            email.SetBody(bodyText, bodyHtml: null, snippet: bodyText[..Math.Min(80, bodyText.Length)]);
        }

        db.EmailMessages.Add(email);
        await db.SaveChangesAsync();
        return email;
    }

    private static AiProcessingJob BuildJob(Guid emailId) =>
        AiProcessingJob.CreateForEmailMessage(AiJobType.ClassifyEmail, emailId);

    // =====================================================================
    // High importance
    // =====================================================================

    [Fact]
    public async Task RunAsync_HighImportance_PersistsFieldsAndCreatesNotificationForOldestActiveAdmin()
    {
        await using var seedDb = CreateDbContext();
        var admin = await SeedAdminAsync(seedDb);
        var email = await SeedEmailAsync(seedDb);

        var result = new EmailImportanceResult(EmailImportance.High, "hivatalos", true);

        await using (var actDb = CreateDbContext())
        {
            var runner = CreateRunner(actDb, result);
            await runner.RunAsync(BuildJob(email.Id), CancellationToken.None);
        }

        await using var assertDb = CreateDbContext();
        var persisted = await assertDb.EmailMessages.SingleAsync(e => e.Id == email.Id);
        persisted.Importance.Should().Be(EmailImportance.High);
        persisted.Category.Should().Be("hivatalos");
        persisted.HasDeadlineHint.Should().BeTrue();

        var feedRows = await assertDb.NotificationFeed
            .Where(n => n.TargetUserAccountId == admin.Id)
            .ToListAsync();

        feedRows.Should().HaveCount(1);
        feedRows[0].Type.Should().Be("ImportantEmail");
        feedRows[0].IdempotencyKey.Should().Be($"important-email-{email.Id}");
        feedRows[0].ActionUrl.Should().Be("/documents");
    }

    [Fact]
    public async Task RunAsync_MultipleActiveAdmins_NotifiesOnlyTheOldestOne()
    {
        await using var seedDb = CreateDbContext();
        var olderAdmin = await SeedAdminAsync(seedDb, "older");
        var newerAdmin = await SeedAdminAsync(seedDb, "newer");

        // Backdate the first admin so ordering by CreatedUtc is deterministic regardless of
        // DateTime.UtcNow resolution between the two inserts above.
        await seedDb.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE app.user_account SET created_utc = created_utc - interval '1 hour' WHERE id = {olderAdmin.Id}");

        var email = await SeedEmailAsync(seedDb);
        var result = new EmailImportanceResult(EmailImportance.High, null, false);

        await using (var actDb = CreateDbContext())
        {
            var runner = CreateRunner(actDb, result);
            await runner.RunAsync(BuildJob(email.Id), CancellationToken.None);
        }

        await using var assertDb = CreateDbContext();
        var allFeedRows = await assertDb.NotificationFeed.ToListAsync();

        allFeedRows.Should().HaveCount(1);
        allFeedRows[0].TargetUserAccountId.Should().Be(olderAdmin.Id);
        allFeedRows[0].TargetUserAccountId.Should().NotBe(newerAdmin.Id);
    }

    // =====================================================================
    // Low / Medium importance
    // =====================================================================

    [Theory]
    [InlineData(EmailImportance.Low)]
    [InlineData(EmailImportance.Medium)]
    public async Task RunAsync_LowOrMediumImportance_PersistsFieldsButCreatesNoNotification(EmailImportance importance)
    {
        await using var seedDb = CreateDbContext();
        await SeedAdminAsync(seedDb);
        var email = await SeedEmailAsync(seedDb, subject: "Heti hírlevél", bodyText: "Ezen a héten...");

        var result = new EmailImportanceResult(importance, "hirlevel", false);

        await using (var actDb = CreateDbContext())
        {
            var runner = CreateRunner(actDb, result);
            await runner.RunAsync(BuildJob(email.Id), CancellationToken.None);
        }

        await using var assertDb = CreateDbContext();
        var persisted = await assertDb.EmailMessages.SingleAsync(e => e.Id == email.Id);
        persisted.Importance.Should().Be(importance);
        persisted.Category.Should().Be("hirlevel");
        persisted.HasDeadlineHint.Should().BeFalse();

        (await assertDb.NotificationFeed.CountAsync()).Should().Be(0);
    }

    // =====================================================================
    // HasDeadlineHint — purely informative
    // =====================================================================

    [Fact]
    public async Task RunAsync_HasDeadlineHintTrue_IsStoredAsSignalOnly_NoDeadlineEntityCreated()
    {
        await using var seedDb = CreateDbContext();
        await SeedAdminAsync(seedDb);
        var email = await SeedEmailAsync(
            seedDb, subject: "Számla", bodyText: "A díj 2026. augusztus 5-én esedékes.");

        // Medium on purpose: the notification path is covered separately; here we isolate the
        // HasDeadlineHint side-effect (contract §5 — hint must not create/modify any other
        // entity, e.g. Deadline, regardless of the notification branch).
        var result = new EmailImportanceResult(EmailImportance.Medium, "szamla", true);

        await using (var actDb = CreateDbContext())
        {
            var runner = CreateRunner(actDb, result);
            await runner.RunAsync(BuildJob(email.Id), CancellationToken.None);
        }

        await using var assertDb = CreateDbContext();
        var persisted = await assertDb.EmailMessages.SingleAsync(e => e.Id == email.Id);
        persisted.HasDeadlineHint.Should().BeTrue();

        (await assertDb.Deadlines.CountAsync()).Should().Be(0);
        (await assertDb.Documents.CountAsync()).Should().Be(0);
    }

    // =====================================================================
    // Retry / idempotency
    // =====================================================================

    [Fact]
    public async Task RunAsync_RunTwiceForSameJob_DoesNotCreateDuplicateNotification()
    {
        await using var seedDb = CreateDbContext();
        await SeedAdminAsync(seedDb);
        var email = await SeedEmailAsync(seedDb);

        var result = new EmailImportanceResult(EmailImportance.High, "hivatalos", false);
        var job = BuildJob(email.Id);

        await using (var actDb1 = CreateDbContext())
        {
            var runner1 = CreateRunner(actDb1, result);
            await runner1.RunAsync(job, CancellationToken.None);
        }

        // Second run — simulates a Hangfire retry / manual re-run of the same job, going through
        // the real InAppNotificationService so its IdempotencyKey-based AnyAsync dedup check is
        // actually exercised (not just asserted against a mock).
        await using (var actDb2 = CreateDbContext())
        {
            var runner2 = CreateRunner(actDb2, result);
            await runner2.RunAsync(job, CancellationToken.None);
        }

        await using var assertDb = CreateDbContext();
        (await assertDb.NotificationFeed.CountAsync(n => n.IdempotencyKey == $"important-email-{email.Id}"))
            .Should().Be(1);
    }

    // =====================================================================
    // No active Admin — guard
    // =====================================================================

    [Fact]
    public async Task RunAsync_NoActiveAdminInHousehold_DoesNotThrow_AndCreatesNoNotification()
    {
        await using var seedDb = CreateDbContext();
        // Deliberately no UserAccount rows at all.
        var email = await SeedEmailAsync(seedDb);

        var result = new EmailImportanceResult(EmailImportance.High, "hivatalos", false);

        await using var actDb = CreateDbContext();
        var runner = CreateRunner(actDb, result);

        var act = async () => await runner.RunAsync(BuildJob(email.Id), CancellationToken.None);
        await act.Should().NotThrowAsync();

        await using var assertDb = CreateDbContext();
        var persisted = await assertDb.EmailMessages.SingleAsync(e => e.Id == email.Id);
        persisted.Importance.Should().Be(EmailImportance.High); // classification still persisted

        (await assertDb.NotificationFeed.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_OnlySoftDeletedAdmin_TreatsAsNoActiveAdmin_DoesNotThrow()
    {
        await using var seedDb = CreateDbContext();
        var admin = await SeedAdminAsync(seedDb);
        await seedDb.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE app.user_account SET deleted_utc = now() WHERE id = {admin.Id}");

        var email = await SeedEmailAsync(seedDb);
        var result = new EmailImportanceResult(EmailImportance.High, null, false);

        await using var actDb = CreateDbContext();
        var runner = CreateRunner(actDb, result);

        var act = async () => await runner.RunAsync(BuildJob(email.Id), CancellationToken.None);
        await act.Should().NotThrowAsync();

        await using var assertDb = CreateDbContext();
        (await assertDb.NotificationFeed.CountAsync()).Should().Be(0);
    }
}
