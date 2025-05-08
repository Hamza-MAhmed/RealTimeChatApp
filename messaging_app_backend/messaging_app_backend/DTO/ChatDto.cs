using System.Text.Json.Serialization;
namespace messaging_app_backend.DTO
{
    public class CreateChatDto
    {
        public string Name { get; set; }
        public bool IsGroup { get; set; }
        public List<int> ParticipantIds { get; set; }
    }

    public class ChatDto
    {
        public int ChatId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsGroup { get; set; }
        public string ProfileUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ChatParticipantDto> Participants { get; set; }
        public MessageDto LastMessage { get; set; }
    }

    public class ChatParticipantDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string ProfileUrl { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public class MessageDto
    {
        public int MessageId { get; set; }
        public int ChatId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public string AttachmentUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SendMessageDto
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("attachmentUrl")]
        public string AttachmentUrl { get; set; }
    }
}
