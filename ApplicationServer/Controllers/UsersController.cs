using ApplicationServer.Dtos.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
