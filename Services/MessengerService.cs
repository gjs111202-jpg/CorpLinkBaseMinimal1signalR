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

    public async Task<List<User>> GetUsersAsync() => await _repository.GetUsersAsync();
    public async Task<User?> GetUserAsync(string userId) => await _repository.GetUserAsync(userId);

    public async Task<(bool Success, string Error, User? User)> CreateUserAsync(string userName, string displayName, string email, string password)
    {
        userName = userName.Trim();
        displayName = displayName.Trim();
        email = email.Trim();

        if (string.IsNullOrWhiteSpace(userName))
            return (false, "Введите имя пользователя.", null);
        if (string.IsNullOrWhiteSpace(password))
            return (false, "Введите пароль.", null);

        if (await _repository.IsUserNameExistsAsync(userName))
            return (false, "Пользователь с таким именем уже существует.", null);

        try
        {
            var user = await _repository.CreateUserAsync(userName, displayName, email, password);
            return (true, string.Empty, user);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    public async Task<List<Chat>> GetChatsForUserAsync(string userId)
        => await _repository.GetChatsForUserAsync(userId);

    public async Task<Chat?> GetChatForUserAsync(string userId, int chatId)
        => await _repository.GetChatForUserAsync(userId, chatId);

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
}