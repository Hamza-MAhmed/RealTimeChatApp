using messaging_app_backend.Models;
using messaging_app_backend.DTO;
using messaging_app_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using messaging_app_backend.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using messaging_app_backend.Hubs;

namespace messaging_app_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatsController : ControllerBase
    {
        private readonly IHubContext<ChatHub> _hubContext; // for Real time updates
        private readonly IChatListService _chatListService;
        private readonly ILogger<ChatsController> _logger;
        private readonly ChatAppDbContext _context;

        public ChatsController(
            ChatAppDbContext context,
            IChatListService chatListService,
            ILogger<ChatsController> logger,
            IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _chatListService = chatListService;
            _logger = logger;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Get all chats for the current user
        /// </summary>
        [HttpGet("user-chats")]
        public async Task<IActionResult> GetUserChats()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                _logger.LogInformation("Getting chats for user {UserId}", userId);

                // Get the chat list from the service
                var chatList = await _chatListService.GetUserChatsAsync(userId);
                _logger.LogInformation("Initial chat list count: {Count}", chatList.Count);

                // Log each chat from the service
                foreach (var chat in chatList)
                {
                    _logger.LogInformation("Chat from service - Id: {Id}, Name: {Name}, IsGroup: {IsGroup}",
                        chat.Id, chat.Name, chat.IsGroup);
                }

                // Get participants for all chats
                var chatIds = chatList.Select(c => c.Id).ToList();
                _logger.LogInformation("Getting participants for {Count} chats", chatIds.Count);

                // Dictionary to store chat participants
                var chatParticipants = new Dictionary<int, List<ChatParticipantDto>>();

                // Get all participants from the database
                var allParticipants = await _context.ChatParticipants
                    .Where(cp => chatIds.Contains(cp.ChatId))
                    .Include(cp => cp.User)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} participants for all chats", allParticipants.Count);

                // Group participants by chat
                foreach (var participant in allParticipants)
                {
                    if (!chatParticipants.ContainsKey(participant.ChatId))
                    {
                        chatParticipants[participant.ChatId] = new List<ChatParticipantDto>();
                    }

                    chatParticipants[participant.ChatId].Add(new ChatParticipantDto
                    {
                        UserId = participant.UserId,
                        Username = participant.User.Username,
                        Email = participant.User.Email,
                        ProfileUrl = participant.User.ProfileUrl,
                        IsAdmin = participant.IsAdmin,
                        JoinedAt = participant.JoinedAt
                    });
                }

                // Convert ChatListDto objects to ChatDto objects
                var chatDtos = new List<ChatDto>();

                foreach (var chat in chatList)
                {
                    // For one-to-one chats, ensure the name is set to the other participant's name
                    string chatName = chat.Name;
                    string profileUrl = chat.ProfileUrl ?? "";

                    _logger.LogInformation("Processing chat {Id}, IsGroup: {IsGroup}, Initial name: {Name}",
                        chat.Id, chat.IsGroup, chatName);

                    // If this is a one-to-one chat, ensure we have the correct name
                    if (!chat.IsGroup)
                    {
                        if (chatParticipants.TryGetValue(chat.Id, out var chatMembers))
                        {
                            _logger.LogInformation("Found {Count} participants for chat {Id}",
                                chatMembers.Count, chat.Id);

                            var otherParticipant = chatMembers.FirstOrDefault(p => p.UserId != userId);
                            if (otherParticipant != null)
                            {
                                chatName = otherParticipant.Username;
                                profileUrl = otherParticipant.ProfileUrl;
                                _logger.LogInformation("Setting chat name to {Name} from participant {UserId}",
                                    chatName, otherParticipant.UserId);
                            }
                            else
                            {
                                _logger.LogWarning("No other participant found for chat {Id}", chat.Id);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("No participants found for chat {Id}", chat.Id);
                        }
                    }

                    // Create the ChatDto with proper name
                    var chatDto = new ChatDto
                    {
                        ChatId = chat.Id,
                        Name = chatName,
                        Description = chat.Description,
                        IsGroup = chat.IsGroup,
                        ProfileUrl = profileUrl,
                        CreatedAt = chat.CreatedAt,
                        UpdatedAt = chat.CreatedAt,
                        LastMessage = chat.LastMessage,
                        Participants = chatParticipants.TryGetValue(chat.Id, out var memberList)
                            ? memberList
                            : new List<ChatParticipantDto>()
                    };

                    chatDtos.Add(chatDto);

                    _logger.LogInformation("Added chat to result - Id: {Id}, Name: {Name}, IsGroup: {IsGroup}",
                        chatDto.ChatId, chatDto.Name, chatDto.IsGroup);
                }

                // Calculate unread message count for all the chats
                foreach (var chat in chatDtos)
                {
                    // Calculate unread message count for this chat
                    var lastReadTimestamp = await _context.UserChatRead
                        .Where(r => r.UserId == userId && r.ChatId == chat.ChatId)
                        .Select(r => r.LastReadAt)
                        .FirstOrDefaultAsync();

                    // Default to chat creation time if never read
                    if (lastReadTimestamp == default)
                    {
                        lastReadTimestamp = chat.CreatedAt;
                    }

                    // Count messages newer than last read time
                    var unreadCount = await _context.Messages
                        .Where(m => m.ChatId == chat.ChatId &&
                               m.SenderId != userId && // Don't count user's own messages
                               m.CreatedAt > lastReadTimestamp)
                        .CountAsync();

                    chat.UnreadCount = unreadCount;
                }

                return Ok(chatDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user chats");
                return StatusCode(500, new { message = "An error occurred while retrieving chats" });
            }
        }

        /// <summary>
        /// Get a specific chat by ID
        /// </summary>
        [HttpGet("{chatId}")]
        public async Task<IActionResult> GetChat(int chatId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                _logger.LogInformation("Getting chat {ChatId} for user {UserId}", chatId, userId);

                var chat = await _chatListService.GetChatByIdAsync(chatId, userId);

                if (chat == null)
                {
                    return NotFound(new { message = "Chat not found or you don't have access" });
                }

                return Ok(chat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat {ChatId}", chatId);
                return StatusCode(500, new { message = "An error occurred while retrieving the chat" });
            }
        }

        /// <summary>
        /// Mark a chat as read
        /// </summary>
        [HttpPost("{chatId}/read")]
        public async Task<IActionResult> MarkChatAsRead(int chatId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                _logger.LogInformation("Marking chat {ChatId} as read for user {UserId}", chatId, userId);

                var success = await _chatListService.MarkChatAsReadAsync(chatId, userId);

                if (!success)
                {
                    return NotFound(new { message = "Chat not found or you don't have access" });
                }

                return Ok(new { message = "Chat marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking chat {ChatId} as read", chatId);
                return StatusCode(500, new { message = "An error occurred while marking the chat as read" });
            }
        }

        /// <summary>
        /// Get user contacts
        /// </summary>
        [HttpGet("contacts")]
        public async Task<IActionResult> GetContacts()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                _logger.LogInformation("Getting contacts for user {UserId}", userId);

                var contacts = await _chatListService.GetContactsAsync(userId);
                return Ok(contacts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contacts");
                return StatusCode(500, new { message = "An error occurred while retrieving contacts" });
            }
        }

        /// <summary>
        /// Get users available for new chats (users that don't have an existing one-to-one chat with current user)
        /// </summary>
        [HttpGet("available-contacts")]
        public async Task<IActionResult> GetAvailableContacts()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                _logger.LogInformation("Getting available contacts for user {UserId}", userId);

                // Get all users
                var allUsers = await _context.Users
                    .Where(u => u.UserId != userId) // Exclude current user
                    .ToListAsync();

                // Get all one-to-one chats the current user is participating in
                var existingOneToOneChats = await _context.ChatParticipants
                    .Include(p => p.Chat)
                    .Where(p => p.UserId == userId && !p.Chat.IsGroup)
                    .Select(p => p.ChatId)
                    .ToListAsync();

                // Get user IDs that already have a one-to-one chat with current user
                var usersWithExistingChats = new List<int>();

                foreach (var chatId in existingOneToOneChats)
                {
                    // Check if this is a one-to-one chat (has exactly 2 participants)
                    var participantCount = await _context.ChatParticipants
                        .CountAsync(p => p.ChatId == chatId);

                    if (participantCount == 2)
                    {
                        // Get the other user's ID
                        var otherUserId = await _context.ChatParticipants
                            .Where(p => p.ChatId == chatId && p.UserId != userId)
                            .Select(p => p.UserId)
                            .FirstOrDefaultAsync();

                        if (otherUserId != 0)
                        {
                            usersWithExistingChats.Add(otherUserId);
                        }
                    }
                }

                // Filter out users that already have a one-to-one chat with current user
                var availableContacts = allUsers
                    .Where(u => !usersWithExistingChats.Contains(u.UserId))
                    .Select(u => new UserDto
                    {
                        Id = u.UserId,
                        Username = u.Username,
                        Email = u.Email,
                        ProfileUrl = u.ProfileUrl
                    })
                    .ToList();

                return Ok(availableContacts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available contacts");
                return StatusCode(500, new { message = "An error occurred while retrieving available contacts" });
            }
        }

        /// <summary>
        /// Create a new individual chat with another user
        /// </summary>
        [HttpPost("individual")]
        public async Task<IActionResult> CreateIndividualChat([FromBody] int contactId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                _logger.LogInformation("Creating individual chat between user {UserId} and contact {ContactId}", userId, contactId);

                if (contactId == userId)
                {
                    return BadRequest(new { message = "Cannot create a chat with yourself" });
                }

                var chat = await _chatListService.CreateIndividualChatAsync(userId, contactId);
                return Ok(chat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating individual chat");
                return StatusCode(500, new { message = "An error occurred while creating the chat" });
            }
        }

        /// <summary>
        /// Create a new group chat
        /// </summary>
        [HttpPost("group")]
        public async Task<IActionResult> CreateGroupChat([FromBody] CreateGroupChatDto groupChatDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                _logger.LogInformation("Creating group chat '{Name}' for user {UserId}", groupChatDto.Name, userId);

                var chat = await _chatListService.CreateGroupChatAsync(userId, groupChatDto);
                return Ok(chat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group chat");
                return StatusCode(500, new { message = "An error occurred while creating the group chat" });
            }
        }

        // GET: api/Chats
        [HttpGet]
        public async Task<IActionResult> GetChats()
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                _logger.LogInformation("Getting chats for user {UserId}", userId);

                var userChats = await _context.ChatParticipants
                    .Where(p => p.UserId == userId)
                    .Include(p => p.Chat)
                    .ThenInclude(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                    .ThenInclude(m => m.Sender)
                    .Include(p => p.Chat)
                    .ThenInclude(c => c.ChatParticipants)
                    .ThenInclude(p => p.User)
                    .ToListAsync();

                var chatsDto = userChats.Select(cp => new ChatDto
                {
                    ChatId = cp.ChatId,
                    Name = cp.Chat.Name,
                    Description = cp.Chat.Description,
                    IsGroup = cp.Chat.IsGroup,
                    ProfileUrl = cp.Chat.ProfileUrl,
                    CreatedAt = cp.Chat.CreatedAt,
                    UpdatedAt = cp.Chat.UpdatedAt,
                    Participants = cp.Chat.ChatParticipants.Select(p => new ChatParticipantDto
                    {
                        UserId = p.UserId,
                        Username = p.User.Username,
                        Email = p.User.Email,
                        ProfileUrl = p.User.ProfileUrl,
                        IsAdmin = p.IsAdmin,
                        JoinedAt = p.JoinedAt
                    }).ToList(),
                    LastMessage = cp.Chat.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault() != null
                        ? new MessageDto
                        {
                            MessageId = cp.Chat.Messages.OrderByDescending(m => m.CreatedAt).First().MessageId,
                            ChatId = cp.Chat.Messages.OrderByDescending(m => m.CreatedAt).First().ChatId,
                            SenderId = cp.Chat.Messages.OrderByDescending(m => m.CreatedAt).First().SenderId,
                            SenderName = cp.Chat.Messages.OrderByDescending(m => m.CreatedAt).First().Sender.Username,
                            Content = cp.Chat.Messages.OrderByDescending(m => m.CreatedAt).First().Content,
                            AttachmentUrl = cp.Chat.Messages.OrderByDescending(m => m.CreatedAt).First().AttachmentUrl,
                            CreatedAt = cp.Chat.Messages.OrderByDescending(m => m.CreatedAt).First().CreatedAt
                        }
                        : null
                }).ToList();

                return Ok(chatsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chats");
                return StatusCode(500, new { message = "An error occurred while retrieving chats" });
            }
        }

        // POST: api/Chats
        [HttpPost]
        public async Task<IActionResult> CreateChat([FromBody] CreateChatDto createChatDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                _logger.LogInformation("User {UserId} is creating a new chat", currentUserId);

                // Validate participants
                if (createChatDto.ParticipantIds == null || createChatDto.ParticipantIds.Count == 0)
                {
                    return BadRequest(new { message = "At least one participant is required" });
                }

                // Ensure current user is included in participants
                if (!createChatDto.ParticipantIds.Contains(currentUserId))
                {
                    createChatDto.ParticipantIds.Add(currentUserId);
                }

                // For non-group chats between just 2 people, check if chat already exists
                if (!createChatDto.IsGroup && createChatDto.ParticipantIds.Count == 2)
                {
                    var otherUserId = createChatDto.ParticipantIds.First(id => id != currentUserId);

                    // Check for existing direct chat
                    var existingChat = await _context.ChatParticipants
                        .Where(p => p.UserId == currentUserId)
                        .Select(p => p.Chat)
                        .Where(c => !c.IsGroup)
                        .Where(c => c.ChatParticipants.Count == 2)
                        .Where(c => c.ChatParticipants.Any(p => p.UserId == otherUserId))
                        .FirstOrDefaultAsync();

                    if (existingChat != null)
                    {
                        _logger.LogInformation("Found existing chat between users {CurrentUserId} and {OtherUserId}", currentUserId, otherUserId);

                        // Return existing chat information
                        var chatDto = new ChatDto
                        {
                            ChatId = existingChat.ChatId,
                            Name = existingChat.Name,
                            Description = existingChat.Description,
                            IsGroup = existingChat.IsGroup,
                            ProfileUrl = existingChat.ProfileUrl,
                            CreatedAt = existingChat.CreatedAt,
                            UpdatedAt = existingChat.UpdatedAt
                        };

                        return Ok(chatDto);
                    }
                }

                // Create new chat
                var chat = new Chat
                {
                    Name = createChatDto.Name,
                    IsGroup = createChatDto.IsGroup,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Chats.Add(chat);
                await _context.SaveChangesAsync();

                // Add participants
                foreach (var participantId in createChatDto.ParticipantIds)
                {
                    var participant = new ChatParticipant
                    {
                        ChatId = chat.ChatId,
                        UserId = participantId,
                        IsAdmin = participantId == currentUserId, // Current user is admin
                        JoinedAt = DateTime.UtcNow
                    };

                    _context.ChatParticipants.Add(participant);
                }

                await _context.SaveChangesAsync();

                // Create response DTO
                var newChatDto = new ChatDto
                {
                    ChatId = chat.ChatId,
                    Name = chat.Name,
                    Description = chat.Description,
                    IsGroup = chat.IsGroup,
                    ProfileUrl = chat.ProfileUrl,
                    CreatedAt = chat.CreatedAt,
                    UpdatedAt = chat.UpdatedAt,
                    Participants = await _context.ChatParticipants
                        .Where(p => p.ChatId == chat.ChatId)
                        .Select(p => new ChatParticipantDto
                        {
                            UserId = p.UserId,
                            Username = p.User.Username,
                            Email = p.User.Email,
                            ProfileUrl = p.User.ProfileUrl,
                            IsAdmin = p.IsAdmin,
                            JoinedAt = p.JoinedAt
                        })
                        .ToListAsync()
                };

                _logger.LogInformation("Created new chat with ID {ChatId}", chat.ChatId);
                return CreatedAtAction(nameof(GetChat), new { chatId = chat.ChatId }, newChatDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chat");
                return StatusCode(500, new { message = "An error occurred while creating the chat" });
            }
        }

        // GET: api/Chats/5/messages
        [HttpGet("{id}/messages")]
        public async Task<IActionResult> GetChatMessages(int id)
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                _logger.LogInformation("User {UserId} is requesting messages for chat {ChatId}", userId, id);

                // Verify user has access to this chat
                var chatParticipant = await _context.ChatParticipants
                    .FirstOrDefaultAsync(p => p.ChatId == id && p.UserId == userId);

                if (chatParticipant == null)
                {
                    _logger.LogWarning("User {UserId} attempted to access messages for chat {ChatId} but is not a participant", userId, id);
                    return NotFound(new { message = "Chat not found or you don't have access" });
                }

                var messages = await _context.Messages
                    .Where(m => m.ChatId == id)
                    .Include(m => m.Sender)
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new MessageDto
                    {
                        MessageId = m.MessageId,
                        ChatId = m.ChatId,
                        SenderId = m.SenderId,
                        SenderName = m.Sender.Username,
                        Content = m.Content,
                        AttachmentUrl = m.AttachmentUrl,
                        CreatedAt = m.CreatedAt
                    })
                    .ToListAsync();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for chat {ChatId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving messages" });
            }
        }

        // POST: api/Chats/5/messages
        [HttpPost("{id}/messages")]
        public async Task<IActionResult> SendMessage(int id, [FromBody] SendMessageDto messageDto)
        {
            try
            {
                _logger.LogInformation("Received message request: {@MessageDto}", messageDto);
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogInformation("Invalid model state: {Errors}", string.Join(" | ", errors));
                    _logger.LogWarning("Invalid model state: {Errors}", string.Join(" | ", errors));
                    return BadRequest(ModelState);
                }


                int senderId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                _logger.LogInformation("User {UserId} is sending a message to chat {ChatId}", senderId, id);

                // Verify user has access to this chat
                var chatParticipant = await _context.ChatParticipants
                    .FirstOrDefaultAsync(p => p.ChatId == id && p.UserId == senderId);

                if (chatParticipant == null)
                {
                    _logger.LogWarning("User {UserId} attempted to send a message to chat {ChatId} but is not a participant", senderId, id);
                    return NotFound(new { message = "Chat not found or you don't have access" });
                }

                var message = new Message
                {
                    ChatId = id,
                    SenderId = senderId,
                    Content = messageDto.Content,
                    AttachmentUrl = messageDto.AttachmentUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);

                // Update chat's UpdatedAt timestamp
                var chat = await _context.Chats.FindAsync(id);
                if (chat != null)
                {
                    chat.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Get sender information
                var sender = await _context.Users.FindAsync(senderId);

                var responseMessageDto = new MessageDto
                {
                    MessageId = message.MessageId,
                    ChatId = message.ChatId,
                    SenderId = message.SenderId,
                    SenderName = sender.Username,
                    Content = message.Content,
                    AttachmentUrl = message.AttachmentUrl,
                    CreatedAt = message.CreatedAt
                };

                // Notify clients about new message
                await _hubContext.Clients.Group($"chat_{id}").SendAsync("ReceiveMessage", responseMessageDto);

                // Notify all clients about new messages for chat list updates
                await _hubContext.Clients.All.SendAsync("NewMessage", responseMessageDto);

                return CreatedAtAction(nameof(GetChatMessages), new { id = message.ChatId }, responseMessageDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to chat {ChatId}", id);
                return StatusCode(500, new { message = "An error occurred while sending the message" });
            }
        }
    }

  
}