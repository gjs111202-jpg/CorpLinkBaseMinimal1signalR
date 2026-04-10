namespace CorpLinkBaseMinimal.Data
{
    public class ChatParticipant
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public Chat Chat { get; set; } = null!;

        public string UserId { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}