using ApplicationServer;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.IO;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
Env.Load();

// Configure shared data protection keys for cookie authentication between servers
// Use absolute path to shared folder at repository root
var sharedKeysPath = new DirectoryInfo(@"D:\laptrinhweb\code_outsrc\SystemChatBoxRealtime\SystemChatBoxRealtime\shared-data-protection-keys");
if (!sharedKeysPath.Exists) {
    sharedKeysPath.Create();
}
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(sharedKeysPath)
    .SetApplicationName("SocialNetworkApp");

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<SocialNetworkContext>(options =>
    options.UseSqlServer(
        Environment.GetEnvironmentVariable("DB_Connection")
    )
);

// Configure cookie authentication - must match WebServer configuration
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".AspNetCore.Cookies";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // For API endpoints, return 401 instead of redirect
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    });

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("webServer", policy =>
    {
        policy.WithOrigins(Environment.GetEnvironmentVariable("WebServer_Origin") ?? "")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});
var app = builder.Build();

app.UseCors("webServer");
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
