using Microsoft.EntityFrameworkCore;
using NeuroNotes.Domain.Entities;

namespace NeuroNotes.Application.Interfaces.Persistence
{
    public interface IApplicationDbContext
    {
        DbSet<Note> Notes { get; }
        DbSet<RefreshToken> RefreshTokens { get; set; }
        DbSet<UserProfile> UserProfiles { get; set; }
        DbSet<UserAIProfile> UserAIProfiles { get; set; }
        DbSet<ChatSession> ChatSessions { get; }
        DbSet<ChatMessage> ChatMessages { get; }
        DbSet<NoteChunk> NoteChunks { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
