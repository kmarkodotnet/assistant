using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class FinancialRecordConfiguration : IEntityTypeConfiguration<FinancialRecord>
{
    public void Configure(EntityTypeBuilder<FinancialRecord> builder)
    {
        builder.ToTable("financial_record", "app");

        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => x.DeletedUtc == null);

        builder.Property(x => x.Vendor).HasMaxLength(300);
        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.Amount).HasColumnType("numeric(18,2)");

        builder.Property(x => x.RecordType)
            .HasColumnType("app.financial_record_type");
        builder.Property(x => x.RecurrencePeriod)
            .HasColumnType("app.recurrence_period");

        builder.HasOne(r => r.Document)
            .WithMany()
            .HasForeignKey(r => r.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
