using Microsoft.AspNetCore.Mvc;
using Strategy9Website.Models;
using Strategy9Website.Services;

namespace Strategy9Website.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEmailIQService _emailIQService;

        public HomeController(ILogger<HomeController> logger, IEmailIQService emailIQService)
        {
            _logger = logger;
            _emailIQService = emailIQService;
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

            var result = await _emailIQService.SendContactEmailAsync(model);
            return Json(result);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
