using Newtonsoft.Json;
using Strategy9Website.Models;
using System.Net.Http.Headers;
using System.Text;

namespace Strategy9Website.Services
{
    public interface IEmailIQService
    {
        Task<NewsletterSignupResponse> AddSubscriberAsync(NewsletterSignupModel model);
        Task<ContactFormResponse> SendContactEmailAsync(ContactFormModel model);
    }

    public class EmailIQService : IEmailIQService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailIQService> _logger;
        private readonly string _apiKey;
        private readonly string _apiUrl;
        private readonly int _mailingListId;

        public EmailIQService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<EmailIQService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            _apiKey = configuration["EmailIQ:ApiKey"] ?? throw new InvalidOperationException("EmailIQ ApiKey not configured");
            _apiUrl = configuration["EmailIQ:ApiUrl"] ?? throw new InvalidOperationException("EmailIQ ApiUrl not configured");
            _mailingListId = int.Parse(configuration["EmailIQ:MailingListId"] ?? throw new InvalidOperationException("EmailIQ MailingListId not configured"));
        }

        public async Task<NewsletterSignupResponse> AddSubscriberAsync(NewsletterSignupModel model)
        {
            try
            {
                _logger.LogInformation("=== AddSubscriberAsync START ===");
                _logger.LogInformation("Email: {Email}, FirstName: {FirstName}, LastName: {LastName}",
                    model.Email, model.FirstName, model.LastName);

                // Get custom field IDs from configuration
                var firstNameFieldId = _configuration["EmailIQ:CustomFields:FirstName"];
                var lastNameFieldId = _configuration["EmailIQ:CustomFields:LastName"];
                var ipAddressFieldId = _configuration["EmailIQ:CustomFields:IpAddress"];
                var signUpDateFieldId = _configuration["EmailIQ:CustomFields:SignUpDate"];

                // Build custom fields dictionary
                var customFields = new Dictionary<string, object>();
                
                if (!string.IsNullOrEmpty(firstNameFieldId))
                    customFields[firstNameFieldId] = model.FirstName;
                
                if (!string.IsNullOrEmpty(lastNameFieldId))
                    customFields[lastNameFieldId] = model.LastName;
                
                if (!string.IsNullOrEmpty(ipAddressFieldId) && !string.IsNullOrEmpty(model.IpAddress))
                    customFields[ipAddressFieldId] = model.IpAddress;
                
                if (!string.IsNullOrEmpty(signUpDateFieldId))
                    customFields[signUpDateFieldId] = model.SignUpDate.ToString("yyyy-MM-dd HH:mm:ss");

                // Following the pattern from your EmailIQAppService.cs
                var subscriberData = new
                {
                    subscriber = new
                    {
                        email = model.Email,
                        status = "active",
                        custom_fields = customFields,
                        first_name = model.FirstName,
                        last_name = model.LastName
                    }
                };

                var jsonContent = JsonConvert.SerializeObject(subscriberData);
                _logger.LogInformation("Request JSON: {Json}", jsonContent);

                var requestContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var requestUrl = $"{_apiUrl}/mailing_lists/{_mailingListId}/subscribers";
                
                _logger.LogInformation("Request URL: {Url}", requestUrl);

                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                {
                    Content = requestContent
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _apiKey);

                _logger.LogInformation("Sending HTTP request...");
                var response = await _httpClient.SendAsync(request);

                _logger.LogInformation("Response Status: {StatusCode} {ReasonPhrase}",
                    (int)response.StatusCode, response.ReasonPhrase);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response Content: {Content}", responseContent);

                dynamic? jsonResponse = null;
                try
                {
                    jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    _logger.LogInformation("Response parsed successfully");
                }
                catch (Exception parseEx)
                {
                    _logger.LogError(parseEx, "Failed to parse response JSON: {Content}", responseContent);
                }

                bool isSuccess = response.IsSuccessStatusCode && (jsonResponse?.success == true);

                _logger.LogInformation("Success Evaluation: HttpSuccess={HttpSuccess}, Final={Final}",
                    response.IsSuccessStatusCode, isSuccess);

                if (isSuccess)
                {
                    _logger.LogInformation("✓ Successfully added subscriber {Email} to mailing list {ListId}",
                        model.Email, _mailingListId);

                    return new NewsletterSignupResponse
                    {
                        Success = true,
                        Message = "Thank you for subscribing! Check your inbox for confirmation.",
                        Data = jsonResponse?.data
                    };
                }
                else
                {
                    string errorMessage = jsonResponse?.error_message?.ToString() ?? response.ReasonPhrase;
                    string errorCode = jsonResponse?.error_code?.ToString() ?? "UNKNOWN";

                    _logger.LogError("✗ Failed to add subscriber {Email}: ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}, HttpStatus={Status}",
                        model.Email, errorCode, errorMessage, (int)response.StatusCode);

                    // Check if it's a duplicate subscriber error
                    if (errorMessage.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                        errorMessage.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                    {
                        return new NewsletterSignupResponse
                        {
                            Success = true,
                            Message = "You're already subscribed to our newsletter!",
                            Data = null
                        };
                    }

                    return new NewsletterSignupResponse
                    {
                        Success = false,
                        Message = $"Failed to subscribe: {errorMessage}",
                        Data = null
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error adding subscriber {Email}", model.Email);
                return new NewsletterSignupResponse
                {
                    Success = false,
                    Message = "An error occurred while processing your subscription. Please try again later.",
                    Data = null
                };
            }
            finally
            {
                _logger.LogInformation("=== AddSubscriberAsync END ===");
            }
        }

        public async Task<ContactFormResponse> SendContactEmailAsync(ContactFormModel model)
        {
            try
            {
                _logger.LogInformation("=== SendContactEmailAsync START ===");
                _logger.LogInformation("Name: {Name}, Email: {Email}, Property: {Property}",
                    model.Name, model.Email, model.Property ?? "N/A");

                // For now, we'll just log the contact form submission
                // In production, you would integrate with EmailIQ's injection API or send via SMTP
                _logger.LogInformation("Contact form submitted successfully");

                // TODO: Implement actual email sending via EmailIQ injection API
                // Following the SendEmailViaInjectionAsync pattern from your code

                return new ContactFormResponse
                {
                    Success = true,
                    Message = "Thank you for your message! We'll be in touch soon."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing contact form from {Email}", model.Email);
                return new ContactFormResponse
                {
                    Success = false,
                    Message = "An error occurred while sending your message. Please try emailing us directly at sales@strategy9.com"
                };
            }
            finally
            {
                _logger.LogInformation("=== SendContactEmailAsync END ===");
            }
        }
    }
}
