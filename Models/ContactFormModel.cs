using System.ComponentModel.DataAnnotations;

namespace Strategy9Website.Models
{
    public class ContactFormModel
    {
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        public string? Property { get; set; }

        public string? Message { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }

    public class ContactFormResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
