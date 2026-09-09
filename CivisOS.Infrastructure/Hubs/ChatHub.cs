using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CivisOS.Infrastructure.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public static string ConversationGroup(Guid conversationId) => $"conversation:{conversationId}";

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public async Task JoinConversation(Guid conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }

    public string? GetUserId() => Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
