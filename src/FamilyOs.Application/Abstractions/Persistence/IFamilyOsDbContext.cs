using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Abstractions.Persistence;

public interface IFamilyOsDbContext
{
    DbSet<FamilyMember> FamilyMembers { get; }
    DbSet<UserAccount> UserAccounts { get; }
    DbSet<RevokedSession> RevokedSessions { get; }
    DbSet<PendingInvite> PendingInvites { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Document> Documents { get; }
    DbSet<DocumentText> DocumentTexts { get; }
    DbSet<Warranty> Warranties { get; }
    DbSet<MedicalRecord> MedicalRecords { get; }
    DbSet<FinancialRecord> FinancialRecords { get; }
    DbSet<AiProcessingJob> AiProcessingJobs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
