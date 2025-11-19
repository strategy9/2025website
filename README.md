# Strategy9 Website

Enterprise casino marketing technology company website built with ASP.NET Core 8 MVC.

## Features

- **Modern, Mobile-First Design** - Built with Tailwind CSS and Alpine.js
- **EmailIQ Integration** - Newsletter signup with GreenArrow API
- **SurveyIQ Embed** - Live survey demo on homepage
- **Contact Form** - Lead capture with email notifications
- **Product Showcase** - All IQ products highlighted
- **SOC2 Compliance** - Security and trust messaging
- **Testimonials** - Real customer feedback

## Tech Stack

- **.NET 8** - Latest LTS framework
- **ASP.NET Core MVC** - Server-side rendering
- **Tailwind CSS** - Utility-first CSS via CDN
- **Alpine.js** - Lightweight JavaScript framework
- **Newtonsoft.Json** - JSON serialization
- **HttpClient** - API integration

## Prerequisites

- .NET 8 SDK or later
- Visual Studio 2022 or VS Code
- IIS or Azure App Service for hosting

## Getting Started

### 1. Clone/Open the Project

```bash
cd Strategy9Website
```

### 2. Configure Settings

Update `appsettings.json` with your configuration:

```json
{
  "EmailIQ": {
    "ApiUrl": "https://mail2.emailiq.ca/ga/api/v2",
    "ApiKey": "YOUR_API_KEY_HERE",
    "MailingListId": 31,
    "CustomFields": {
      "FirstName": 120,
      "LastName": 121,
      "IpAddress": 122,
      "SignUpDate": 123
    }
  },
  "SurveyIQ": {
    "SurveyEmbedUrl": "https://default.surveyiq.com/s/YOUR_SURVEY_ID"
  }
}
```

### 3. Restore Packages

```bash
dotnet restore
```

### 4. Run the Application

```bash
dotnet run
```

The site will be available at `https://localhost:5001`

## Project Structure

```
Strategy9Website/
├── Controllers/
│   └── HomeController.cs          # Main controller with Newsletter and Contact actions
├── Models/
│   ├── NewsletterSignupModel.cs   # Newsletter form model
│   └── ContactFormModel.cs        # Contact form model
├── Services/
│   └── EmailIQService.cs          # EmailIQ API integration
├── Views/
│   ├── Home/
│   │   └── Index.cshtml           # Homepage
│   └── Shared/
│       └── _Layout.cshtml         # Master layout
├── wwwroot/
│   ├── css/                       # Custom styles (if needed)
│   ├── js/                        # Custom JavaScript (if needed)
│   └── images/                    # Logo and images
├── appsettings.json               # Configuration
├── Program.cs                     # Application entry point
└── Strategy9Website.csproj        # Project file
```

## Deployment

### IIS Deployment

1. Build the project:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. Copy contents of `./publish` to your IIS site directory

3. Configure IIS:
   - Create/update application pool (.NET CLR Version: No Managed Code)
   - Set application pool identity with appropriate permissions
   - Ensure ASP.NET Core Hosting Bundle is installed

### Azure App Service Deployment

1. Create an Azure App Service (Windows, .NET 8)

2. Deploy using Visual Studio:
   - Right-click project → Publish
   - Select Azure App Service
   - Follow wizard

3. Or use Azure CLI:
   ```bash
   az webapp up --name strategy9-website --resource-group YourResourceGroup
   ```

4. Configure Application Settings in Azure Portal:
   - Add EmailIQ settings
   - Add SurveyIQ settings
   - Configure connection strings if needed

## Configuration

### EmailIQ Integration

The EmailIQ service (`Services/EmailIQService.cs`) handles newsletter signups by:

1. Collecting user data (email, first name, last name, IP address)
2. Mapping to GreenArrow custom fields
3. Posting to EmailIQ API with proper authentication
4. Handling duplicate subscriber scenarios
5. Logging all operations for debugging

### SurveyIQ Integration

The survey is embedded via iframe. To update:

1. Create/update survey in SurveyIQ
2. Get the embed URL from SurveyIQ
3. Update `appsettings.json` → `SurveyIQ:SurveyEmbedUrl`

### Contact Form

Currently logs submissions. To enable email sending:

1. Implement `SendContactEmailAsync` in `EmailIQService.cs`
2. Use EmailIQ injection API or configure SMTP
3. Update email templates as needed

## Customization

### Adding Images/Logo

1. Add images to `wwwroot/images/`
2. Update references in `_Layout.cshtml` and `Index.cshtml`
3. Replace placeholders:
   - Logo in navigation
   - Hero section image
   - SOC2 badge

### Updating Content

- **Homepage**: Edit `Views/Home/Index.cshtml`
- **Layout/Navigation**: Edit `Views/Shared/_Layout.cshtml`
- **Styles**: Add custom CSS to `wwwroot/css/site.css` or update Tailwind classes

### Adding Pages

1. Add action to `HomeController.cs`
2. Create corresponding view in `Views/Home/`
3. Update navigation in `_Layout.cshtml`

## API Endpoints

### POST /Home/Newsletter
Newsletter signup endpoint

**Request:**
```json
{
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Thank you for subscribing!",
  "data": {}
}
```

### POST /Home/Contact
Contact form submission endpoint

**Request:**
```json
{
  "name": "John Doe",
  "email": "user@example.com",
  "property": "Example Casino",
  "message": "I'd like to schedule a demo"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Thank you for your message!"
}
```

## Troubleshooting

### Newsletter signup fails

1. Check EmailIQ API credentials in `appsettings.json`
2. Verify mailing list ID is correct
3. Check custom field IDs match your EmailIQ configuration
4. Review application logs for detailed error messages

### Survey doesn't load

1. Verify SurveyIQ embed URL is correct
2. Check iframe permissions (may need to update Content-Security-Policy)
3. Ensure survey is published and publicly accessible

### Build errors

1. Ensure .NET 8 SDK is installed: `dotnet --version`
2. Clean and rebuild: `dotnet clean && dotnet build`
3. Check NuGet package versions are compatible

## Support

For questions or issues:
- Email: sales@strategy9.com
- Phone: 1-855-838-3999

## License

Copyright © 2025 Strategy9. All rights reserved.
