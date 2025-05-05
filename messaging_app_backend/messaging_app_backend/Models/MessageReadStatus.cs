using System.ComponentModel.DataAnnotations.Schema;

namespace messaging_app_backend.Models
{
    public class MessageReadStatus
    {
        public int MessageReadStatusId { get; set; }
        public int MessageId { get; set; }
        public int UserId { get; set; }
        public DateTime ReadAt { get; set; } = DateTime.UtcNow;

        // Navigation properties with explicit foreign key annotations
        [ForeignKey("MessageId")]
        public virtual Message Message { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
