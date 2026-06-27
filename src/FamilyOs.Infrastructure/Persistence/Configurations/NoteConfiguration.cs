using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("note", "app");

        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => x.DeletedUtc == null);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Body).IsRequired();

        builder.HasOne(n => n.RelatedFamilyMember)
            .WithMany()
            .HasForeignKey(n => n.RelatedFamilyMemberId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(x => new { x.CreatedByUserAccountId, x.CreatedUtc })
            .HasDatabaseName("ix_note_user_created")
            .HasFilter("deleted_utc IS NULL");
    }
}
