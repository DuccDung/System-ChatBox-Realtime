using ApplicationServer.Dtos.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApplicationServer.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly SocialNetworkContext _context;
        public UsersController(SocialNetworkContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _context.Accounts.FindAsync(id);
                if (user == null) return NotFound("User not found.");

                var response = new userDto
                {
                    AccountId = user.AccountId,
                    AccountName = user.AccountName,
                    Email = user.Email,
                    PhotoPath = user.PhotoPath ?? string.Empty,
                    PhotoBackground = user.PhotoBackground ?? string.Empty,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    Bio = user.Bio
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ===================== SEARCH BY EMAIL (EXACT) =====================
        // GET: /api/users/search?email=minhchung@example.com
        [HttpGet("search")]
        public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest("Email is required.");

                var normalizedEmail = email.Trim().ToLower();

                var user = await _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Email.ToLower() == normalizedEmail);

                if (user == null) return NotFound("User not found.");

                var response = new userDto
                {
                    AccountId = user.AccountId,
                    AccountName = user.AccountName,
                    Email = user.Email,
                    PhotoPath = user.PhotoPath ?? string.Empty
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
