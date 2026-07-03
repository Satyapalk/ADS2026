using ADS2026.Models;
using Microsoft.EntityFrameworkCore;

namespace ADS2026.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<TV> TVs { get; set; }
        public DbSet<User> Users { get; set; }
    }
}