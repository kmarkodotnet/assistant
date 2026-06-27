using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class AiProcessingJobConfiguration : IEntityTypeConfiguration<AiProcessingJob>
{
    public void Configure(EntityTypeBuilder<AiProcessingJob> builder)
    {
        builder.ToTable("ai_processing_job", "app");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.JobType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.TargetType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(x => x.NextAttemptUtc)
            .IsRequired();

        builder.Property(x => x.CreatedUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedUtc)
            .IsRequired();

        // Partial index for pending/failed jobs waiting to be scheduled
        builder.HasIndex(x => new { x.NextAttemptUtc, x.CreatedUtc })
            .HasDatabaseName("ix_ai_processing_job_pending")
            .HasFilter("status IN ('Queued', 'Failed')");
    }
}
