using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class DocumentTopicConfiguration : IEntityTypeConfiguration<DocumentTopic>
{
    public void Configure(EntityTypeBuilder<DocumentTopic> builder)
    {
        builder.ToTable("document_topic", "app");

        builder.HasKey(x => new { x.DocumentId, x.TopicId });

        builder.Property(x => x.Origin)
            .HasColumnType("app.origin");

        builder.HasOne(x => x.Document)
            .WithMany()
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Topic)
            .WithMany(t => t.DocumentTopics)
            .HasForeignKey(x => x.TopicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
