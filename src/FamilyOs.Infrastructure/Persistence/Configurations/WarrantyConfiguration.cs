using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class WarrantyConfiguration : IEntityTypeConfiguration<Warranty>
{
    public void Configure(EntityTypeBuilder<Warranty> builder)
    {
        builder.ToTable("warranty", "app");

        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => x.DeletedUtc == null);

        builder.Property(x => x.ProductName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Brand).HasMaxLength(200);
        builder.Property(x => x.Model).HasMaxLength(200);
        builder.Property(x => x.SerialNumber).HasMaxLength(200);
        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.Seller).HasMaxLength(300);

        builder.Property(x => x.PurchasePrice).HasColumnType("numeric(18,2)");

        builder.HasIndex(x => x.DocumentId)
            .IsUnique()
            .HasDatabaseName("ux_warranty_document_id");

        builder.HasOne(w => w.Document)
            .WithMany()
            .HasForeignKey(w => w.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
