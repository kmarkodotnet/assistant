using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
{
    public void Configure(EntityTypeBuilder<MedicalRecord> builder)
    {
        builder.ToTable("medical_record", "app");

        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => x.DeletedUtc == null);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Provider).HasMaxLength(300);
        builder.Property(x => x.StructuredJson).HasColumnType("jsonb");

        builder.Property(x => x.RecordType)
            .HasColumnType("app.medical_record_type");

        builder.HasOne(r => r.Document)
            .WithMany()
            .HasForeignKey(r => r.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.FamilyMember)
            .WithMany()
            .HasForeignKey(r => r.FamilyMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
