using System.Net;
using System.Net.Http.Json;
using WebServer.Dtos;
using WebServer.Interfaces;

namespace WebServer.Services
{
    public class UserService : IUserService
    {
        private readonly HttpClient _http;
        public UserService(HttpClient http)
        {
            _http = http;
        }

        public async Task<UserDto> GetUserByIdAsync(int userId)
        {
            if (userId < 0) throw new ArgumentNullException(nameof(userId));

            var response = await _http.GetAsync($"api/users/{userId}");
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to get user. Status: {response.StatusCode}. Body: {err}");
            }

            var data = await response.Content.ReadFromJsonAsync<UserDto>();
            if (data == null) throw new Exception("Failed to deserialize user data");
            return data;
        }

        public async Task<UserDto> UpdateUserProfileAsync(UserDto profileData)
        {
            if (profileData == null) throw new ArgumentNullException(nameof(profileData));

            var response = await _http.PutAsJsonAsync("api/profile/me", profileData);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to update profile. Status: {response.StatusCode}. Body: {err}");
            }

            var data = await response.Content.ReadFromJsonAsync<UserDto>();
            if (data == null) throw new Exception("Failed to deserialize updated profile");
            return data;
        }

        public async Task<UserDto> UploadAvatarAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("File avatar is empty.");

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Chỉ chấp nhận file ảnh.");

            if (file.Length > 5 * 1024 * 1024)
                throw new Exception("Kích thước ảnh không được vượt quá 5MB.");

            var relativeUrl = await SaveProfileImageAsync(file);

            var response = await _http.PutAsJsonAsync("api/profile/me", new { photoPath = relativeUrl });
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to update avatar. Status: {response.StatusCode}. Body: {err}");
            }

            var data = await response.Content.ReadFromJsonAsync<UserDto>();
            if (data == null) throw new Exception("Failed to deserialize avatar response");
            return data;
        }

        public async Task<UserDto> UploadCoverAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("File cover is empty.");

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Chỉ chấp nhận file ảnh.");

            if (file.Length > 10 * 1024 * 1024)
                throw new Exception("Kích thước ảnh không được vượt quá 10MB.");

            var relativeUrl = await SaveProfileImageAsync(file);

            var response = await _http.PutAsJsonAsync("api/profile/me", new { photoBackground = relativeUrl });
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to update cover. Status: {response.StatusCode}. Body: {err}");
            }

            var data = await response.Content.ReadFromJsonAsync<UserDto>();
            if (data == null) throw new Exception("Failed to deserialize cover response");
            return data;
        }

        public async Task<UserDto> SearchUsersByEmailAsync(string email, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(email))
                return new UserDto();

            var safeEmail = Uri.EscapeDataString(email.Trim());
            limit = Math.Clamp(limit, 1, 50);

            var response = await _http.GetAsync($"api/users/search?email={safeEmail}");

            if (response.StatusCode == HttpStatusCode.NotFound)
                return new UserDto();

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"Search user failed. Status: {response.StatusCode}. Body: {err}");
            }

            var data = await response.Content.ReadFromJsonAsync<UserDto>();
            return data ?? new UserDto();
        }

        private static async Task<string> SaveProfileImageAsync(IFormFile file)
        {
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".gif",
                ".webp"
            };

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext) || !allowedExtensions.Contains(ext))
                throw new Exception("Định dạng ảnh không hợp lệ.");

            var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profile");
            if (!Directory.Exists(uploadsRoot))
                Directory.CreateDirectory(uploadsRoot);

            var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/profile/{fileName}";
        }
    }
}
