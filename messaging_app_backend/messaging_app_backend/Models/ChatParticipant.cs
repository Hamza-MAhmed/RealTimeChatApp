namespace messaging_app_backend.Models
{
    public class ChatParticipant
    {
        public int ChatParticipantId { get; set; }
        public int ChatId { get; set; }
        public int UserId { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Chat Chat { get; set; }
        public virtual User User { get; set; }
    }

}
