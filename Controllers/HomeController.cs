using Microsoft.AspNetCore.Mvc;
using Strategy9Website.Models;
using Strategy9Website.Services;
using Strategy9Website.Data;

namespace Strategy9Website.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEmailIQService _emailIQService;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, IEmailIQService emailIQService, ApplicationDbContext context)
        {
            _logger = logger;
            _emailIQService = emailIQService;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Newsletter([FromBody] NewsletterSignupModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new NewsletterSignupResponse
                {
                    Success = false,
                    Message = "Please fill in all required fields."
                });
            }

            // Get the user's IP address
            model.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _emailIQService.AddSubscriberAsync(model);
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact([FromBody] ContactFormModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ContactFormResponse
                {
                    Success = false,
                    Message = "Please fill in all required fields."
                });
            }

            // Save to database
            var contactRequest = new ContactRequest
            {
                Name = model.Name,
                Email = model.Email,
                Property = model.Property,
                Message = model.Message,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString(),
                SubmittedAt = DateTime.UtcNow
            };

            try
            {
                _context.ContactRequests.Add(contactRequest);
                await _context.SaveChangesAsync();

                // Optional: Still send email via EmailIQ
                var emailResult = await _emailIQService.SendContactEmailAsync(model);

                return Json(new ContactFormResponse
                {
                    Success = true,
                    Message = "Thank you for contacting us! We'll be in touch soon."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving contact request");
                return Json(new ContactFormResponse
                {
                    Success = false,
                    Message = "An error occurred. Please try again."
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSurveyStats(string shortCode)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var url = $"https://default.surveyiq.com/api/services/app/Survey/GetPublicStatistics?shortCode={shortCode}";
                    var response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        return Content(content, "application/json");
                    }

                    return StatusCode((int)response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching survey statistics");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}