using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Strategy9Website.Services
{
    public class JiraConfiguration
    {
        public string AtlassianDomain { get; set; } = string.Empty;
        public string ServiceDeskId { get; set; } = string.Empty;
        public string RequestTypeId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ApiToken { get; set; } = string.Empty;
    }

    public class JiraTicketRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class JiraTicketResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? TicketKey { get; set; }
        public string? TicketUrl { get; set; }
    }

    public class JiraService
    {
        private readonly HttpClient _httpClient;
        private readonly JiraConfiguration _config;
        private readonly ILogger<JiraService> _logger;

        public JiraService(HttpClient httpClient, IConfiguration configuration, ILogger<JiraService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _config = configuration.GetSection("Jira").Get<JiraConfiguration>() ?? new JiraConfiguration();

            // Set up authentication
            if (!string.IsNullOrEmpty(_config.Email) && !string.IsNullOrEmpty(_config.ApiToken))
            {
                var authToken = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{_config.Email}:{_config.ApiToken}")
                );
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", authToken);
            }

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );
        }

        public bool IsConfigured()
        {
            return !string.IsNullOrEmpty(_config.AtlassianDomain) &&
                   !string.IsNullOrEmpty(_config.ServiceDeskId) &&
                   !string.IsNullOrEmpty(_config.RequestTypeId) &&
                   !string.IsNullOrEmpty(_config.Email) &&
                   !string.IsNullOrEmpty(_config.ApiToken);
        }

        public async Task<JiraTicketResponse> CreateSupportTicketAsync(JiraTicketRequest request)
        {
            try
            {
                if (!IsConfigured())
                {
                    _logger.LogWarning("Jira is not configured. Tickets cannot be created via API.");
                    return new JiraTicketResponse
                    {
                        Success = false,
                        Message = "Jira API is not configured. Please contact support at support@strategy9.com"
                    };
                }

                // Build description with all details
                var fullDescription = BuildTicketDescription(request);

                // Create Jira Service Desk request
                var jiraRequest = new
                {
                    serviceDeskId = _config.ServiceDeskId,
                    requestTypeId = _config.RequestTypeId,
                    requestFieldValues = new
                    {
                        summary = $"[{request.Product}] {request.Subject}",
                        description = fullDescription
                    },
                    raiseOnBehalfOf = request.Email
                };

                var jsonContent = JsonSerializer.Serialize(jiraRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var url = $"https://{_config.AtlassianDomain}/rest/servicedeskapi/request";

                _logger.LogInformation("Creating Jira ticket via API: {Url}", url);

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var jiraResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var ticketKey = jiraResponse.GetProperty("issueKey").GetString();
                    var ticketUrl = $"https://{_config.AtlassianDomain}/servicedesk/customer/portal/1/{ticketKey}";

                    _logger.LogInformation("Successfully created Jira ticket: {TicketKey}", ticketKey);

                    return new JiraTicketResponse
                    {
                        Success = true,
                        Message = "Your support ticket has been created successfully. Our team will respond shortly.",
                        TicketKey = ticketKey,
                        TicketUrl = ticketUrl
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create Jira ticket. Status: {StatusCode}, Error: {Error}",
                        response.StatusCode, errorContent);

                    return new JiraTicketResponse
                    {
                        Success = false,
                        Message = $"Failed to create ticket via API (Status: {response.StatusCode}). Please try again or contact support@strategy9.com"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception creating Jira ticket");
                return new JiraTicketResponse
                {
                    Success = false,
                    Message = "An error occurred while creating your ticket. Please contact support@strategy9.com"
                };
            }
        }

        private string BuildTicketDescription(JiraTicketRequest request)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"*Reporter:* {request.Name}");
            sb.AppendLine($"*Email:* {request.Email}");

            if (!string.IsNullOrEmpty(request.Company))
            {
                sb.AppendLine($"*Company/Property:* {request.Company}");
            }

            sb.AppendLine($"*Product:* {request.Product}");
            sb.AppendLine($"*Priority:* {request.Priority}");
            sb.AppendLine();
            sb.AppendLine("*Description:*");
            sb.AppendLine(request.Description);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine($"_Submitted via Strategy9.com on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC_");

            return sb.ToString();
        }

        public string GetPortalUrl()
        {
            return $"https://{_config.AtlassianDomain}/servicedesk/customer/portal/1/group/1/create/1";
        }
    }
}
