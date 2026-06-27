using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("document", "app");

        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => x.DeletedUtc == null);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Sha256).IsRequired().HasMaxLength(64);
        builder.Property(x => x.MimeType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.StoragePath).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(500);

        builder.Property(x => x.ProcessingStatus)
            .HasColumnType("app.processing_status");
        builder.Property(x => x.SourceType)
            .HasColumnType("app.source_type");
        builder.Property(x => x.Origin)
            .HasColumnType("app.origin");

        // xmin concurrency token (PostgreSQL system column)
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        // UpdatedUtc managed by DB trigger
        builder.Property(x => x.UpdatedUtc)
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);

        builder.HasOne(d => d.RelatedFamilyMember)
            .WithMany()
            .HasForeignKey(d => d.RelatedFamilyMemberId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(d => d.DocumentText)
            .WithOne(t => t.Document!)
            .HasForeignKey<DocumentText>(t => t.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
