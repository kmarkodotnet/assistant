using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using Microsoft.EntityFrameworkCore;

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
    public DbSet<NoteTag> NoteTags => Set<NoteTag>();
    public DbSet<NoteTopic> NoteTopics => Set<NoteTopic>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("app");

        // pgvector extension
        modelBuilder.HasPostgresExtension("vector");

        // Register PostgreSQL enums
        modelBuilder.HasPostgresEnum<UserRole>("app", "user_role");
        modelBuilder.HasPostgresEnum<Relation>("app", "relation");
        modelBuilder.HasPostgresEnum<AuditAction>("app", "audit_action");
        modelBuilder.HasPostgresEnum<ProcessingStatus>("app", "processing_status");
        modelBuilder.HasPostgresEnum<SourceType>("app", "source_type");
        modelBuilder.HasPostgresEnum<Origin>("app", "origin");
        modelBuilder.HasPostgresEnum<ExtractionMethod>("app", "extraction_method");
        modelBuilder.HasPostgresEnum<MedicalRecordType>("app", "medical_record_type");
        modelBuilder.HasPostgresEnum<FinancialRecordType>("app", "financial_record_type");
        modelBuilder.HasPostgresEnum<RecurrencePeriod>("app", "recurrence_period");

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FamilyOsDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
