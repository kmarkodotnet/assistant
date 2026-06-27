using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Abstractions.Persistence;

public interface IFamilyOsDbContext
{
    DbSet<FamilyMember> FamilyMembers { get; }
    DbSet<UserAccount> UserAccounts { get; }
    DbSet<RevokedSession> RevokedSessions { get; }
    DbSet<PendingInvite> PendingInvites { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
