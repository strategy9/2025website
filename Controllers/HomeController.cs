using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Strategy9Website.Data;
using Strategy9Website.Models;
using Strategy9Website.Services;
using System.Text.Json;

namespace Strategy9Website.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEmailIQService _emailIQService;
        private readonly ApplicationDbContext _context;
        private readonly JiraService _jiraService;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IEmailIQService emailIQService, ApplicationDbContext context, JiraService jiraService, IConfiguration configuration)
        {
            _logger = logger;
            _emailIQService = emailIQService;
            _context = context;
            _jiraService = jiraService;
            _configuration = configuration;

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

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult TermsOfService()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Support()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Support([FromBody] JiraTicketRequest request)
        {
            try
            {
                // Validate request
                if (string.IsNullOrEmpty(request.Name) ||
                    string.IsNullOrEmpty(request.Email) ||
                    string.IsNullOrEmpty(request.Subject) ||
                    string.IsNullOrEmpty(request.Description))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Please fill in all required fields."
                    });
                }

                // Try to create ticket via Jira API
                var jiraResult = await _jiraService.CreateSupportTicketAsync(request);

                if (jiraResult.Success)
                {
                    // Success - ticket created via API
                    return Json(new
                    {
                        success = true,
                        message = jiraResult.Message,
                        ticketKey = jiraResult.TicketKey,
                        ticketUrl = jiraResult.TicketUrl
                    });
                }
                else
                {
                    // Fallback: If Jira API fails, send email notification and provide portal link
                    _logger.LogWarning("Jira API failed, falling back to email notification");

                    // Send email notification to support team via EmailIQ
                    var emailSuccess = await SendSupportEmailNotification(request);

                    if (emailSuccess)
                    {
                        return Json(new
                        {
                            success = true,
                            message = $"Your support request has been received. Our team will contact you at {request.Email} shortly.",
                            fallback = true
                        });
                    }
                    else
                    {
                        // Last resort - provide portal link
                        var portalUrl = _jiraService.GetPortalUrl();

                        return Json(new
                        {
                            success = false,
                            message = $"We're having trouble processing your request. Please submit your ticket directly at: {portalUrl}",
                            portalUrl = portalUrl
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing support request");

                return Json(new
                {
                    success = false,
                    message = "An error occurred. Please contact us at support@strategy9.com or call 1-855-838-3999"
                });
            }
        }

        private async Task<bool> SendSupportEmailNotification(JiraTicketRequest request)
        {
            try
            {
                // Get EmailIQ configuration
                var emailIqApiKey = _configuration["EmailIQ:ApiKey"];
                var emailIqListId = _configuration["EmailIQ:SupportNotificationListId"]; // New config value

                if (string.IsNullOrEmpty(emailIqApiKey) || string.IsNullOrEmpty(emailIqListId))
                {
                    _logger.LogWarning("EmailIQ not configured for support notifications");
                    return false;
                }

                using var client = new HttpClient();

                var emailContent = new
                {
                    apiKey = emailIqApiKey,
                    listId = emailIqListId,
                    subject = $"[SUPPORT] [{request.Product}] {request.Subject}",
                    fromEmail = "support@strategy9.com",
                    fromName = "Strategy9 Support",
                    htmlBody = BuildSupportEmailHtml(request),
                    textBody = BuildSupportEmailText(request)
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(emailContent),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync("https://www.emailiq.ca/api/v1/send/transactional", content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send support email notification");
                return false;
            }
        }

        private string BuildSupportEmailHtml(JiraTicketRequest request)
        {
            return $@"
        <html>
        <body style='font-family: Arial, sans-serif;'>
            <h2 style='color: #3eb3e6;'>New Support Request</h2>
            <table style='border-collapse: collapse; width: 100%;'>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd; background: #f5f5f5;'><strong>Name:</strong></td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{request.Name}</td>
                </tr>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd; background: #f5f5f5;'><strong>Email:</strong></td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{request.Email}</td>
                </tr>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd; background: #f5f5f5;'><strong>Company:</strong></td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{request.Company}</td>
                </tr>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd; background: #f5f5f5;'><strong>Product:</strong></td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{request.Product}</td>
                </tr>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd; background: #f5f5f5;'><strong>Priority:</strong></td>
                    <td style='padding: 8px; border: 1px solid #ddd;'><strong>{request.Priority}</strong></td>
                </tr>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd; background: #f5f5f5;'><strong>Subject:</strong></td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{request.Subject}</td>
                </tr>
            </table>
            <h3>Description:</h3>
            <div style='padding: 12px; background: #f9f9f9; border-left: 4px solid #3eb3e6;'>
                {request.Description.Replace("\n", "<br/>")}
            </div>
            <p style='margin-top: 20px; color: #666; font-size: 12px;'>
                Submitted via Strategy9.com on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
            </p>
        </body>
        </html>
    ";
        }

        private string BuildSupportEmailText(JiraTicketRequest request)
        {
            return $@"
NEW SUPPORT REQUEST

Name: {request.Name}
Email: {request.Email}
Company: {request.Company}
Product: {request.Product}
Priority: {request.Priority}
Subject: {request.Subject}

Description:
{request.Description}

Submitted via Strategy9.com on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
    ";
        }


        [HttpPost]
        public async Task<IActionResult> GetJiraIds([FromBody] JiraCredentials credentials)
        {
            try
            {
                var auth = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes($"{credentials.Email}:{credentials.ApiToken}")
                );

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
                );

                var baseUrl = $"https://{credentials.Domain}.atlassian.net";

                // Get service desks
                var serviceDesksResponse = await client.GetAsync($"{baseUrl}/rest/servicedeskapi/servicedesk");

                if (!serviceDesksResponse.IsSuccessStatusCode)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Failed to authenticate. Status: {serviceDesksResponse.StatusCode}. Check your email and API token."
                    });
                }

                var serviceDesksJson = await serviceDesksResponse.Content.ReadAsStringAsync();
                var serviceDesks = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(serviceDesksJson);

                var results = new List<object>();

                foreach (var sd in serviceDesks.GetProperty("values").EnumerateArray())
                {
                    var serviceDeskId = sd.GetProperty("id").GetString();

                    // Get request types for this service desk
                    var requestTypesResponse = await client.GetAsync(
                        $"{baseUrl}/rest/servicedeskapi/servicedesk/{serviceDeskId}/requesttype"
                    );

                    if (requestTypesResponse.IsSuccessStatusCode)
                    {
                        var requestTypesJson = await requestTypesResponse.Content.ReadAsStringAsync();
                        var requestTypes = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(requestTypesJson);

                        results.Add(new
                        {
                            serviceDesk = new
                            {
                                id = sd.GetProperty("id").GetString(),
                                projectName = sd.GetProperty("projectName").GetString()
                            },
                            requestTypes = requestTypes.GetProperty("values").EnumerateArray()
                                .Select(rt => new
                                {
                                    id = rt.GetProperty("id").GetString(),
                                    name = rt.GetProperty("name").GetString(),
                                    description = rt.TryGetProperty("description", out var desc) ? desc.GetString() : ""
                                }).ToList()
                        });
                    }
                }

                return Json(new { success = true, results = results, domain = credentials.Domain });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Jira IDs");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        public class JiraCredentials
        {
            public string Email { get; set; } = string.Empty;
            public string ApiToken { get; set; } = string.Empty;
            public string Domain { get; set; } = string.Empty;
        }




    }
}
