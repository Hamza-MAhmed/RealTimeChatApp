namespace messaging_app_backend.Models
{
    public class Message
    {
        public int MessageId { get; set; }
        public int ChatId { get; set; }
        public int SenderId { get; set; }
        public string Content { get; set; }
        public string AttachmentUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Chat Chat { get; set; }
        public virtual User Sender { get; set; }
        public virtual ICollection<MessageReadStatus> ReadStatus { get; set; } = new List<MessageReadStatus>();
    }
}
