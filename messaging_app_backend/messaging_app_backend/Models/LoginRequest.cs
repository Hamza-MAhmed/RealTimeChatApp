using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace messaging_app_backend.Models
{
    public class LoginRequest
    {
        // Username is optional since we can login with email
        [JsonProperty("username")]
        public string Username { get; set; }

        // Email is now an option for login
        [JsonProperty("email")]
        public string Email { get; set; }

        // Password is still required
        [Required]
        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
