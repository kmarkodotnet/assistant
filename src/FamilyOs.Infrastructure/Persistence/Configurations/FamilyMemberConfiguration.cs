using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class FamilyMemberConfiguration : IEntityTypeConfiguration<FamilyMember>
{
    public void Configure(EntityTypeBuilder<FamilyMember> builder)
    {
        builder.ToTable("family_member", "app", t =>
        {
            t.HasCheckConstraint("ck_family_member_display_name_len",
                "char_length(display_name) BETWEEN 1 AND 100");
            t.HasCheckConstraint("ck_family_member_full_name_len",
                "full_name IS NULL OR char_length(full_name) <= 200");
            t.HasCheckConstraint("ck_family_member_birth_date",
                "birth_date IS NULL OR birth_date <= current_date");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.FullName).HasMaxLength(200);
        builder.Property(x => x.Notes);

        builder.HasIndex(x => x.Relation)
            .HasFilter("deleted_utc IS NULL")
            .HasDatabaseName("ix_family_member_relation");
        builder.HasIndex(x => x.HasUserAccount)
            .HasFilter("deleted_utc IS NULL")
            .HasDatabaseName("ix_family_member_has_user_account");

        // xmin concurrency token (PostgreSQL system column)
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        builder.HasQueryFilter(x => x.DeletedUtc == null);

        // UpdatedUtc is managed by DB trigger
        builder.Property(x => x.UpdatedUtc)
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
    }
}
