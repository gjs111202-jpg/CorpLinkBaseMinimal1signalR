using CorpLinkBaseMinimal.Data;
using CorpLinkBaseMinimal.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CorpLinkBaseMinimal.Services;

public class MessengerService
{
    private readonly IMessengerRepository _repository;
    private readonly IHubContext<ChatHub> _hubContext;

    public MessengerService(IMessengerRepository repository, IHubContext<ChatHub> hubContext)
    {
        _repository = repository;
        _hubContext = hubContext;
    }

    public Task<List<User>> GetUsersAsync(CancellationToken cancellationToken = default)
        => _repository.GetUsersAsync(cancellationToken);

    public Task<User?> GetUserAsync(string userId, CancellationToken cancellationToken = default)
        => _repository.GetUserAsync(userId, cancellationToken);

    public async Task<(bool Success, RegistrationError ErrorCode, string ErrorMessage, User? User)>
        CreateUserAsync(string userName, string displayName, string email, string password)
    {
        userName = userName.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? userName : displayName.Trim();
        email = email.Trim();

        if (string.IsNullOrWhiteSpace(userName))
            return (false, RegistrationError.UsernameEmpty, "Имя пользователя не может быть пустым.", null);
        if (string.IsNullOrWhiteSpace(password))
            return (false, RegistrationError.PasswordEmpty, "Пароль не может быть пустым.", null);
        if (string.IsNullOrWhiteSpace(email))
            return (false, RegistrationError.EmailEmpty, "Email не может быть пустым.", null);

        if (!IsValidEmail(email))
            return (false, RegistrationError.EmailInvalid, "Введите корректный email.", null);

        if (await _repository.IsUserNameExistsAsync(userName))
            return (false, RegistrationError.UsernameTaken, "Пользователь с таким именем уже существует.", null);
        if (await _repository.IsEmailExistsAsync(email))
            return (false, RegistrationError.EmailTaken, "Пользователь с таким email уже существует.", null);

        try
        {
            var user = await _repository.CreateUserAsync(userName, displayName, email, password);
            return (true, RegistrationError.None, string.Empty, user);
        }
        catch (Exception ex)
        {
            return (false, RegistrationError.PasswordTooWeak, ex.Message, null);
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public Task<List<Chat>> GetChatsForUserAsync(string userId)
        => _repository.GetChatsForUserAsync(userId);

    public Task<Chat?> GetChatForUserAsync(string userId, int chatId, CancellationToken cancellationToken = default)
        => _repository.GetChatForUserAsync(userId, chatId, cancellationToken);

    public async Task<(bool Success, string Error, Chat? Chat)> CreateDirectChatAsync(string currentUserId, string otherUserId)
    {
        if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(otherUserId))
            return (false, "Выберите двух пользователей.", null);
        if (currentUserId == otherUserId)
            return (false, "Нельзя создать чат с самим собой.", null);

        var existing = (await _repository.GetChatsForUserAsync(currentUserId))
            .FirstOrDefault(c => c.Participants.Count == 2
                && c.Participants.Any(p => p.UserId == currentUserId)
                && c.Participants.Any(p => p.UserId == otherUserId));
        if (existing != null)
            return (true, string.Empty, existing);

        var currentUser = await _repository.GetUserAsync(currentUserId);
        var otherUser = await _repository.GetUserAsync(otherUserId);
        if (currentUser == null || otherUser == null)
            return (false, "Один из пользователей не найден.", null);

        var chat = new Chat
        {
            Title = $"{currentUser.DisplayName ?? currentUser.UserName} / {otherUser.DisplayName ?? otherUser.UserName}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Participants = new List<ChatParticipant>
            {
                new() { UserId = currentUserId },
                new() { UserId = otherUserId }
            }
        };

        await _repository.CreateDirectChatAsync(chat);
        var createdChat = await _repository.GetChatWithParticipantsAndMessagesAsync(chat.Id);
        await _hubContext.Clients.Group($"chat-{createdChat!.Id}").SendAsync("ChatListChanged", createdChat.Id);
        return (true, string.Empty, createdChat);
    }

    public async Task<(bool Success, string Error, Chat? Chat)> CreateDirectChatBySearchAsync(string currentUserId, string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
            return (false, "Введите логин или email пользователя.", null);

        var otherUser = await _repository.FindUserByLoginOrEmailAsync(searchQuery);
        if (otherUser == null)
            return (false, "Пользователь с таким логином или email не найден.", null);

        if (currentUserId == otherUser.Id)
            return (false, "Нельзя создать чат с самим собой.", null);

        var existing = (await _repository.GetChatsForUserAsync(currentUserId))
            .FirstOrDefault(c => c.Participants.Count == 2
                && c.Participants.Any(p => p.UserId == currentUserId)
                && c.Participants.Any(p => p.UserId == otherUser.Id));

        if (existing != null)
            return (true, string.Empty, existing);

        var currentUser = await _repository.GetUserAsync(currentUserId);
        if (currentUser == null)
            return (false, "Текущий пользователь не найден.", null);

        var chat = new Chat
        {
            Title = $"{currentUser.DisplayName ?? currentUser.UserName} / {otherUser.DisplayName ?? otherUser.UserName}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Participants = new List<ChatParticipant>
            {
                new() { UserId = currentUserId },
                new() { UserId = otherUser.Id }
            }
        };

        await _repository.CreateDirectChatAsync(chat);
        var createdChat = await _repository.GetChatWithParticipantsAndMessagesAsync(chat.Id);
        await _hubContext.Clients.Group($"chat-{createdChat!.Id}").SendAsync("ChatListChanged", createdChat.Id);
        return (true, string.Empty, createdChat);
    }

    public async Task<(bool Success, string Error, Message? Message)> SendMessageAsync(int chatId, string senderId, string text)
    {
        var cleanText = text.Trim();
        if (string.IsNullOrWhiteSpace(cleanText))
            return (false, "Введите текст сообщения.", null);

        if (!await _repository.IsChatParticipantAsync(chatId, senderId))
            return (false, "Чат не найден.", null);

        var message = new Message
        {
            ChatId = chatId,
            SenderId = senderId,
            Text = cleanText,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddMessageAsync(message);
        await _repository.UpdateChatUpdatedAtAsync(chatId, DateTime.UtcNow);
        await _repository.LoadMessageWithSenderAsync(message);

        await _hubContext.Clients.Group($"chat-{chatId}").SendAsync("ReceiveMessage", chatId);
        return (true, string.Empty, message);
    }

    public async Task<(bool Success, string Error)> UpdateMessageAsync(int chatId, int messageId, string userId, string text)
    {
        var cleanText = text.Trim();
        if (string.IsNullOrWhiteSpace(cleanText))
            return (false, "Введите текст сообщения.");

        if (!await _repository.IsChatParticipantAsync(chatId, userId))
            return (false, "Нет доступа к чату.");

        var owned = await _repository.GetMessageOwnedByUserAsync(chatId, messageId, userId);
        if (owned is null)
            return (false, "Сообщение не найдено или вы не можете его изменить.");

        var ok = await _repository.UpdateMessageTextAsync(messageId, cleanText, DateTime.UtcNow);
        if (!ok)
            return (false, "Не удалось сохранить изменения.");

        await _repository.UpdateChatUpdatedAtAsync(chatId, DateTime.UtcNow);
        await _hubContext.Clients.Group($"chat-{chatId}").SendAsync("ReceiveMessage", chatId);
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> DeleteMessageAsync(int chatId, int messageId, string userId)
    {
        if (!await _repository.IsChatParticipantAsync(chatId, userId))
            return (false, "Нет доступа к чату.");

        var owned = await _repository.GetMessageOwnedByUserAsync(chatId, messageId, userId);
        if (owned is null)
            return (false, "Сообщение не найдено или вы не можете его удалить.");

        var ok = await _repository.DeleteMessageAsync(messageId);
        if (!ok)
            return (false, "Не удалось удалить сообщение.");

        await _repository.UpdateChatUpdatedAtAsync(chatId, DateTime.UtcNow);
        await _hubContext.Clients.Group($"chat-{chatId}").SendAsync("ReceiveMessage", chatId);
        return (true, string.Empty);
    }
}