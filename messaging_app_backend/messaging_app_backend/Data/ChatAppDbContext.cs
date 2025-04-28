using messaging_app_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace messaging_app_backend.Data
{
    public class ChatAppDbContext : DbContext
    {
        public ChatAppDbContext(DbContextOptions<ChatAppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
    }
}
