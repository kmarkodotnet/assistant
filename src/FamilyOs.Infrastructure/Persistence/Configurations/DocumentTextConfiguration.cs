using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class DocumentTextConfiguration : IEntityTypeConfiguration<DocumentText>
{
    public void Configure(EntityTypeBuilder<DocumentText> builder)
    {
        builder.ToTable("document_text", "app");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Content).IsRequired();

        builder.Property(x => x.ExtractionMethod)
            .HasColumnType("app.extraction_method");

        builder.Property(x => x.OcrConfidence)
            .HasColumnType("numeric(5,2)");

        // UpdatedUtc managed by DB trigger
        builder.Property(x => x.UpdatedUtc)
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);

        // tsv is a GENERATED ALWAYS AS column in PostgreSQL — EF does not manage it
        builder.Ignore("tsv");
    }
}
