using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NeuroNotes.Application.Interfaces.Persistence;
using NeuroNotes.Domain.Entities;
using NeuroNotes.Infrastructure.Identity.Models;

namespace NeuroNotes.Infrastructure.Persistence
{
    public class NeuroNotesDbContext : IdentityDbContext<AppUser>, IApplicationDbContext
    {
        public const string ConnectionStringName = "DefaultConnection";

        public NeuroNotesDbContext(DbContextOptions<NeuroNotesDbContext> options)
            : base(options) { }

        public DbSet<Note> Notes { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<UserAIProfile> UserAIProfiles { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<NoteChunk> NoteChunks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<RefreshToken>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Token).IsRequired();
                builder.HasIndex(x => x.Token).IsUnique();
            });

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(NeuroNotesDbContext).Assembly);
        }
    }
}
