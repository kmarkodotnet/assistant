using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Infrastructure.Persistence;

public sealed class FamilyOsDbContext : DbContext
{
    public FamilyOsDbContext(DbContextOptions<FamilyOsDbContext> options) : base(options) { }

    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("app");

        // Register PostgreSQL enums
        modelBuilder.HasPostgresEnum<UserRole>("app", "user_role");
        modelBuilder.HasPostgresEnum<Relation>("app", "relation");

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FamilyOsDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
