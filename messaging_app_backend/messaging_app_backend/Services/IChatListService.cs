using messaging_app_backend.DTO;

namespace messaging_app_backend.Services
{
    public interface IChatListService
    {
        Task<List<ChatListItemDto>> GetUserChatsAsync(int userId);
    }
}