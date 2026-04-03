using CorpLinkBaseMinimal.Data;
using CorpLinkBaseMinimal.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CorpLinkBaseMinimal.Services;

public class MessengerService
{
    private readonly IDbContextFactory<MessengerDbContext> _dbFactory;
    private readonly IHubContext<ChatHub> _hubContext;

    public MessengerService(
        IDbContextFactory<MessengerDbContext> dbFactory,
        IHubContext<ChatHub> hubContext)
    {
        _dbFactory = dbFactory;
        _hubContext = hubContext;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Users
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<User?> GetUserAsync(int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users.FirstOrDefaultAsync(x => x.Id == userId,default);
    }

    public async Task<(bool Success, string Error, User? User)> CreateUserAsync(string name)
    {
        var cleanName = name.Trim();

        if (string.IsNullOrWhiteSpace(cleanName))
        {
            return (false, "Введите имя пользователя.", null);
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        var exists = await db.Users.AnyAsync(x => x.Name.ToLower() == cleanName.ToLower());
        if (exists)
        {
            return (false, "Пользователь с таким именем уже существует.", null);
        }

        var user = new User
        {
            Name = cleanName
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return (true, string.Empty, user);
    }

    public async Task<List<Chat>> GetChatsForUserAsync(int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Chats
            .AsNoTracking()
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                .ThenInclude(m => m.Sender)
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Chat?> GetChatForUserAsync(int userId, int chatId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Chats
            .AsNoTracking()
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.Id == chatId && c.Participants.Any(p => p.UserId == userId));
    }

    public async Task<(bool Success, string Error, Chat? Chat)> CreateDirectChatAsync(int currentUserId, int otherUserId)
    {
        if (currentUserId == 0 || otherUserId == 0)
        {
            return (false, "Выбери двух пользователей.", null);
        }

        if (currentUserId == otherUserId)
        {
            return (false, "Нельзя создать чат с самим собой.", null);
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.Chats
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.Participants.Count == 2
                && c.Participants.Any(p => p.UserId == currentUserId)
                && c.Participants.Any(p => p.UserId == otherUserId));

        if (existing is not null)
        {
            return (true, string.Empty, existing);
        }

        var currentUser = await db.Users.FirstOrDefaultAsync(x => x.Id == currentUserId);
        var otherUser = await db.Users.FirstOrDefaultAsync(x => x.Id == otherUserId);

        if (currentUser is null || otherUser is null)
        {
            return (false, "Один из пользователей не найден.", null);
        }

        var chat = new Chat
        {
            Title = $"{currentUser.Name} / {otherUser.Name}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Participants = new List<ChatParticipant>
            {
                new() { UserId = currentUserId },
                new() { UserId = otherUserId }
            }
        };

        db.Chats.Add(chat);
        await db.SaveChangesAsync();

        var result = await ReloadChatAsync(db, chat.Id);

        if (result.Success && result.Chat is not null)
        {
            await _hubContext.Clients.Group($"chat-{result.Chat.Id}")
                .SendAsync("ChatListChanged", result.Chat.Id);
        }

        return result;
    }

    public async Task<(bool Success, string Error, Message? Message)> SendMessageAsync(int chatId, int senderId, string text)
    {
        var cleanText = text.Trim();

        if (string.IsNullOrWhiteSpace(cleanText))
        {
            return (false, "Введите текст сообщения.", null);
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        var chatExists = await db.Chats.AnyAsync(c => c.Id == chatId && c.Participants.Any(p => p.UserId == senderId));
        if (!chatExists)
        {
            return (false, "Чат не найден.", null);
        }

        var message = new Message
        {
            ChatId = chatId,
            SenderId = senderId,
            Text = cleanText,
            CreatedAt = DateTime.UtcNow
        };

        db.Messages.Add(message);

        var chat = await db.Chats.FirstAsync(c => c.Id == chatId);
        chat.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await db.Entry(message).Reference(m => m.Sender).LoadAsync();

        await _hubContext.Clients.Group($"chat-{chatId}")
            .SendAsync("ReceiveMessage", chatId);

        return (true, string.Empty, message);
    }

    private static async Task<(bool Success, string Error, Chat? Chat)> ReloadChatAsync(MessengerDbContext db, int chatId)
    {
        var chat = await db.Chats
            .AsNoTracking()
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.Id == chatId);

        return chat is null
            ? (false, "Не удалось загрузить чат.", null)
            : (true, string.Empty, chat);
    }
}