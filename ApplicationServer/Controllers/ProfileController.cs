using ApplicationServer.Dtos.Profile;
using ApplicationServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ApplicationServer.Controllers
{
    [Route("api/profile")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly SocialNetworkContext _context;
        private readonly ILogger<ProfileController> _logger;
        private readonly IWebHostEnvironment _env;

        public ProfileController(
            SocialNetworkContext context,
            ILogger<ProfileController> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// Lấy thông tin profile của user hiện tại (cần đăng nhập)
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new { message = "Không xác thực được người dùng" });

                var user = await _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.AccountId == userId);

                if (user == null)
                    return NotFound(new { message = "User not found" });

                var response = new ProfileResponseDto
                {
                    AccountId = user.AccountId,
                    AccountName = user.AccountName ?? "Người dùng",
                    Email = user.Email,
                    PhotoPath = user.PhotoPath ?? string.Empty,
                    PhotoBackground = user.PhotoBackground ?? string.Empty,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    Bio = user.Bio ?? "Chưa có giới thiệu"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my profile");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin profile của user hiện tại
        /// </summary>
        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new { message = "Không xác thực được người dùng" });

                var user = await _context.Accounts.FindAsync(userId);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                // Cập nhật các field
                if (request.AccountName != null)
                    user.AccountName = request.AccountName;
                if (request.Bio != null)
                    user.Bio = request.Bio;
                if (request.DateOfBirth.HasValue)
                    user.DateOfBirth = request.DateOfBirth;
                if (request.Gender.HasValue)
                    user.Gender = request.Gender;
                if (request.PhotoPath != null)
                    user.PhotoPath = request.PhotoPath;
                if (request.PhotoBackground != null)
                    user.PhotoBackground = request.PhotoBackground;

                await _context.SaveChangesAsync();

                var response = new ProfileResponseDto
                {
                    AccountId = user.AccountId,
                    AccountName = user.AccountName ?? "Người dùng",
                    Email = user.Email,
                    PhotoPath = user.PhotoPath ?? string.Empty,
                    PhotoBackground = user.PhotoBackground ?? string.Empty,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    Bio = user.Bio ?? "Chưa có giới thiệu"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating my profile");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Upload ảnh đại diện
        /// </summary>
        [Authorize]
        [HttpPost("me/avatar")]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile avatar)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new { message = "Không xác thực được người dùng" });

                if (avatar == null || avatar.Length == 0)
                    return BadRequest(new { message = "File ảnh là bắt buộc" });

                // Validate file type
                if (!avatar.ContentType.StartsWith("image/"))
                    return BadRequest(new { message = "Chỉ chấp nhận file ảnh" });

                // Validate file size (max 5MB)
                if (avatar.Length > 5 * 1024 * 1024)
                    return BadRequest(new { message = "Kích thước ảnh không được vượt quá 5MB" });

                var user = await _context.Accounts.FindAsync(userId);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                // Tạo thư mục upload
                var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "profile");
                if (!Directory.Exists(uploadsRoot))
                    Directory.CreateDirectory(uploadsRoot);

                // Tạo tên file unique
                var ext = Path.GetExtension(avatar.FileName);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(uploadsRoot, fileName);

                // Lưu file
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }

                // Cập nhật đường dẫn vào DB
                var relativePath = $"/uploads/profile/{fileName}";
                user.PhotoPath = relativePath;
                await _context.SaveChangesAsync();

                return Ok(new ProfileResponseDto
                {
                    AccountId = user.AccountId,
                    AccountName = user.AccountName ?? "Người dùng",
                    Email = user.Email,
                    PhotoPath = user.PhotoPath ?? string.Empty,
                    PhotoBackground = user.PhotoBackground ?? string.Empty,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    Bio = user.Bio ?? "Chưa có giới thiệu"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Upload ảnh bìa
        /// </summary>
        [Authorize]
        [HttpPost("me/cover")]
        public async Task<IActionResult> UploadCover([FromForm] IFormFile cover)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new { message = "Không xác thực được người dùng" });

                if (cover == null || cover.Length == 0)
                    return BadRequest(new { message = "File ảnh là bắt buộc" });

                // Validate file type
                if (!cover.ContentType.StartsWith("image/"))
                    return BadRequest(new { message = "Chỉ chấp nhận file ảnh" });

                // Validate file size (max 10MB)
                if (cover.Length > 10 * 1024 * 1024)
                    return BadRequest(new { message = "Kích thước ảnh không được vượt quá 10MB" });

                var user = await _context.Accounts.FindAsync(userId);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                // Tạo thư mục upload
                var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "profile");
                if (!Directory.Exists(uploadsRoot))
                    Directory.CreateDirectory(uploadsRoot);

                // Tạo tên file unique
                var ext = Path.GetExtension(cover.FileName);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(uploadsRoot, fileName);

                // Lưu file
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await cover.CopyToAsync(stream);
                }

                // Cập nhật đường dẫn vào DB
                var relativePath = $"/uploads/profile/{fileName}";
                user.PhotoBackground = relativePath;
                await _context.SaveChangesAsync();

                return Ok(new ProfileResponseDto
                {
                    AccountId = user.AccountId,
                    AccountName = user.AccountName ?? "Người dùng",
                    Email = user.Email,
                    PhotoPath = user.PhotoPath ?? string.Empty,
                    PhotoBackground = user.PhotoBackground ?? string.Empty,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    Bio = user.Bio ?? "Chưa có giới thiệu"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading cover");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Tìm kiếm user theo email (cho tìm bạn bè)
        /// </summary>
        [AllowAnonymous]
        [HttpGet("by-email")]
        public async Task<IActionResult> GetProfileByEmail([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest(new { message = "Email is required" });

                var normalizedEmail = email.Trim().ToLower();

                var user = await _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Email.ToLower() == normalizedEmail);

                if (user == null)
                    return NotFound(new { message = "User not found" });

                var response = new ProfileResponseDto
                {
                    AccountId = user.AccountId,
                    AccountName = user.AccountName ?? "Người dùng",
                    Email = user.Email,
                    PhotoPath = user.PhotoPath ?? string.Empty,
                    PhotoBackground = user.PhotoBackground ?? string.Empty,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    Bio = user.Bio ?? "Chưa có giới thiệu"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching profile by email: {Email}", email);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thông tin profile công khai theo userId (PHẢI ĐẶT SAU 'me' VÀ 'by-email')
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{userId:int}")]
        public async Task<IActionResult> GetPublicProfile(int userId)
        {
            try
            {
                var user = await _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.AccountId == userId);

                if (user == null)
                    return NotFound(new { message = "User not found" });

                var response = new ProfileResponseDto
                {
                    AccountId = user.AccountId,
                    AccountName = user.AccountName ?? "Người dùng",
                    PhotoPath = user.PhotoPath ?? string.Empty,
                    PhotoBackground = user.PhotoBackground ?? string.Empty,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    Bio = user.Bio ?? "Chưa có giới thiệu"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public profile for userId: {UserId}", userId);
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
