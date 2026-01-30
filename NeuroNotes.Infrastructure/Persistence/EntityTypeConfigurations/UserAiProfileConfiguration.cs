using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Infrastructure.Persistence.EntityTypeConfigurations
{
    public class UserAiProfileConfiguration : IEntityTypeConfiguration<UserAIProfile>
    {
        public void Configure(EntityTypeBuilder<UserAIProfile> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.UserId).IsRequired();

            builder.Property(x => x.ProviderSettingsJson)
                .HasColumnType("jsonb")
                .IsRequired();
        }
    }
}
