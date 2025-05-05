using messaging_app_backend.Data;
using messaging_app_backend.DTO;
using messaging_app_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace messaging_app_backend.Services
{
    public interface IChatListService
    {
        Task<List<ChatListDto>> GetUserChatsAsync(int userId);
        Task<ChatListDto> GetChatByIdAsync(int chatId, int userId);
        Task<bool> MarkChatAsReadAsync(int chatId, int userId);
        Task<List<UserDto>> GetContactsAsync(int userId);
        Task<ChatListDto> CreateIndividualChatAsync(int userId, int contactId);
        Task<ChatListDto> CreateGroupChatAsync(int userId, CreateGroupChatDto groupChatDto);
    }

    public class ChatListService : IChatListService
    {
        private readonly ChatAppDbContext _context;

        public ChatListService(ChatAppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ChatListDto>> GetUserChatsAsync(int userId)
        {
            // Get all chats where the user is a participant
            var userChats = await _context.ChatParticipants
                .Where(cp => cp.UserId == userId)
                .Include(cp => cp.Chat)
                .ThenInclude(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .ThenInclude(m => m.Sender)
                .Include(cp => cp.Chat)
                .ThenInclude(c => c.ChatParticipants)
                .ThenInclude(cp => cp.User)
                .ToListAsync();

            var chatDtos = new List<ChatListDto>();

            foreach (var chatParticipant in userChats)
            {
                var chat = chatParticipant.Chat;
                var lastMessage = chat.Messages.FirstOrDefault();

                var unreadCount = await _context.Messages
                    .CountAsync(m => m.ChatId == chat.ChatId &&
                                 m.SenderId != userId &&
                                 !_context.MessageReadStatus
                                    .Any(mrs => mrs.MessageId == m.MessageId && mrs.UserId == userId));

                var isGroup = chat.IsGroup;

                // For direct messages, get the other participant's name and profile
                string chatName = chat.Name;
                string profileUrl = "";

                if (!isGroup)
                {
                    var otherParticipant = chat.ChatParticipants
                        .FirstOrDefault(cp => cp.UserId != userId)?.User;

                    if (otherParticipant != null)
                    {
                        chatName = otherParticipant.Username;
                        profileUrl = otherParticipant.ProfileUrl;
                    }
                }

                chatDtos.Add(new ChatListDto
                {
                    Id = chat.ChatId,
                    Name = chatName,
                    Description = chat.Description,
                    IsGroup = isGroup,
                    ProfileUrl = isGroup ? chat.ProfileUrl : profileUrl,
                    MemberCount = chat.ChatParticipants.Count,
                    UnreadCount = unreadCount,
                    LastMessage = lastMessage != null ? new MessageDto
                    {
                        Id = lastMessage.MessageId,
                        Content = lastMessage.Content,
                        SentAt = lastMessage.CreatedAt,
                        SenderId = lastMessage.SenderId,
                        SenderName = lastMessage.Sender.Username
                    } : null
                });
            }

            // Order by most recent message
            return chatDtos.OrderByDescending(c => c.LastMessage?.SentAt ?? c.CreatedAt).ToList();
        }

        public async Task<ChatListDto> GetChatByIdAsync(int chatId, int userId)
        {
            // Verify user is a participant
            var isParticipant = await _context.ChatParticipants
                .AnyAsync(cp => cp.ChatId == chatId && cp.UserId == userId);

            if (!isParticipant)
            {
                return null; // User is not authorized to access this chat
            }

            var chat = await _context.Chats
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .ThenInclude(m => m.Sender)
                .Include(c => c.ChatParticipants)
                .ThenInclude(cp => cp.User)
                .FirstOrDefaultAsync(c => c.ChatId == chatId);

            if (chat == null)
            {
                return null;
            }

            var lastMessage = chat.Messages.FirstOrDefault();

            var unreadCount = await _context.Messages
                .CountAsync(m => m.ChatId == chatId &&
                             m.SenderId != userId &&
                             !_context.MessageReadStatus
                                .Any(mrs => mrs.MessageId == m.MessageId && mrs.UserId == userId));

            var isGroup = chat.IsGroup;

            // For direct messages, get the other participant's name and profile
            string chatName = chat.Name;
            string profileUrl = "";

            if (!isGroup)
            {
                var otherParticipant = chat.ChatParticipants
                    .FirstOrDefault(cp => cp.UserId != userId)?.User;

                if (otherParticipant != null)
                {
                    chatName = otherParticipant.Username;
                    profileUrl = otherParticipant.ProfileUrl;
                }
            }

            return new ChatListDto
            {
                Id = chat.ChatId,
                Name = chatName,
                Description = chat.Description,
                IsGroup = isGroup,
                ProfileUrl = isGroup ? chat.ProfileUrl : profileUrl,
                MemberCount = chat.ChatParticipants.Count,
                UnreadCount = unreadCount,
                LastMessage = lastMessage != null ? new MessageDto
                {
                    Id = lastMessage.MessageId,
                    Content = lastMessage.Content,
                    SentAt = lastMessage.CreatedAt,
                    SenderId = lastMessage.SenderId,
                    SenderName = lastMessage.Sender.Username
                } : null,
                CreatedAt = chat.CreatedAt
            };
        }

        public async Task<bool> MarkChatAsReadAsync(int chatId, int userId)
        {
            // Verify user is a participant
            var isParticipant = await _context.ChatParticipants
                .AnyAsync(cp => cp.ChatId == chatId && cp.UserId == userId);

            if (!isParticipant)
            {
                return false; // User is not authorized to access this chat
            }

            // Get all unread messages in this chat
            var unreadMessages = await _context.Messages
                .Where(m => m.ChatId == chatId &&
                       m.SenderId != userId &&
                       !_context.MessageReadStatus
                          .Any(mrs => mrs.MessageId == m.MessageId && mrs.UserId == userId))
                .ToListAsync();

            // Mark each message as read
            foreach (var message in unreadMessages)
            {
                _context.MessageReadStatus.Add(new MessageReadStatus
                {
                    MessageId = message.MessageId,
                    UserId = userId,
                    ReadAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<UserDto>> GetContactsAsync(int userId)
        {
            // Get users who share a direct (non-group) chat with the current user
            var contactIds = await _context.ChatParticipants
                .Where(cp => cp.UserId != userId &&
                       _context.ChatParticipants
                           .Any(cp2 => cp2.ChatId == cp.ChatId &&
                                  cp2.UserId == userId &&
                                  !cp2.Chat.IsGroup))
                .Select(cp => cp.UserId)
                .Distinct()
                .ToListAsync();

            // Get user details for these contacts
            var contacts = await _context.Users
                .Where(u => contactIds.Contains(u.UserId))
                .Select(u => new UserDto
                {
                    Id = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    PhoneNo = u.PhoneNo,
                    ProfileUrl = u.ProfileUrl
                })
                .ToListAsync();

            return contacts;
        }

        public async Task<ChatListDto> CreateIndividualChatAsync(int userId, int contactId)
        {
            // Check if chat already exists
            var existingChat = await _context.ChatParticipants
                .Where(cp => cp.UserId == userId)
                .Select(cp => cp.ChatId)
                .Intersect(
                    _context.ChatParticipants
                        .Where(cp => cp.UserId == contactId)
                        .Select(cp => cp.ChatId)
                )
                .Where(chatId => !_context.Chats
                    .Any(c => c.ChatId == chatId && c.IsGroup))
                .FirstOrDefaultAsync();

            if (existingChat != 0)
            {
                // Chat already exists, return it
                return await GetChatByIdAsync(existingChat, userId);
            }

            // Create new chat
            var newChat = new Chat
            {
                Name = "",  // For individual chats, we use the other person's name
                IsGroup = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Chats.Add(newChat);
            await _context.SaveChangesAsync();

            // Add participants
            _context.ChatParticipants.Add(new ChatParticipant
            {
                ChatId = newChat.ChatId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow,
                IsAdmin = true
            });

            _context.ChatParticipants.Add(new ChatParticipant
            {
                ChatId = newChat.ChatId,
                UserId = contactId,
                JoinedAt = DateTime.UtcNow,
                IsAdmin = false
            });

            await _context.SaveChangesAsync();

            // Return the new chat
            return await GetChatByIdAsync(newChat.ChatId, userId);
        }

        public async Task<ChatListDto> CreateGroupChatAsync(int userId, CreateGroupChatDto groupChatDto)
        {
            // Create new group chat
            var newChat = new Chat
            {
                Name = groupChatDto.Name,
                Description = groupChatDto.Description,
                IsGroup = true,
                ProfileUrl = groupChatDto.ProfileUrl ?? "",
                CreatedAt = DateTime.UtcNow
            };

            _context.Chats.Add(newChat);
            await _context.SaveChangesAsync();

            // Add creator as admin
            _context.ChatParticipants.Add(new ChatParticipant
            {
                ChatId = newChat.ChatId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow,
                IsAdmin = true
            });

            // Add other participants
            if (groupChatDto.ParticipantIds != null && groupChatDto.ParticipantIds.Any())
            {
                foreach (var participantId in groupChatDto.ParticipantIds)
                {
                    if (participantId != userId)
                    {
                        _context.ChatParticipants.Add(new ChatParticipant
                        {
                            ChatId = newChat.ChatId,
                            UserId = participantId,
                            JoinedAt = DateTime.UtcNow,
                            IsAdmin = false
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Return the new chat
            return await GetChatByIdAsync(newChat.ChatId, userId);
        }
    }
}