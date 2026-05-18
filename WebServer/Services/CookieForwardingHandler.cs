using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Text;

namespace WebServer.Services
{
    /// <summary>
    /// HTTP Message Handler that forwards cookies from the current request
    /// to the ApplicationServer when making outbound HTTP calls.
    /// </summary>
    public class CookieForwardingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CookieForwardingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Copy cookies from current HttpContext to the outbound request
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext != null && httpContext.Request.Cookies.Any())
            {
                // Forward all cookies to the downstream service
                var cookies = httpContext.Request.Cookies;
                var cookieHeader = new StringBuilder();

                foreach (var cookie in cookies)
                {
                    if (cookieHeader.Length > 0)
                        cookieHeader.Append("; ");
                    cookieHeader.Append($"{cookie.Key}={cookie.Value}");
                }

                request.Headers.Add("Cookie", cookieHeader.ToString());
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}