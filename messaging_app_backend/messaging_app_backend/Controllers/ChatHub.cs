using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace messaging_app_backend.Controllers
{
    public class ChatHub : Hub
    {
        // Send message to a specific user
        public async Task SendMessageToUser(string receiverUserId, object message)
        {
            await Clients.User(receiverUserId).SendAsync("ReceiveMessage", message);
        }

        // Send message to a group (for group chats)
        public async Task SendMessageToGroup(string groupId, object message)
        {
            await Clients.Group(groupId).SendAsync("ReceiveMessage", message);
        }

        // Typing indicator
        public async Task SendTyping(string receiverUserId)
        {
            await Clients.User(receiverUserId).SendAsync("Typing", Context.UserIdentifier);
        }

        // Join SignalR group (for group chats)
        public async Task JoinGroup(string groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        }

        // Leave SignalR group
        public async Task LeaveGroup(string groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        }
    }

}
