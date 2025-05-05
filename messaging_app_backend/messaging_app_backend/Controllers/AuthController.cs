using messaging_app_backend.Data;
using messaging_app_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace messaging_app_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ChatAppDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ChatAppDbContext context, ILogger<AuthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            _logger.LogInformation("Received test request!");
            return Ok(new { message = "API is working" });
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupRequest signupRequest)
        {
            try
            {
                // Log complete request details for debugging
                _logger.LogInformation("Received signup request with data: Username={Username}, Email={Email}, PhoneNo={PhoneNo}",
                    signupRequest.Username, signupRequest.Email, signupRequest.PhoneNo);

                // Manual validation in case model binding doesn't trigger validation
                if (string.IsNullOrEmpty(signupRequest.Username) ||
                    string.IsNullOrEmpty(signupRequest.Email) ||
                    string.IsNullOrEmpty(signupRequest.Password) ||
                    string.IsNullOrEmpty(signupRequest.PhoneNo))
                {
                    return BadRequest("All required fields must be provided");
                }

                // Check for existing user
                if (await _context.Users.AnyAsync(u => u.Username == signupRequest.Username))
                {
                    return BadRequest("Username already exists");
                }

                if (await _context.Users.AnyAsync(u => u.Email == signupRequest.Email))
                {
                    return BadRequest("Email already in use");
                }

                // Create new user
                var user = new User
                {
                    Username = signupRequest.Username,
                    Email = signupRequest.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(signupRequest.Password),
                    PhoneNo = signupRequest.PhoneNo,
                    ProfileUrl = signupRequest.ProfileUrl ?? "", // Handle possible null
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User registration successful: {Username}", signupRequest.Username);

                // Return token to allow immediate login
                var token = GenerateJwtToken(user);
                return Ok(new { token = token, message = "User registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for {Username}", signupRequest?.Username ?? "unknown");
                return StatusCode(500, "An error occurred during registration");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            try
            {
                // Manual validation to ensure either username or email is provided
                if (string.IsNullOrEmpty(loginRequest.Password))
                {
                    return BadRequest(new { message = "Password is required" });
                }

                if (string.IsNullOrEmpty(loginRequest.Username) && string.IsNullOrEmpty(loginRequest.Email))
                {
                    return BadRequest(new { message = "Either username or email is required" });
                }

                // Initialize user as null
                User user = null;

                // Check if we have a username or email
                if (!string.IsNullOrEmpty(loginRequest.Username))
                {
                    _logger.LogInformation("Login attempt for username: {Username}", loginRequest.Username);
                    user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == loginRequest.Username.ToLower());
                }
                else if (!string.IsNullOrEmpty(loginRequest.Email))
                {
                    _logger.LogInformation("Login attempt for email: {Email}", loginRequest.Email);
                    user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == loginRequest.Email.ToLower());
                }

                // Validate user exists and password is correct
                if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.Password))
                {
                    _logger.LogWarning("Login failed: Invalid credentials");
                    return Unauthorized(new { message = "Invalid username/email or password" });
                }

                // Generate the token
                var token = GenerateJwtToken(user);
                _logger.LogInformation("Login successful for user ID: {UserId}", user.UserId);

                // Return token with correct capitalization to match frontend expectation
                return Ok(new { token = token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        //[HttpPost("login")]
        //public IActionResult Login([FromBody] User user)
        //{
        //    var existingUser = _context.Users.FirstOrDefault(u => u.Username == user.Username && u.Password == user.Password);

        //    if (existingUser == null)
        //    {
        //        return Unauthorized("Invalid username or password");
        //    }

        //    return Ok("Login successful");
        //}

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("IAmTheSecretKeyWithExactly32CharsOK!"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                //new Claim("UserId", user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };
            foreach (var claim in claims)
            {
                Console.WriteLine($"{claim.Type}: {claim.Value}");
            }

            var token = new JwtSecurityToken(
                issuer: "yourapp.com",
                audience: "yourapp.com",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}



