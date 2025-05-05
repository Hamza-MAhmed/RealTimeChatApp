using messaging_app_backend.Data;
using messaging_app_backend.DTO;
using Microsoft.EntityFrameworkCore; // Add this namespace for ToListAsync
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace messaging_app_backend.Services
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllUsersAsync();
        Task<UserDto> GetUserByIdAsync(int userId);
        Task<UserDto> UpdateUserProfileAsync(int userId, UpdateUserProfileDto profileDto);
        Task<string> UploadProfilePictureAsync(int userId, IFormFile file);
    }

    public class UserService : IUserService
    {
        private readonly ChatAppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UserService> _logger;

        public UserService(
            ChatAppDbContext context,
            IWebHostEnvironment environment,
            ILogger<UserService> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Get all users from the database
        /// </summary>
        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            _logger.LogInformation("Getting all users");

            var users = await _context.Users
                .Select(u => new UserDto
                {
                    Id = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    ProfileUrl = u.ProfileUrl,
                    PhoneNo = u.PhoneNo
                })
                .ToListAsync();

            return users;
        }

        /// <summary>
        /// Get a specific user by their ID
        /// </summary>
        public async Task<UserDto> GetUserByIdAsync(int userId)
        {
            _logger.LogInformation("Getting user with ID: {UserId}", userId);

            var user = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new UserDto
                {
                    Id = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    ProfileUrl = u.ProfileUrl,
                    PhoneNo = u.PhoneNo
                })
                .FirstOrDefaultAsync();

            return user;
        }

        /// <summary>
        /// Update a user's profile information
        /// </summary>
        public async Task<UserDto> UpdateUserProfileAsync(int userId, UpdateUserProfileDto profileDto)
        {
            _logger.LogInformation("Updating profile for user: {UserId}", userId);

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                throw new KeyNotFoundException($"User with ID {userId} not found");
            }

            // Update user properties
            if (!string.IsNullOrEmpty(profileDto.Name))
            {
                user.Username = profileDto.Name;
            }

            if (!string.IsNullOrEmpty(profileDto.PhoneNumber))
            {
                user.PhoneNo = profileDto.PhoneNumber;
            }

            await _context.SaveChangesAsync();

            // Return updated user
            return new UserDto
            {
                Id = user.UserId,
                Username = user.Username,
                Email = user.Email,
                ProfileUrl = user.ProfileUrl,
                PhoneNo = user.PhoneNo,
            };
        }

        /// <summary>
        /// Upload a profile picture for a user
        /// </summary>
        public async Task<string> UploadProfilePictureAsync(int userId, IFormFile file)
        {
            _logger.LogInformation("Uploading profile picture for user: {UserId}", userId);

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                throw new KeyNotFoundException($"User with ID {userId} not found");
            }

            // Check if file is an image
            if (!file.ContentType.StartsWith("image/"))
            {
                _logger.LogWarning("Invalid file type: {ContentType}", file.ContentType);
                throw new ArgumentException("Only image files are allowed");
            }

            // Create upload directory if it doesn't exist
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Delete old profile picture if exists
            if (!string.IsNullOrEmpty(user.ProfileUrl))
            {
                var oldFilePath = Path.Combine(_environment.WebRootPath, user.ProfileUrl.TrimStart('/'));
                if (File.Exists(oldFilePath))
                {
                    File.Delete(oldFilePath);
                }
            }

            // Save new file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Update profile URL in database
            var profileUrl = $"/uploads/profiles/{fileName}";
            user.ProfileUrl = profileUrl;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Profile picture updated successfully for user: {UserId}", userId);

            return profileUrl;
        }
    }
}