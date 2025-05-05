namespace messaging_app_backend.DTO
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PhoneNo { get; set; }
        public string ProfileUrl { get; set; }
    }

    public class UpdateUserProfileDto
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
    }
}
