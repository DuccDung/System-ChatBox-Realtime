using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using System.IO;
using System.Net.Http.Headers;
using WebServer.Infrastructure.HttpClients.Options;
using WebServer.Interfaces;
using WebServer.Services;
Env.Load();
var builder = WebApplication.CreateBuilder(args);

// Configure shared data protection keys for cookie authentication between servers
// Use absolute path to shared folder at repository root
var sharedKeysPath = new DirectoryInfo(@"D:\laptrinhweb\code_outsrc\SystemChatBoxRealtime\SystemChatBoxRealtime\shared-data-protection-keys");
if (!sharedKeysPath.Exists) {
    sharedKeysPath.Create();
}
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(sharedKeysPath)
    .SetApplicationName("SocialNetworkApp");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.Configure<ApiClientOptions>(
    builder.Configuration.GetSection("ApiClients:Auth")
);

// Get API base URL from config
var apiBaseUrl = builder.Configuration.GetSection("ApiClients:Auth:BaseUrl").Value ?? "http://localhost:5001";

// Custom handler to forward cookies to ApplicationServer
builder.Services.AddHttpClient<IAuthService, AuthService>(
    (sp, client) =>
    {
        var opt = sp.GetRequiredService<IOptions<ApiClientOptions>>().Value;
        client.BaseAddress = new Uri(opt.BaseUrl);
    }).AddHttpMessageHandler(sp => new CookieForwardingHandler(sp.GetRequiredService<IHttpContextAccessor>()));

builder.Services.AddHttpClient<IConversationService, ConversationService>(
    (sp, client) =>
    {
        var opt = sp.GetRequiredService<IOptions<ApiClientOptions>>().Value;
        client.BaseAddress = new Uri(opt.BaseUrl);
    }).AddHttpMessageHandler(sp => new CookieForwardingHandler(sp.GetRequiredService<IHttpContextAccessor>()));

builder.Services.AddHttpClient<IUserService, UserService>(
    (sp, client) =>
    {
        var opt = sp.GetRequiredService<IOptions<ApiClientOptions>>().Value;
        client.BaseAddress = new Uri(opt.BaseUrl);
    }).AddHttpMessageHandler(sp => new CookieForwardingHandler(sp.GetRequiredService<IHttpContextAccessor>()));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.Cookie.Name = ".AspNetCore.Cookies";
        opt.LoginPath = "/Auth/Login";
        opt.LogoutPath = "/Auth/Logout";
        opt.ExpireTimeSpan = TimeSpan.FromHours(8);
        opt.SlidingExpiration = true;
    });
builder.Services.AddSingleton<WebServer.Services.RealtimeHub>();
builder.Services.AddSingleton<WebServer.Services.WebSocketHandler>();
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
app.UseWebSockets();
app.UseStaticFiles();

app.Map("/ws", async httpContext =>
{
    if (!httpContext.WebSockets.IsWebSocketRequest)
    {
        httpContext.Response.StatusCode = 400;
        await httpContext.Response.WriteAsync("WebSocket request required");
        return;
    }

    // lấy userId từ claims (cookie auth)
    string? userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    // fallback: query userId (nếu bạn vẫn muốn)
    if (string.IsNullOrWhiteSpace(userId))
        userId = httpContext.Request.Query["userId"].ToString();

    if (string.IsNullOrWhiteSpace(userId))
    {
        httpContext.Response.StatusCode = 401;
        await httpContext.Response.WriteAsync("Missing userId");
        return;
    }

    var ws = await httpContext.WebSockets.AcceptWebSocketAsync();
    var handler = httpContext.RequestServices.GetRequiredService<WebServer.Services.WebSocketHandler>();

    await handler.HandleAsync(ws, userId, httpContext.RequestAborted);
});

// API Proxy middleware - forward /api/* to ApplicationServer with cookie forwarding
// MUST be before MapStaticAssets and MapControllerRoute
app.Map("/api/{**slug}", async (HttpContext httpContext) =>
{
    var slug = httpContext.Request.Path.Value.Substring("/api/".Length);
    var query = httpContext.Request.QueryString.ToString();
    var targetUrl = $"{apiBaseUrl}/api/{slug}{query}";

    var clientFactory = httpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
    var client = clientFactory.CreateClient();

    // Forward cookies from current request to ApplicationServer
    if (httpContext.Request.Cookies.Any())
    {
        var cookieHeader = string.Join("; ", httpContext.Request.Cookies.Select(c => $"{c.Key}={c.Value}"));
        client.DefaultRequestHeaders.Add("Cookie", cookieHeader);
    }

    // Read request body once (for POST/PUT/PATCH)
    byte[]? bodyBytes = null;
    if (httpContext.Request.Method != "GET" && httpContext.Request.Method != "DELETE")
    {
        using var ms = new MemoryStream();
        await httpContext.Request.Body.CopyToAsync(ms);
        bodyBytes = ms.ToArray();
    }

    // Forward the request based on method
    HttpResponseMessage response;
    switch (httpContext.Request.Method.ToUpper())
    {
        case "GET":
            response = await client.GetAsync(targetUrl);
            break;
        case "POST":
            {
                using var postContent = new ByteArrayContent(bodyBytes ?? Array.Empty<byte>());
                postContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(httpContext.Request.ContentType ?? "application/json");
                response = await client.PostAsync(targetUrl, postContent);
            }
            break;
        case "PUT":
            {
                using var putContent = new ByteArrayContent(bodyBytes ?? Array.Empty<byte>());
                putContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(httpContext.Request.ContentType ?? "application/json");
                response = await client.PutAsync(targetUrl, putContent);
            }
            break;
        case "DELETE":
            response = await client.DeleteAsync(targetUrl);
            break;
        case "PATCH":
            {
                using var patchContent = new ByteArrayContent(bodyBytes ?? Array.Empty<byte>());
                patchContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(httpContext.Request.ContentType ?? "application/json");
                response = await client.PatchAsync(targetUrl, patchContent);
            }
            break;
        default:
            httpContext.Response.StatusCode = 405;
            await httpContext.Response.WriteAsync("Method not allowed");
            return;
    }

    // Copy response status code
    httpContext.Response.StatusCode = (int)response.StatusCode;

    // Copy response headers
    foreach (var header in response.Headers)
    {
        httpContext.Response.Headers.Append(header.Key, header.Value.ToArray());
    }

    // Copy content type if available
    var contentType = response.Content.Headers.ContentType?.ToString();
    if (contentType != null)
    {
        httpContext.Response.ContentType = contentType;
    }

    // Read and write response content
    var contentBytes = await response.Content.ReadAsByteArrayAsync();
    await httpContext.Response.Body.WriteAsync(contentBytes);
});

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}"
);

app.Run();
