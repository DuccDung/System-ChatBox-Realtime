using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using WebServer.Infrastructure.HttpClients.Options;
using WebServer.Interfaces;
using WebServer.Services;
Env.Load();
var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<ApiClientOptions>(
    builder.Configuration.GetSection("ApiClients:Auth")
);
builder.Services.AddHttpClient<IAuthService, AuthService>(
    (sp, client) =>
    {
        var opt = sp.GetRequiredService<IOptions<ApiClientOptions>>().Value;

        client.BaseAddress = new Uri(opt.BaseUrl);
        //client.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds);
    });
builder.Services.AddHttpClient<IUserService, UserService>(
    (sp, client) =>
    {
        var opt = sp.GetRequiredService<IOptions<ApiClientOptions>>().Value;
        client.BaseAddress = new Uri(opt.BaseUrl);
        //client.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds);
    });
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt => { opt.LoginPath = "/Auth/Login"; opt.LogoutPath = "/Auth/Logout"; opt.ExpireTimeSpan = TimeSpan.FromHours(8); opt.SlidingExpiration = true; });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}"
);

app.Run();
