using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebServer.Dtos;
using WebServer.Interfaces;

namespace WebServer.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            IUserService userService,
            IAuthService authService,
            ILogger<ProfileController> logger)
        {
            _userService = userService;
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Trang profile của user hiện tại (chỉnh sửa)
        /// </summary>
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        [HttpGet]
        [Route("/Profile/Me")]
        public async Task<IActionResult> Me()
        {
            // Lấy userId từ cookie authentication
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                // Chưa đăng nhập -> redirect về login
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                ViewBag.User = user;
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile for userId: {UserId}", userId);
                TempData["Error"] = "Không thể tải thông tin hồ sơ.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Trang profile công khai của user khác
        /// </summary>
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        [HttpPut]
        [Route("/Profile/Me/Profile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile([FromBody] UserDto request)
        {
            if (request == null)
                return BadRequest(new { message = "Dữ liệu hồ sơ không hợp lệ." });

            var accountName = request.AccountName?.Trim();
            var bio = request.Bio?.Trim();

            if (string.IsNullOrWhiteSpace(accountName))
                return BadRequest(new { message = "Tên hiển thị không được để trống." });

            if (accountName.Length > 80)
                return BadRequest(new { message = "Tên hiển thị không được vượt quá 80 ký tự." });

            if (!string.IsNullOrEmpty(bio) && bio.Length > 280)
                return BadRequest(new { message = "Giới thiệu không được vượt quá 280 ký tự." });

            if (request.DateOfBirth.HasValue && request.DateOfBirth.Value > DateOnly.FromDateTime(DateTime.Today))
                return BadRequest(new { message = "Ngày sinh không được lớn hơn ngày hiện tại." });

            if (request.Gender.HasValue && request.Gender.Value > 2)
                return BadRequest(new { message = "Giới tính không hợp lệ." });

            try
            {
                var updatedUser = await _userService.UpdateUserProfileAsync(new UserDto
                {
                    AccountName = accountName,
                    Bio = bio,
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender
                });

                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                return BadRequest(new { message = "Không thể cập nhật hồ sơ. Vui lòng thử lại." });
            }
        }

        [HttpGet]
        [Route("/Profile/Public/{userId}")]
        public async Task<IActionResult> Public(int userId)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(currentUserIdClaim, out var currentUserId))
                {
                    ViewBag.User = await _userService.GetUserByIdAsync(currentUserId);
                }

                var user = await _userService.GetUserByIdAsync(userId);
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading public profile for userId: {UserId}", userId);
                return RedirectToAction("Index", "Home");
            }
        }

        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        [HttpPost]
        [Route("/Profile/Me/Avatar")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile avatar)
        {
            try
            {
                var user = await _userService.UploadAvatarAsync(avatar);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar");
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        [HttpPost]
        [Route("/Profile/Me/Cover")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadCover([FromForm] IFormFile cover)
        {
            try
            {
                var user = await _userService.UploadCoverAsync(cover);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading cover");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
