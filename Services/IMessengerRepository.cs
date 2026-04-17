using CorpLinkBaseMinimal.Data;

namespace CorpLinkBaseMinimal.Services;

public interface IMessengerRepository
{
    Task<List<User>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<User?> GetUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> IsUserNameExistsAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> IsEmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> FindUserByLoginOrEmailAsync(string search, CancellationToken cancellationToken = default);
    Task<User> CreateUserAsync(string userName, string displayName, string email, string password, CancellationToken cancellationToken = default);

    Task<List<Chat>> GetChatsForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<Chat?> GetChatForUserAsync(string userId, int chatId, CancellationToken cancellationToken = default);
    Task<bool> IsChatParticipantAsync(int chatId, string userId, CancellationToken cancellationToken = default);
    Task<Chat?> GetChatWithParticipantsAndMessagesAsync(int chatId, CancellationToken cancellationToken = default);

    Task<Chat> CreateDirectChatAsync(Chat chat, CancellationToken cancellationToken = default);
    Task<Message> AddMessageAsync(Message message, CancellationToken cancellationToken = default);
    Task UpdateChatUpdatedAtAsync(int chatId, DateTime updatedAt, CancellationToken cancellationToken = default);
    Task<Message?> LoadMessageWithSenderAsync(Message message, CancellationToken cancellationToken = default);

    Task<Message?> GetMessageOwnedByUserAsync(int chatId, int messageId, string senderId, CancellationToken cancellationToken = default);
    Task<bool> UpdateMessageTextAsync(int messageId, string newText, DateTime editedAtUtc, CancellationToken cancellationToken = default);
    Task<bool> DeleteMessageAsync(int messageId, CancellationToken cancellationToken = default);

    Task<List<User>> GetFavoritesAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> IsFavoriteAsync(string userId, string favoriteUserId, CancellationToken cancellationToken = default);
    Task AddFavoriteAsync(string userId, string favoriteUserId, CancellationToken cancellationToken = default);
    Task RemoveFavoriteAsync(string userId, string favoriteUserId, CancellationToken cancellationToken = default);
    Task<User?> GetUserWithProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateUserProfileAsync(string userId, string? displayName, string? bio, string? status, CancellationToken cancellationToken = default);
    Task<bool> DeleteChatAsync(int chatId, CancellationToken cancellationToken = default);
}
