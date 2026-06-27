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
    public DbSet<Warranty> Warranties => Set<Warranty>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<FinancialRecord> FinancialRecords => Set<FinancialRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("app");

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
