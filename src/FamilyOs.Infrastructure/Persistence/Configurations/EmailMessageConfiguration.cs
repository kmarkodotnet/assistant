using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class EmailMessageConfiguration : IEntityTypeConfiguration<EmailMessage>
{
    public void Configure(EntityTypeBuilder<EmailMessage> builder)
    {
        builder.ToTable("email_message", "app");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.GmailMessageId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ThreadId)
            .HasMaxLength(200);

        builder.Property(x => x.FromAddress)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.ToAddresses)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.Snippet)
            .HasMaxLength(500);

        builder.Property(x => x.IngestStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.CreatedUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.SourceId, x.GmailMessageId })
            .IsUnique()
            .HasDatabaseName("uix_email_message_source_gmail");

        builder.HasIndex(x => x.IngestStatus)
            .HasDatabaseName("ix_email_message_ingest_status_pending")
            .HasFilter("ingest_status IN ('Pending', 'Failed')");

        builder.HasIndex(x => x.ReceivedUtc)
            .HasDatabaseName("ix_email_message_received_utc");
    }
}
