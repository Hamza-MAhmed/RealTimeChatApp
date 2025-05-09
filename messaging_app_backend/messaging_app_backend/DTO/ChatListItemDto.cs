namespace messaging_app_backend.DTO
{
    public class ChatListItemDto
    {
        public int ChatId { get; set; }          // userId for 1-on-1, groupId for group
        public string ChatName { get; set; }
        public string ProfileUrl { get; set; }    // profile image or group icon
        public string LastMessage { get; set; }
        public DateTime LastMessageTime { get; set; }
        public bool IsGroup { get; set; }
    }

}
