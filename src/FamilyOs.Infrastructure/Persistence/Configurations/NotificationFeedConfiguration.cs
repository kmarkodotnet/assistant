using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class NotificationFeedConfiguration : IEntityTypeConfiguration<NotificationFeed>
{
    public void Configure(EntityTypeBuilder<NotificationFeed> builder)
    {
        builder.ToTable("notification_feed", "app");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Body).HasMaxLength(2000);
        builder.Property(x => x.ActionUrl).HasMaxLength(500);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(256);

        // Unique on idempotency_key WHERE NOT NULL
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("uix_notification_feed_idempotency_key")
            .HasFilter("idempotency_key IS NOT NULL");

        // Index for unread feed
        builder.HasIndex(x => new { x.TargetUserAccountId, x.CreatedUtc })
            .HasDatabaseName("ix_notification_feed_user_created")
            .HasFilter("read_utc IS NULL");
    }
}
