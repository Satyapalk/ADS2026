using System.ComponentModel.DataAnnotations;

namespace ADS2026.Models
{
    public class TV
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public required string Name { get; set; }

        [MaxLength(100)]
        public required string Room { get; set; }

        [MaxLength(50)]
        public required string Floor { get; set; }

        public bool Enabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}