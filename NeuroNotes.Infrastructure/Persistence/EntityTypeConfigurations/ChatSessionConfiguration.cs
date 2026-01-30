using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Infrastructure.Persistence.EntityTypeConfigurations
{
    public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
    {
        public void Configure(EntityTypeBuilder<ChatSession> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedNever();

            builder.HasMany(s => s.Messages)
                   .WithOne(m => m.ChatSession) 
                   .HasForeignKey(m => m.ChatSessionId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
