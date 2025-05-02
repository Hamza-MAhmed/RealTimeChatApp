using messaging_app_backend.Models;
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

        public ChatsController(IChatListService chatListService)
        {
            _chatListService = chatListService;
        }

        [HttpGet("contacts")]
        public async Task<IActionResult> GetUserChatList()
        {
            //var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            //if(string.IsNullOrEmpty(userId))
            //    return Unauthorized("User ID claim not found.");

            //if (!int.TryParse(userId, out int userId))
            //    return BadRequest("Invalid user ID format.");

            var chats = await _chatListService.GetUserChatsAsync(userId);
            return Ok(chats);
        }
    }

}
