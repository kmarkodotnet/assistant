using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("user_account", "app", t =>
        {
            t.HasCheckConstraint("ck_user_account_email_lower", "email = lower(email)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.GoogleSubject).IsRequired();
        builder.Property(x => x.Email).IsRequired();
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);

        builder.HasIndex(x => x.GoogleSubject)
            .IsUnique()
            .HasFilter("deleted_utc IS NULL")
            .HasDatabaseName("ux_user_account_google_subject");
        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasFilter("deleted_utc IS NULL")
            .HasDatabaseName("ux_user_account_email");
        builder.HasIndex(x => x.IsActive)
            .HasFilter("deleted_utc IS NULL")
            .HasDatabaseName("ix_user_account_is_active");

        builder.HasOne(x => x.FamilyMember)
            .WithOne(x => x.UserAccount)
            .HasForeignKey<UserAccount>(x => x.FamilyMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // xmin concurrency token (PostgreSQL system column)
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        builder.HasQueryFilter(x => x.DeletedUtc == null);

        builder.Property(x => x.UpdatedUtc)
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
    }
}
