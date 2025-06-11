using Microsoft.EntityFrameworkCore;
using Comms.Models;

namespace Comms.Data
{
    public class CommunicationsDbContext : DbContext
    {
        public CommunicationsDbContext(DbContextOptions<CommunicationsDbContext> options) : base(options)
        {
        }

        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatParticipant> ChatParticipants { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageRead> MessageReads { get; set; }
        public DbSet<UnreadMessageCount> UnreadMessageCounts { get; set; }
        public DbSet<PushSubscription> PushSubscriptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure schema
            modelBuilder.HasDefaultSchema("communications");

            // Configure composite keys
            modelBuilder.Entity<ChatParticipant>()
                .HasKey(cp => new { cp.ChatId, cp.UserId });

            modelBuilder.Entity<MessageRead>()
                .HasKey(mr => new { mr.MessageId, mr.UserId });

            modelBuilder.Entity<UnreadMessageCount>()
                .HasKey(umc => new { umc.UserId, umc.ChatId });

            // Configure unique constraints
            modelBuilder.Entity<PushSubscription>()
                .HasIndex(ps => new { ps.UserId, ps.Endpoint })
                .IsUnique();

            // Configure indexes for performance
            modelBuilder.Entity<ChatParticipant>()
                .HasIndex(cp => cp.UserId);

            modelBuilder.Entity<Message>()
                .HasIndex(m => m.ChatId);

            modelBuilder.Entity<Message>()
                .HasIndex(m => m.SenderId);

            modelBuilder.Entity<MessageRead>()
                .HasIndex(mr => mr.UserId);

            modelBuilder.Entity<UnreadMessageCount>()
                .HasIndex(umc => umc.UserId);

            modelBuilder.Entity<PushSubscription>()
                .HasIndex(ps => ps.UserId);

            // Configure relationships
            modelBuilder.Entity<ChatParticipant>()
                .HasOne(cp => cp.Chat)
                .WithMany(c => c.Participants)
                .HasForeignKey(cp => cp.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessageRead>()
                .HasOne(mr => mr.Message)
                .WithMany(m => m.ReadReceipts)
                .HasForeignKey(mr => mr.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UnreadMessageCount>()
                .HasOne(umc => umc.Chat)
                .WithMany(c => c.UnreadMessageCounts)
                .HasForeignKey(umc => umc.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure array columns for PostgreSQL
            modelBuilder.Entity<Message>()
                .Property(m => m.MediaUrls)
                .HasColumnType("text[]");

            // Configure enums to be stored as strings
            modelBuilder.Entity<Chat>()
                .Property(c => c.ChatType)
                .HasConversion<string>();

            modelBuilder.Entity<Chat>()
                .Property(c => c.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Message>()
                .Property(m => m.Type)
                .HasConversion<string>();

            modelBuilder.Entity<Message>()
                .Property(m => m.Status)
                .HasConversion<string>();
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity.GetType().GetProperty("UpdatedAt") != null)
                {
                    entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                }
            }
        }
    }
} 