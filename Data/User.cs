using Microsoft.AspNetCore.Identity;

namespace CorpLinkBaseMinimal.Data
{
    public class User : IdentityUser
    {
        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public string? Status { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<ChatParticipant> ChatParticipants { get; set; } = new List<ChatParticipant>();
        public virtual ICollection<Message> Messages { get; set; } = [];

        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public virtual ICollection<Favorite> FavoriteOf { get; set; } = new List<Favorite>();
    }
}