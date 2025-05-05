using System.ComponentModel.DataAnnotations;

namespace messaging_app_backend.DTO
{
    public class CreateGroupChatDto
    {
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Name { get; set; }

        public string Description { get; set; }

        public string ProfileUrl { get; set; }

        [Required]
        public List<int> ParticipantIds { get; set; }
    }
}
