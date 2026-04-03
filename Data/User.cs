using Microsoft.AspNetCore.Identity;

namespace CorpLinkBaseMinimal.Data
{
    public class User : IdentityUser
    {
        public string? DisplayName { get; set; }
        public virtual ICollection<ChatParticipant> ChatParticipants { get; set; } = new List<ChatParticipant>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}