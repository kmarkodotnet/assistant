using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class PendingInviteConfiguration : IEntityTypeConfiguration<PendingInvite>
{
    public void Configure(EntityTypeBuilder<PendingInvite> builder)
    {
        builder.ToTable("pending_invite", "app");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(x => x.Role)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("ux_pending_invite_email");
    }
}
