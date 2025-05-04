using messaging_app_backend.Data;
using messaging_app_backend.DTO;
using messaging_app_backend.Models;
using Microsoft.EntityFrameworkCore;


namespace messaging_app_backend.Services
{
    public class ChatListService : IChatListService
    {
        private readonly ChatAppDbContext _context;

        public ChatListService(ChatAppDbContext context)
        {
            _context = context;
        }
            public async Task<List<ChatListItemDto>> GetUserChatsAsync(int userId)
        {
            // ------------------------------
            // 1. Get latest 1-on-1 messages
            // ------------------------------
            var directMessages = await _context.ChatMessages
                .Where(m => m.GroupId == null && (m.SenderId == userId || m.ReceiverId == userId))
                .GroupBy(m => new
                {
                    User1 = m.SenderId < m.ReceiverId ? m.SenderId : m.ReceiverId,
                    User2 = m.SenderId < m.ReceiverId ? m.ReceiverId : m.SenderId
                })
                .Select(g => g.OrderByDescending(m => m.CreatedAt).FirstOrDefault())
                .ToListAsync();

            var otherUserIds = directMessages
                .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToList();

            var users = await _context.Users
                .Where(u => otherUserIds.Contains(u.UserId))
                .ToListAsync();

            var directChats = directMessages
                .Select(m =>
                {
                    var otherUserId = m.SenderId == userId ? m.ReceiverId : m.SenderId;
                    var otherUser = users.FirstOrDefault(u => u.UserId == otherUserId);

                    return new ChatListItemDto
                    {
                        ChatId = otherUser?.UserId ?? 0,
                        ChatName = otherUser?.Username ?? "Unknown",
                        ProfileUrl = otherUser?.ProfileUrl,
                        LastMessage = m.Message,
                        LastMessageTime = m.CreatedAt,
                        IsGroup = false
                    };
                })
                .ToList();

            // ------------------------------
            // 2. Get latest group messages
            // ------------------------------
            var groupIds = await _context.ChatGroupMembers
                .Where(m => m.UserId == userId && m.IsActive)
                .Select(m => m.GroupId)
                .ToListAsync();

            var latestGroupMessages = await _context.ChatMessages
                .Where(m => m.GroupId != null && groupIds.Contains(m.GroupId.Value))
                .GroupBy(m => m.GroupId)
                .Select(g => g.OrderByDescending(m => m.CreatedAt).FirstOrDefault())
                .ToListAsync();

            var groups = await _context.ChatGroups
                .Where(g => groupIds.Contains(g.Id))
                .ToListAsync();

            var groupChats = groups
    .Select(group =>
    {
        var msg = latestGroupMessages.FirstOrDefault(m => m.GroupId == group.Id);

        return new ChatListItemDto
        {
            ChatId = group.Id,
            ChatName = group.Name,
            ProfileUrl = "", // Replace with group icon if available
            LastMessage = msg?.Message ?? "No messages yet",
            LastMessageTime = msg?.CreatedAt ?? group.CreatedAt, // Or use DateTime.MinValue
            IsGroup = true
        };
    })
    .ToList();


            // ------------------------------
            // 3. Combine and return all chats
            // ------------------------------
            var allChats = directChats
                .Concat(groupChats)
                .OrderByDescending(c => c.LastMessageTime)
                .ToList();
     
            return allChats;
        }

    }

}
