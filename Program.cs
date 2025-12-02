using Strategy9Website.Services;
using Microsoft.EntityFrameworkCore;
using Strategy9Website.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Register JiraService with HttpClient
builder.Services.AddHttpClient<Strategy9Website.Services.JiraService>();

builder.Services.AddHttpClient();

builder.Services.AddRazorPages();

// Register EmailIQ Service
builder.Services.AddScoped<IEmailIQService, EmailIQService>();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

// 👇 Add this custom route BEFORE the default route
app.MapControllerRoute(
    name: "playeriqapp",
    pattern: "PlayerIQApp",
    defaults: new { controller = "Home", action = "PlayerIQApp" }
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();