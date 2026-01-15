using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebServer.Dtos;
using WebServer.Services;
using WebServer.ViewModels.Auth;

namespace WebServer.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        public IActionResult Login() { return View(); }

        [HttpPost("/auth/login")]
        public async Task<IActionResult> HandleLogin([FromBody] ReqLoginDto req)
        {
            try
            {
                if (req == null) return BadRequest("Invalid login data.");
                var data = await _authService.LoginAsync(new LoginVm { Email = req.Email, Password = req.Password });

                var claims = new List<Claim>
            {
              new Claim(ClaimTypes.NameIdentifier , data.AccountId.ToString()),
              new Claim(ClaimTypes.Name , data.Email),
            };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                if (req.RememberMe)
                {
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                        new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                        });
                }
                else
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                return Ok(new
                {
                    status = "success",
                    accountId = data.AccountId,
                    accountName = data.AccountName,
                    email = data.Email,
                    photoPath = data.PhotoPath
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message });
            }

        }

    }
}
