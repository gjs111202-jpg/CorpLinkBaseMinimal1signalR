namespace CorpLinkBaseMinimal.Data
{
    public class Message
    {
        public int Id { get; init; }
        public int ChatId { get; set; }
        public virtual Chat Chat { get; set; } = null!;

        public  string SenderId { get; set; } = null!;
        public User Sender { get; set; } = null!;

        public string Text { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EditedAt { get; set; }
    }
}