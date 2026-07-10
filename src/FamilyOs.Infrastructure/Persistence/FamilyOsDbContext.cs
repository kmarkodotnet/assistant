using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql.NameTranslation;

namespace FamilyOs.Infrastructure.Persistence;

public sealed class FamilyOsDbContext : DbContext, IFamilyOsDbContext
{
    public FamilyOsDbContext(DbContextOptions<FamilyOsDbContext> options) : base(options) { }

    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<RevokedSession> RevokedSessions => Set<RevokedSession>();
    public DbSet<PendingInvite> PendingInvites => Set<PendingInvite>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentText> DocumentTexts => Set<DocumentText>();
    public DbSet<DocumentSummary> DocumentSummaries => Set<DocumentSummary>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<DocumentTag> DocumentTags => Set<DocumentTag>();
    public DbSet<DocumentTopic> DocumentTopics => Set<DocumentTopic>();
    public DbSet<Warranty> Warranties => Set<Warranty>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<FinancialRecord> FinancialRecords => Set<FinancialRecord>();
    public DbSet<Deadline> Deadlines => Set<Deadline>();
    public DbSet<FamilyTask> Tasks => Set<FamilyTask>();
    public DbSet<AiProcessingJob> AiProcessingJobs => Set<AiProcessingJob>();
    public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<NotificationFeed> NotificationFeed => Set<NotificationFeed>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<NoteChunk> NoteChunks => Set<NoteChunk>();
    public DbSet<TaskChunk> TaskChunks => Set<TaskChunk>();
    public DbSet<DeadlineChunk> DeadlineChunks => Set<DeadlineChunk>();
    public DbSet<NoteTag> NoteTags => Set<NoteTag>();
    public DbSet<NoteTopic> NoteTopics => Set<NoteTopic>();
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("app");

        // pgvector extension
        modelBuilder.HasPostgresExtension("vector");

        // DB enum labels are PascalCase; NpgsqlNullNameTranslator preserves C# member names as-is.
        var pgNameTranslator = new NpgsqlNullNameTranslator();

        modelBuilder.HasPostgresEnum<UserRole>("app", "user_role", pgNameTranslator);
        modelBuilder.HasPostgresEnum<Relation>("app", "relation", pgNameTranslator);
        modelBuilder.HasPostgresEnum<AuditAction>("app", "audit_action", pgNameTranslator);
        modelBuilder.HasPostgresEnum<ProcessingStatus>("app", "processing_status", pgNameTranslator);
        modelBuilder.HasPostgresEnum<SourceType>("app", "source_type", pgNameTranslator);
        modelBuilder.HasPostgresEnum<Origin>("app", "origin", pgNameTranslator);
        modelBuilder.HasPostgresEnum<ExtractionMethod>("app", "extraction_method", pgNameTranslator);
        modelBuilder.HasPostgresEnum<MedicalRecordType>("app", "medical_record_type", pgNameTranslator);
        modelBuilder.HasPostgresEnum<FinancialRecordType>("app", "financial_record_type", pgNameTranslator);
        modelBuilder.HasPostgresEnum<RecurrencePeriod>("app", "recurrence_period", pgNameTranslator);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FamilyOsDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
