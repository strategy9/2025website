using System.ComponentModel.DataAnnotations;

namespace Strategy9Website.Models
{
    public class NewsletterSignupModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; } = string.Empty;

        public string? IpAddress { get; set; }
        
        public DateTime SignUpDate { get; set; } = DateTime.UtcNow;
    }

    public class NewsletterSignupResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}
