using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class NoteTopicConfiguration : IEntityTypeConfiguration<NoteTopic>
{
    public void Configure(EntityTypeBuilder<NoteTopic> builder)
    {
        builder.ToTable("note_topic", "app");

        builder.HasKey(x => new { x.NoteId, x.TopicId });

        builder.HasOne(nt => nt.Note)
            .WithMany(n => n.NoteTopics)
            .HasForeignKey(nt => nt.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(nt => nt.Topic)
            .WithMany(t => t.NoteTopics)
            .HasForeignKey(nt => nt.TopicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
