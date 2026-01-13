using ApplicationServer.Dtos.Auth;
using ApplicationServer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApplicationServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly SocialNetworkContext _context;
        public AuthController(SocialNetworkContext context)
        {
            _context = context;
        }

        [HttpPost("auth/login")]
        public async Task<IActionResult> login([FromBody] ReqLogin req)
        {
            try
            {
                var user = await _context.Accounts.FirstOrDefaultAsync(u => u.Email == req.email && u.Password == req.password);
                if (user == null) return BadRequest("Invalid email or password");
                return Ok(new ResLogin
                {
                    AccountId = user.AccountId,
                    AccountName = user.AccountName,
                    Password = user.Password,
                    Email = user.Email,
                    PhotoPath = user.PhotoPath
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("auth/register")]
        public async Task<IActionResult> register([FromBody] ReqRegister req)
        {
            try
            {
                var existingUser = await _context.Accounts.FirstOrDefaultAsync(u => u.Email == req.email);
                if (existingUser == null) return BadRequest("acc early exits");
                var newUser = new Account
                {
                    Email = req.email!,
                    Password = req.password!,
                    AccountName = "New User"
                };
                await _context.Accounts.AddAsync(newUser);
                await _context.SaveChangesAsync();
                return Ok("Init success new user.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
