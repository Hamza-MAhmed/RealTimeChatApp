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

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupRequest signupRequest)
        {
            _logger.LogInformation("Received signup request with data: {@SignupRequest}", signupRequest);

            if (await _context.Users.AnyAsync(u => u.Username == signupRequest.Username))
            {
                return BadRequest("Username already exists");
            }

            var user = new User
            {
                Username = signupRequest.Username,
                Email = signupRequest.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(signupRequest.Password),  // 🔥 Hash password here
                PhoneNo = signupRequest.PhoneNo,
                ProfileUrl = signupRequest.ProfileUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Signup attempt for: {signupRequest.Username}"); // Simple logging


            return Ok("User registered successfully");
        }



        //[HttpPost("signup")]
        //public IActionResult Signup([FromBody] User user)
        //{
        //    if (_context.Users.Any(u => u.Username == user.Username))
        //    {
        //        return BadRequest("Username already exists");
        //    }

        //    _context.Users.Add(user);
        //    _context.SaveChanges();
        //    return Ok("Signup successful");
        //}

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == loginRequest.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.Password)) //  Verify hash
            {
                return Unauthorized("Invalid username or password");
            }

            var token = GenerateJwtToken(user);

            return Ok(new { Token = token });
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
            var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("YourSuperSecretKeyHere123!"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, user.Username),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

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



