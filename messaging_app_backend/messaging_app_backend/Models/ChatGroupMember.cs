using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace messaging_app_backend.Models
{
    public class ChatGroupMember
    {
        public enum GroupRole
        {
            Member,
            Admin,
            Owner
        }

        [Key]
        public int Id { get; set; }

        [Required]
        public int GroupId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("GroupId")]
        public ChatGroup Group { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        [Required]
        [Column(TypeName = "varchar(20)")]
        public GroupRole Role { get; set; } = GroupRole.Member;
    }

}
