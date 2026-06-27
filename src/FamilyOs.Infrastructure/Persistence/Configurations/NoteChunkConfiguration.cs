using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class NoteChunkConfiguration : IEntityTypeConfiguration<NoteChunk>
{
    public void Configure(EntityTypeBuilder<NoteChunk> builder)
    {
        builder.ToTable("note_chunk", "app");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.EmbeddingModel).IsRequired().HasMaxLength(200);

        builder.Property(x => x.Embedding)
            .HasColumnType("vector(768)");

        builder.HasIndex(x => new { x.NoteId, x.ChunkIndex })
            .IsUnique()
            .HasDatabaseName("uix_note_chunk_note_index");

        builder.HasOne(c => c.Note)
            .WithMany(n => n.Chunks)
            .HasForeignKey(c => c.NoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
