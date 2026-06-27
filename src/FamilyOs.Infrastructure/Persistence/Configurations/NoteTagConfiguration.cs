using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyOs.Infrastructure.Persistence.Configurations;

internal sealed class NoteTagConfiguration : IEntityTypeConfiguration<NoteTag>
{
    public void Configure(EntityTypeBuilder<NoteTag> builder)
    {
        builder.ToTable("note_tag", "app");

        builder.HasKey(x => new { x.NoteId, x.TagId });

        builder.Property(x => x.Origin)
            .HasColumnType("app.origin");

        builder.HasOne(nt => nt.Note)
            .WithMany(n => n.NoteTags)
            .HasForeignKey(nt => nt.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(nt => nt.Tag)
            .WithMany()
            .HasForeignKey(nt => nt.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
