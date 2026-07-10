using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class TaskChunkConfiguration : IEntityTypeConfiguration<TaskChunk>
{
    public void Configure(EntityTypeBuilder<TaskChunk> builder)
    {
        builder.ToTable("task_chunk", "app");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.EmbeddingModel).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Embedding).HasColumnType("vector(768)");
        builder.HasIndex(x => new { x.TaskId, x.ChunkIndex })
            .IsUnique()
            .HasDatabaseName("uix_task_chunk_task_index");
        builder.HasOne(c => c.Task)
            .WithMany()
            .HasForeignKey(c => c.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
