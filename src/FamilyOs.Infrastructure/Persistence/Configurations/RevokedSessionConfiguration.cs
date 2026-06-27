using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class RevokedSessionConfiguration : IEntityTypeConfiguration<RevokedSession>
{
    public void Configure(EntityTypeBuilder<RevokedSession> builder)
    {
        builder.ToTable("revoked_session", "app");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SessionId)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(x => x.SessionId)
            .IsUnique()
            .HasDatabaseName("ix_revoked_session_session_id");
    }
}
