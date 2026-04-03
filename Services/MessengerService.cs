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

    public async Task<User?> GetUserAsync(int userId) => await _repository.GetUserAsync(userId);

    public async Task<(bool Success, string Error, User? User)> CreateUserAsync(string name)
    {
        var cleanName = name.Trim();
        if (string.IsNullOrWhiteSpace(cleanName))
            return (false, "Введите имя пользователя.", null);

        if (await _repository.IsUserNameExistsAsync(cleanName))
            return (false, "Пользователь с таким именем уже существует.", null);

        var user = await _repository.CreateUserAsync(cleanName);
        return (true, string.Empty, user);
    }

    public async Task<List<Chat>> GetChatsForUserAsync(int userId)
        => await _repository.GetChatsForUserAsync(userId);

    public async Task<Chat?> GetChatForUserAsync(int userId, int chatId)
        => await _repository.GetChatForUserAsync(userId, chatId);

    public async Task<(bool Success, string Error, Chat? Chat)> CreateDirectChatAsync(int currentUserId, int otherUserId)
    {
        if (currentUserId == 0 || otherUserId == 0)
            return (false, "Выбери двух пользователей.", null);
        if (currentUserId == otherUserId)
            return (false, "Нельзя создать чат с самим собой.", null);

        // Проверяем, не существует ли уже такой чат
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
            Title = $"{currentUser.Name} / {otherUser.Name}",
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

    public async Task<(bool Success, string Error, Message? Message)> SendMessageAsync(int chatId, int senderId, string text)
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