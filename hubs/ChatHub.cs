using Microsoft.AspNetCore.SignalR;

namespace CorpLinkBaseMinimal.Hubs
{
    public class ChatHub : Hub
    {
        public async Task JoinChat(string chatGroup)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chatGroup);
        }

        public async Task LeaveChat(string chatGroup)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatGroup);
        }
    }
}