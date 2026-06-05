using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Infrastructure.Persistence.EntityTypeConfigurations
{
    public class UserAIProfileConfiguration : IEntityTypeConfiguration<UserAIProfile>
    {
        public void Configure(EntityTypeBuilder<UserAIProfile> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId).IsRequired();
            builder.HasIndex(x => x.UserId).IsUnique();

            builder.Property(x => x.AIOperationLanguage).HasMaxLength(2);

            builder.Property(x => x.ProviderSettingsJson)
                .HasColumnType("jsonb")
                .IsRequired();

            builder.OwnsOne(x => x.Transcription, owned =>
            {
                owned.Property(o => o.TargetLanguage).HasMaxLength(2);
                owned.Property(o => o.CustomPrompt).HasMaxLength(2000);
                owned.Property(o => o.UseCustomPrompt);
                owned.Property(o => o.IsAutomatic);
            });

            builder.OwnsOne(x => x.Structuring, owned =>
            {
                owned.Property(o => o.TargetLanguage).HasMaxLength(2);
                owned.Property(o => o.CustomPrompt).HasMaxLength(5000);
                owned.Property(o => o.UseCustomPrompt);
                owned.Property(o => o.IsAutomatic);
            });

            builder.OwnsOne(x => x.Summarization, owned =>
            {
                owned.Property(o => o.TargetLanguage).HasMaxLength(2);
                owned.Property(o => o.CustomPrompt).HasMaxLength(2000);
                owned.Property(o => o.UseCustomPrompt);
                owned.Property(o => o.IsAutomatic);
            });

            builder.OwnsOne(x => x.GlobalChat, owned =>
            {
                owned.Property(o => o.TargetLanguage).HasMaxLength(2);
                owned.Property(o => o.CustomPrompt).HasMaxLength(5000);
                owned.Property(o => o.UseCustomPrompt);
                owned.Property(o => o.IsAutomatic);
            });

            builder.OwnsOne(x => x.NoteChat, owned =>
            {
                owned.Property(o => o.TargetLanguage).HasMaxLength(2);
                owned.Property(o => o.CustomPrompt).HasMaxLength(5000);
                owned.Property(o => o.UseCustomPrompt);
                owned.Property(o => o.IsAutomatic);
            });

            builder.OwnsOne(x => x.Classification, owned =>
            {
                owned.Property(o => o.TargetLanguage).HasMaxLength(2);
                owned.Property(o => o.CustomPrompt).HasMaxLength(2000);
                owned.Property(o => o.UseCustomPrompt);
                owned.Property(o => o.IsAutomatic);
            });
        }
    }
}
