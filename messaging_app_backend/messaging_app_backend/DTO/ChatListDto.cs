namespace messaging_app_backend.DTO
{
    public class ChatListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsGroup { get; set; }
        public string ProfileUrl { get; set; }
        public int MemberCount { get; set; }
        public int UnreadCount { get; set; }
        public MessageDto LastMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
