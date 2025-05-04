//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace messaging_app_backend.Models
//{
//    public class ChatMessage
//    {
//        [Key]
//        public Guid Id { get; set; }

//        public int GroupId { get; set; }

//        [Required]
//        public int SenderId { get; set; }

//        [Required]
//        public int ReceiverId { get; set; }

//        [Required]
//        public string Message { get; set; }

//        public bool IsEdited { get; set; } = false;

//        public bool IsSystem { get; set; } = false;

//        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

//        [ForeignKey(nameof(SenderId))]
//        public User Sender { get; set; }

//        [ForeignKey(nameof(ReceiverId))]
//        public User Receiver { get; set; }

//        // Optional: add navigation if you have a Group entity
//        // [ForeignKey(nameof(GroupId))]
//        // public Group Group { get; set; }
//    }
//}


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace messaging_app_backend.Models
{
    public class ChatMessage
    {
        [Key]
        public Guid Id { get; set; }

        public int? GroupId { get; set; } // Nullable for direct messages

        [Required]
        public int SenderId { get; set; }

        public int? ReceiverId { get; set; } // Nullable for group messages

        [Required]
        public string Message { get; set; }

        public bool IsEdited { get; set; } = false;

        public bool IsSystem { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(SenderId))]
        public User Sender { get; set; }

        [ForeignKey(nameof(ReceiverId))]
        public User Receiver { get; set; }

        [ForeignKey(nameof(GroupId))]
        public ChatGroup Group { get; set; }
    }
}
