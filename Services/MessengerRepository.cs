using Microsoft.EntityFrameworkCore;
using CorpLinkBaseMinimal.Data;

namespace CorpLinkBaseMinimal.Services;

public class MessengerRepository : IMessengerRepository
{
    private readonly IDbContextFactory<MessengerDbContext> _dbFactory;

    public MessengerRepository(IDbContextFactory<MessengerDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<User?> GetUserAsync(int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users.FirstOrDefaultAsync(x => x.Id == userId);
    }

    public async Task<bool> IsUserNameExistsAsync(string name)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users.AnyAsync(x => x.Name.ToLower() == name.ToLower());
    }

    public async Task<User> CreateUserAsync(string name)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = new User { Name = name };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<List<Chat>> GetChatsForUserAsync(int userId)
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

    public async Task<Chat?> GetChatForUserAsync(int userId, int chatId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Chats
            .AsNoTracking()
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt)).ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.Id == chatId && c.Participants.Any(p => p.UserId == userId));
    }

    public async Task<bool> IsChatParticipantAsync(int chatId, int userId)
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
}