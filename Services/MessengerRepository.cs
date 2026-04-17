using Microsoft.EntityFrameworkCore;
using CorpLinkBaseMinimal.Data;
using Microsoft.AspNetCore.Identity;

namespace CorpLinkBaseMinimal.Services;

public class MessengerRepository : IMessengerRepository
{
    private readonly IDbContextFactory<MessengerDbContext> _dbFactory;
    private readonly UserManager<User> _userManager;

    public MessengerRepository(IDbContextFactory<MessengerDbContext> dbFactory, UserManager<User> userManager)
    {
        _dbFactory = dbFactory;
        _userManager = userManager;
    }

    public async Task<List<User>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Users.OrderBy(x => x.DisplayName ?? x.UserName).ToListAsync(cancellationToken);
    }

    public async Task<User?> GetUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public async Task<bool> IsUserNameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Users.AnyAsync(x => x.UserName!.ToLower() == name.ToLower(), cancellationToken);
    }

    public async Task<bool> IsEmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Users.AnyAsync(x => x.Email!.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<User?> FindUserByLoginOrEmailAsync(string search, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Users
            .FirstOrDefaultAsync(u => u.UserName!.ToLower() == search.ToLower()
                                   || u.Email!.ToLower() == search.ToLower(),
                                   cancellationToken);
    }

    public async Task<User> CreateUserAsync(string userName, string displayName, string email, string password, CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            UserName = userName,
            Email = email,
            DisplayName = displayName
        };
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        return user;
    }

    public async Task<List<Chat>> GetChatsForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Chats
            .AsNoTracking()
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt)).ThenInclude(m => m.Sender)
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Chat?> GetChatForUserAsync(string userId, int chatId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Chats
            .AsNoTracking()
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt)).ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.Id == chatId && c.Participants.Any(p => p.UserId == userId), cancellationToken);
    }

    public async Task<bool> IsChatParticipantAsync(int chatId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Chats.AnyAsync(c => c.Id == chatId && c.Participants.Any(p => p.UserId == userId), cancellationToken);
    }

    public async Task<Chat?> GetChatWithParticipantsAndMessagesAsync(int chatId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Chats
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt)).ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.Id == chatId, cancellationToken);
    }

    public async Task<Chat> CreateDirectChatAsync(Chat chat, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        db.Chats.Add(chat);
        await db.SaveChangesAsync(cancellationToken);
        return chat;
    }

    public async Task<Message> AddMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        db.Messages.Add(message);
        await db.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task UpdateChatUpdatedAtAsync(int chatId, DateTime updatedAt, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var chat = await db.Chats.FirstAsync(c => c.Id == chatId, cancellationToken);
        chat.UpdatedAt = updatedAt;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Message?> LoadMessageWithSenderAsync(Message message, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await db.Entry(message).Reference(m => m.Sender).LoadAsync(cancellationToken);
        return message;
    }

    public async Task<Message?> GetMessageOwnedByUserAsync(int chatId, int messageId, string senderId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Messages
            .AsTracking()
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ChatId == chatId && m.SenderId == senderId, cancellationToken);
    }

    public async Task<bool> UpdateMessageTextAsync(int messageId, string newText, DateTime editedAtUtc, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var msg = await db.Messages.FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        if (msg is null) return false;
        msg.Text = newText;
        msg.EditedAt = editedAtUtc;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteMessageAsync(int messageId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var msg = await db.Messages.FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        if (msg is null) return false;
        db.Messages.Remove(msg);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // === Новые методы для избранного и профиля ===
    public async Task<List<User>> GetFavoritesAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Favorites
            .Where(f => f.UserId == userId)
            .Include(f => f.FavoriteUser)
            .Select(f => f.FavoriteUser)
            .OrderBy(u => u.DisplayName ?? u.UserName)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsFavoriteAsync(string userId, string favoriteUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Favorites.AnyAsync(f => f.UserId == userId && f.FavoriteUserId == favoriteUserId, cancellationToken);
    }

    public async Task AddFavoriteAsync(string userId, string favoriteUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        if (await IsFavoriteAsync(userId, favoriteUserId, cancellationToken))
            return;
        db.Favorites.Add(new Favorite { UserId = userId, FavoriteUserId = favoriteUserId });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFavoriteAsync(string userId, string favoriteUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var fav = await db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.FavoriteUserId == favoriteUserId, cancellationToken);
        if (fav is not null)
        {
            db.Favorites.Remove(fav);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<User?> GetUserWithProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new User
            {
                Id = u.Id,
                UserName = u.UserName,
                DisplayName = u.DisplayName,
                Email = u.Email,
                Bio = u.Bio,
                Status = u.Status,
                AvatarUrl = u.AvatarUrl,
                CreatedAt = u.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateUserProfileAsync(string userId, string? displayName, string? bio, string? status, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return;
        user.DisplayName = displayName;
        user.Bio = bio;
        user.Status = status;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteChatAsync(int chatId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var chat = await db.Chats
            .Include(c => c.Messages)
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == chatId, cancellationToken);
        if (chat is null) return false;

        db.Chats.Remove(chat);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}