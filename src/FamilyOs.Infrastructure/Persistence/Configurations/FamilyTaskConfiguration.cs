using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class FamilyTaskConfiguration : IEntityTypeConfiguration<FamilyTask>
{
    public void Configure(EntityTypeBuilder<FamilyTask> builder)
    {
        builder.ToTable("task", "app");

        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => x.DeletedUtc == null);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(2000);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Priority)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Origin)
            .HasColumnType("app.origin");

        builder.HasOne(t => t.SourceDocument)
            .WithMany()
            .HasForeignKey(t => t.SourceDocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.SourceDocumentId, x.Title })
            .HasDatabaseName("ix_task_source_title")
            .HasFilter("deleted_utc IS NULL");
    }
}
