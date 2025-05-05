using messaging_app_backend.Models;
using messaging_app_backend.DTO;
using messaging_app_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace messaging_app_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatsController : ControllerBase
    {
        private readonly IChatListService _chatListService;
        private readonly ILogger<ChatsController> _logger;

        public ChatsController(IChatListService chatListService, ILogger<ChatsController> logger)
        {
            _chatListService = chatListService;
            _logger = logger;
        }

        /// <summary>
        /// Get all chats for the current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUserChats()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                _logger.LogInformation("Getting chats for user {UserId}", userId);

                var chats = await _chatListService.GetUserChatsAsync(userId);
                return Ok(chats);
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
    }
}