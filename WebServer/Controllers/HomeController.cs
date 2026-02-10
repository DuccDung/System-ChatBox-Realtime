using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using WebServer.Interfaces;
using System.Security.Claims;
using WebServer.Interfaces;
namespace WebServer.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IUserService _userService;
        public HomeController(IUserService userService)
        {
            _userService = userService;
        }

        public IActionResult Index() => View();

        public async Task<IActionResult> Main()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Auth");

            var user = await _userService.GetUserByIdAsync(int.Parse(userId));
            ViewBag.User = user;
            return View();
        }

        // Render cái form modal (HTML)
        [HttpGet("/chat/search_view")]
        public IActionResult SearchView()
        {
            return PartialView("Partials/_FormFriends");
        }

        [HttpGet("/chat/personal")]
        public async Task<IActionResult> PersonalView(int userId)
        {
            var friend = await _userService.GetUserByIdAsync(userId);
            return PartialView("Partials/_FormPersonal", friend);
        }

        [HttpGet("/chat/search_user")]
        public async Task<IActionResult> SearchUser([FromQuery] string email, [FromQuery] int limit = 20)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var user = await _userService.SearchUsersByEmailAsync(email, limit);
                if (user.AccountId == int.Parse(userId))
                {
                    return Content("<div class='form_friends__empty'>Đây là tài khoản của bạn!</div>", "text/html");
                }
                if (user.AccountId == 0)
                {
                    return Content("<div class='form_friends__empty'>Không tìm thấy người dùng nào.</div>", "text/html");
                }
                return PartialView("Partials/_FriendSearchResults", user);
            }
            catch
            {
                Response.StatusCode = 500;
                return Content("<div class='form_friends__empty'>Có lỗi xảy ra khi tìm kiếm.</div>", "text/html");
            }
        }
        [HttpGet("/chat/threads")]
        public async Task<IActionResult> ThreadsView([FromServices] IConversationService conversationService)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr))
                return Content("<div class='threads-empty'>Bạn chưa đăng nhập.</div>", "text/html");

            var accountId = int.Parse(userIdStr);

            try
            {
                var threads = await conversationService.GetThreadsAsync(accountId);
                return PartialView("Partials/_ChatThreads", threads);
            }
            catch
            {
                Response.StatusCode = 500;
                return Content("<div class='threads-empty'>Không tải được danh sách cuộc trò chuyện.</div>", "text/html");
            }
        }
    }
}
