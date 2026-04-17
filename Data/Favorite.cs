namespace CorpLinkBaseMinimal.Data;

public class Favorite
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public string FavoriteUserId { get; set; } = null!;
    public virtual User FavoriteUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}