using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CorpLinkBaseMinimal.Data
{
    public class MessengerDbContext : IdentityDbContext<User>
    {
        public MessengerDbContext(DbContextOptions<MessengerDbContext> options)
            : base(options)
        {
        }

        public DbSet<Chat> Chats => Set<Chat>();
        public DbSet<ChatParticipant> ChatParticipants => Set<ChatParticipant>();
        public DbSet<Message> Messages => Set<Message>();

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
        }
    }
}