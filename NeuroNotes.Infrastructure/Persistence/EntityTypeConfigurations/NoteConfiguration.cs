using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Infrastructure.Persistence.EntityTypeConfigurations
{
    public class NoteConfiguration : IEntityTypeConfiguration<Note>
    {
        public void Configure(EntityTypeBuilder<Note> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.SourceType)
                .HasConversion<string>();

            builder.Property(x => x.SourceFileUrl)
                .IsRequired(false);

            builder.Property(x => x.RawText)
                .IsRequired(false);
            builder.Property(x => x.StructuredText)
                .IsRequired(false);
            builder.Property(x => x.SummaryText)
                .IsRequired(false);

            builder.Property(x => x.Status)
                .HasConversion<string>();
        }
    }
}
