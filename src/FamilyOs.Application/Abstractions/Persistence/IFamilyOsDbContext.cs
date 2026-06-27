using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyOs.Application.Abstractions.Persistence;

public interface IFamilyOsDbContext
{
    DatabaseFacade Database { get; }
    DbSet<FamilyMember> FamilyMembers { get; }
    DbSet<UserAccount> UserAccounts { get; }
    DbSet<RevokedSession> RevokedSessions { get; }
    DbSet<PendingInvite> PendingInvites { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Document> Documents { get; }
    DbSet<DocumentText> DocumentTexts { get; }
    DbSet<DocumentSummary> DocumentSummaries { get; }
    DbSet<DocumentChunk> DocumentChunks { get; }
    DbSet<Tag> Tags { get; }
    DbSet<Topic> Topics { get; }
    DbSet<DocumentTag> DocumentTags { get; }
    DbSet<DocumentTopic> DocumentTopics { get; }
    DbSet<Warranty> Warranties { get; }
    DbSet<MedicalRecord> MedicalRecords { get; }
    DbSet<FinancialRecord> FinancialRecords { get; }
    DbSet<Deadline> Deadlines { get; }
    DbSet<FamilyTask> Tasks { get; }
    DbSet<AiProcessingJob> AiProcessingJobs { get; }
    DbSet<SavedSearch> SavedSearches { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
