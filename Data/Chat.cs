namespace CorpLinkBaseMinimal.Data
{
    public class Chat
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public List<ChatParticipant> Participants { get; set; } = new();
        public List<Message> Messages { get; set; } = new();
    }
}