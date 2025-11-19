using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Strategy9Website.Models
{
    [Table("ContactRequests")]
    public class ContactRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Property { get; set; }

        [MaxLength(2000)]
        public string? Message { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public bool IsProcessed { get; set; } = false;

        public DateTime? ProcessedAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}