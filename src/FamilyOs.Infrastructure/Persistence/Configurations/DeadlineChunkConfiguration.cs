using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class DeadlineChunkConfiguration : IEntityTypeConfiguration<DeadlineChunk>
{
    public void Configure(EntityTypeBuilder<DeadlineChunk> builder)
    {
        builder.ToTable("deadline_chunk", "app");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.EmbeddingModel).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Embedding).HasColumnType("vector(768)");
        builder.HasIndex(x => new { x.DeadlineId, x.ChunkIndex })
            .IsUnique()
            .HasDatabaseName("uix_deadline_chunk_deadline_index");
        builder.HasOne(c => c.Deadline)
            .WithMany()
            .HasForeignKey(c => c.DeadlineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
