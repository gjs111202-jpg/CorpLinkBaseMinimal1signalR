using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CorpLinkBaseMinimal.Data
{
    public class MessengerDbContext : IdentityDbContext<User>
    {
        public MessengerDbContext(DbContextOptions<MessengerDbContext> options)
            : base(options) { }

        public DbSet<Chat> Chats => Set<Chat>();
        public DbSet<ChatParticipant> ChatParticipants => Set<ChatParticipant>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<Favorite> Favorites => Set<Favorite>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ChatParticipant>()
                .HasOne(x => x.Chat)
                .WithMany(x => x.Participants)
                .HasForeignKey(x => x.ChatId);

            builder.Entity<ChatParticipant>()
                .HasOne(x => x.User)
                .WithMany(x => x.ChatParticipants)
                .HasForeignKey(x => x.UserId);

            builder.Entity<Message>()
                .HasOne(x => x.Chat)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ChatId);

            builder.Entity<Message>()
                .HasOne(x => x.Sender)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.SenderId);

            builder.Entity<Favorite>(entity =>
            {
                entity.HasOne(f => f.User)
                    .WithMany(u => u.Favorites)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.FavoriteUser)
                    .WithMany(u => u.FavoriteOf)
                    .HasForeignKey(f => f.FavoriteUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(f => new { f.UserId, f.FavoriteUserId }).IsUnique();
            });
        }
    }
}