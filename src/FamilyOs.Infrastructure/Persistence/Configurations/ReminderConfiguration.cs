using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.ToTable("reminder", "app");

        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => x.DeletedUtc == null);

        builder.Property(x => x.Channel)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.RruleExpression).HasMaxLength(500);
        builder.Property(x => x.SnoozeNote).HasMaxLength(500);

        // FK to task (nullable)
        builder.HasOne(r => r.Task)
            .WithMany()
            .HasForeignKey(r => r.TaskId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        // FK to deadline (nullable)
        builder.HasOne(r => r.Deadline)
            .WithMany()
            .HasForeignKey(r => r.DeadlineId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        // DB-level "at most one anchor" check (ADR-0011 D5)
        builder.ToTable(t => t.HasCheckConstraint(
            "chk_reminder_xor",
            "NOT (task_id IS NOT NULL AND deadline_id IS NOT NULL)"));

        // Index on (target_user_account_id, trigger_utc) filtered by Scheduled
        builder.HasIndex(x => new { x.TargetUserAccountId, x.TriggerUtc })
            .HasDatabaseName("ix_reminder_user_trigger")
            .HasFilter("status = 'Scheduled'");
    }
}
