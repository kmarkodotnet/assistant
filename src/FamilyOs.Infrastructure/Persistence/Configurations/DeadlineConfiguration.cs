using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class DeadlineConfiguration : IEntityTypeConfiguration<Deadline>
{
    public void Configure(EntityTypeBuilder<Deadline> builder)
    {
        builder.ToTable("deadline", "app");

        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => x.DeletedUtc == null);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(2000);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Origin)
            .HasColumnType("app.origin");

        builder.HasOne(d => d.SourceDocument)
            .WithMany()
            .HasForeignKey(d => d.SourceDocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.SourceDocumentId, x.Title })
            .HasDatabaseName("ix_deadline_source_title")
            .HasFilter("deleted_utc IS NULL");
    }
}
