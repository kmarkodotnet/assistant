using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class DocumentSummaryConfiguration : IEntityTypeConfiguration<DocumentSummary>
{
    public void Configure(EntityTypeBuilder<DocumentSummary> builder)
    {
        builder.ToTable("document_summary", "app");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.ModelName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.PromptVersion).IsRequired().HasMaxLength(50);
        builder.Property(x => x.IsCurrent).IsRequired();

        // Unique partial index: only one current summary per document
        builder.HasIndex(x => x.DocumentId)
            .IsUnique()
            .HasDatabaseName("uix_document_summary_current")
            .HasFilter("is_current = true");

        builder.HasOne(s => s.Document)
            .WithMany()
            .HasForeignKey(s => s.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
