using CorpLinkBaseMinimal.Data;

namespace CorpLinkBaseMinimal.Services;

public interface IMessengerRepository
{
    Task<List<User>> GetUsersAsync();
    Task<User?> GetUserAsync(string userId);
    Task<bool> IsUserNameExistsAsync(string name);
    Task<User> CreateUserAsync(string userName, string displayName, string email, string password);

    Task<List<Chat>> GetChatsForUserAsync(string userId);
    Task<Chat?> GetChatForUserAsync(string userId, int chatId);
    Task<bool> IsChatParticipantAsync(int chatId, string userId);
    Task<Chat?> GetChatWithParticipantsAndMessagesAsync(int chatId);

    Task<Chat> CreateDirectChatAsync(Chat chat);
    Task<Message> AddMessageAsync(Message message);
    Task UpdateChatUpdatedAtAsync(int chatId, DateTime updatedAt);
    Task<Message?> LoadMessageWithSenderAsync(Message message);

    Task<Message?> GetMessageOwnedByUserAsync(int chatId, int messageId, string senderId);
    Task<bool> UpdateMessageTextAsync(int messageId, string newText, DateTime editedAtUtc);
    Task<bool> DeleteMessageAsync(int messageId);
}