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
            if(userId < 0) throw new ArgumentNullException(nameof(userId));
            var response = await _http.GetAsync($"api/users/{userId}");
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to get user with status code: {response.Content.ReadAsStringAsync()}");
            var data = await response.Content.ReadFromJsonAsync<UserDto>();
            if (data == null) throw new Exception("Failed to deserialize user data");
            return data;
        }
    }
}
