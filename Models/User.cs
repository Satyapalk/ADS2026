using System.ComponentModel.DataAnnotations;

namespace ADS2026.Models
{
    public class User
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public required string Username { get; set; }

        public required string PasswordHash { get; set; }

        [MaxLength(50)]
        public string Role { get; set; } = "user";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
