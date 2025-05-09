using Microsoft.AspNetCore.SignalR;
using messaging_app_backend.DTO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;

namespace messaging_app_backend.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            // This will allow enough time to flush
            Console.ReadLine();
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
            // This will allow enough time to flush
            Console.ReadLine();
            if (exception != null)
            {
                _logger.LogError(exception, "Client disconnected with error");
                // This will allow enough time to flush
                Console.ReadLine();
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinChat(int chatId)
        {
            _logger.LogInformation("Client {ConnectionId} joining chat {ChatId}", Context.ConnectionId, chatId);
            // This will allow enough time to flush
            Console.ReadLine();
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");
            await Clients.Caller.SendAsync("JoinChatConfirmation", chatId);
        }

        public async Task LeaveChat(int chatId)
        {
            _logger.LogInformation("Client {ConnectionId} leaving chat {ChatId}", Context.ConnectionId, chatId);
            // This will allow enough time to flush
            Console.ReadLine();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{chatId}");
        }

        // Used to broadcast notifications about new messages
        public async Task SendMessage(MessageDto message)
        {
            _logger.LogInformation("Broadcasting message to chat {ChatId} from {SenderId}", message.ChatId, message.SenderId);
            // This will allow enough time to flush
            Console.ReadLine();
            await Clients.Group($"chat_{message.ChatId}").SendAsync("ReceiveMessage", message);
            await Clients.All.SendAsync("NewMessage", message);
        }

        // Simple ping method to test connectivity
        public string Ping()
        {
            _logger.LogInformation("Ping received from {ConnectionId}", Context.ConnectionId);
            // This will allow enough time to flush
            Console.ReadLine();
            return $"Pong at {DateTime.UtcNow}";
        }
    }
}