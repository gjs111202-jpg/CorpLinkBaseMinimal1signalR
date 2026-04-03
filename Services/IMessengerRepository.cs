using CorpLinkBaseMinimal.Data;

namespace CorpLinkBaseMinimal.Services;

public interface IMessengerRepository
{
    Task<List<User>> GetUsersAsync();
    Task<User?> GetUserAsync(int userId);
    Task<bool> IsUserNameExistsAsync(string name);
    Task<User> CreateUserAsync(string name);

    Task<List<Chat>> GetChatsForUserAsync(int userId);
    Task<Chat?> GetChatForUserAsync(int userId, int chatId);
    Task<bool> IsChatParticipantAsync(int chatId, int userId);
    Task<Chat?> GetChatWithParticipantsAndMessagesAsync(int chatId);

    Task<Chat> CreateDirectChatAsync(Chat chat);
    Task<Message> AddMessageAsync(Message message);
    Task UpdateChatUpdatedAtAsync(int chatId, DateTime updatedAt);
    Task<Message?> LoadMessageWithSenderAsync(Message message);
}