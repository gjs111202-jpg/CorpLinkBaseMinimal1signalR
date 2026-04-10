using Microsoft.EntityFrameworkCore;
using CorpLinkBaseMinimal.Data;
using Microsoft.AspNetCore.Identity;

namespace CorpLinkBaseMinimal.Services;

public class MessengerRepository : IMessengerRepository
{
    private readonly IDbContextFactory<MessengerDbContext> _dbFactory;
    private readonly UserManager<User> _userManager; // добавим для создания пользователей

    public MessengerRepository(IDbContextFactory<MessengerDbContext> dbFactory, UserManager<User> userManager)
    {
        _dbFactory = dbFactory;
        _userManager = userManager;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        
        return await db.Users.OrderBy(x => x.DisplayName ?? x.UserName).ToListAsync();
    }

    public async Task<User?> GetUserAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users.FirstOrDefaultAsync(x => x.Id == userId);
    }

    public async Task<bool> IsUserNameExistsAsync(string name)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        
        return await db.Users.AnyAsync(x => x.UserName!.ToLower() == name.ToLower());
    }

    public async Task<User> CreateUserAsync(string userName, string displayName, string email, string password)
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

    public async Task<List<Chat>> GetChatsForUserAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Chats
            .AsNoTracking()
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt)).ThenInclude(m => m.Sender)
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Chat?> GetChatForUserAsync(string userId, int chatId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Chats
            .AsNoTracking()
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt)).ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.Id == chatId && c.Participants.Any(p => p.UserId == userId));
    }

    public async Task<bool> IsChatParticipantAsync(int chatId, string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Chats.AnyAsync(c => c.Id == chatId && c.Participants.Any(p => p.UserId == userId));
    }

    public async Task<Chat?> GetChatWithParticipantsAndMessagesAsync(int chatId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Chats
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt)).ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.Id == chatId);
    }

    public async Task<Chat> CreateDirectChatAsync(Chat chat)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Chats.Add(chat);
        await db.SaveChangesAsync();
        return chat;
    }

    public async Task<Message> AddMessageAsync(Message message)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Messages.Add(message);
        await db.SaveChangesAsync();
        return message;
    }

    public async Task UpdateChatUpdatedAtAsync(int chatId, DateTime updatedAt)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var chat = await db.Chats.FirstAsync(c => c.Id == chatId);
        chat.UpdatedAt = updatedAt;
        await db.SaveChangesAsync();
    }

    public async Task<Message?> LoadMessageWithSenderAsync(Message message)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Entry(message).Reference(m => m.Sender).LoadAsync();
        return message;
    }

    public async Task<Message?> GetMessageOwnedByUserAsync(int chatId, int messageId, string senderId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Messages
            .AsTracking()
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ChatId == chatId && m.SenderId == senderId);
    }

    public async Task<bool> UpdateMessageTextAsync(int messageId, string newText, DateTime editedAtUtc)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var msg = await db.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
        if (msg is null)
            return false;
        msg.Text = newText;
        msg.EditedAt = editedAtUtc;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMessageAsync(int messageId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var msg = await db.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
        if (msg is null)
            return false;
        db.Messages.Remove(msg);
        await db.SaveChangesAsync();
        return true;
    }
}