using System.ComponentModel.DataAnnotations;

namespace ADS2026.Models
{
    public class MediaFile
    {
        public int Id { get; set; }

        public required string FileName { get; set; }

        [MaxLength(200)]
        public string? Title { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public required string FileUrl { get; set; }

        public required string FileType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}