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
            // --- 1-on-1 Chats ---
            //var directChats = await _context.ChatMessages
            //    .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            //    .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            //    .Select(g => g.OrderByDescending(m => m.CreatedAt).FirstOrDefault())
            //    .Join(_context.Users,
            //          msg => msg.SenderId == userId ? msg.ReceiverId : msg.SenderId,
            //          user => user.UserId,
            //          (msg, user) => new ChatListItemDto
            //          {
            //              ChatId = user.UserId,
            //              ChatName = user.Username,
            //              ProfileUrl = user.ProfileUrl,
            //              LastMessage = msg.Message,
            //              LastMessageTime = msg.CreatedAt,
            //              IsGroup = false
            //          })
            //    .ToListAsync();
            //        var groupedMessages = await _context.ChatMessages
            //.Where(m => m.SenderId == userId || m.ReceiverId == userId)
            //.GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            //.Select(g => g.OrderByDescending(m => m.CreatedAt).FirstOrDefault())
            //.ToListAsync();

            //        var directChats = groupedMessages
            //            .Join(_context.Users.AsEnumerable(),
            //                  msg => msg.SenderId == userId ? msg.ReceiverId : msg.SenderId,
            //                  user => user.UserId,
            //                  (msg, user) => new ChatListItemDto
            //                  {
            //                      ChatId = user.UserId,
            //                      ChatName = user.Username,
            //                      ProfileUrl = user.ProfileUrl,
            //                      LastMessage = msg.Message,
            //                      LastMessageTime = msg.CreatedAt,
            //                      IsGroup = false
            //                  })
            //            .ToList();


           
            // Step 1: Get all relevant chat messages involving the user
            var messages = await _context.ChatMessages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .ToListAsync();

            // Step 2: Group messages into conversations
            var grouped = messages
                .GroupBy(m => new
                {
                    User1 = Math.Min(m.SenderId, m.ReceiverId),
                    User2 = Math.Max(m.SenderId, m.ReceiverId)
                })
                .Select(g => g.OrderByDescending(m => m.CreatedAt).First()) // latest message per chat
                .ToList();

            // Step 3: Load user info for the other person in the chat
            var userIds = grouped
                .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToList();

            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToListAsync();

            // Step 4: Join and project
            var chatList = grouped
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
                .OrderByDescending(x => x.LastMessageTime)
                .ToList();



            // --- Group Chats ---
            //var groupIds = await _context.ChatGroupMembers
            //    .Where(m => m.UserId == userId)
            //    .Select(m => m.GroupId)
            //    .ToListAsync();

            //var groupMessages = await _context.ChatMessages
            //    .Where(m => groupIds.Contains(m.GroupId))
            //    .GroupBy(m => m.GroupId)
            //    .Select(g => g.OrderByDescending(m => m.CreatedAt).FirstOrDefault())
            //    .Join(_context.ChatGroups,
            //          msg => msg.GroupId,
            //          grp => grp.Id,
            //          (msg, grp) => new ChatListItemDto
            //          {
            //              ChatId = grp.Id,
            //              ChatName = grp.Name,
            //              ProfileUrl = "", // You can add group icon if supported
            //              LastMessage = msg.Message,
            //              LastMessageTime = msg.CreatedAt,
            //              IsGroup = true
            //          })
            //    .ToListAsync();

            //var allChats = directChats
            //    .Concat(groupMessages)
            //    .OrderByDescending(c => c.LastMessageTime)  
            //    .ToList();

            return chatList;
        }
    }

}
