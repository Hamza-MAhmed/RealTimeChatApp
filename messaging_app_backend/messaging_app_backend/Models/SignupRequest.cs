namespace messaging_app_backend.Models
{
    public class SignupRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNo { get; set; }
        public string ProfileUrl { get; set; }
    }
}
