using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Infrastructure.Persistence.EntityTypeConfigurations
{
    public class NoteChunkConfiguration : IEntityTypeConfiguration<NoteChunk>
    {
        public void Configure(EntityTypeBuilder<NoteChunk> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Content).IsRequired();

            builder.Property(x => x.Embedding)
                .HasColumnType("vector"); // TODO Подумать

            builder.HasOne(x => x.Note)
                .WithMany()
                .HasForeignKey(x => x.NoteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
