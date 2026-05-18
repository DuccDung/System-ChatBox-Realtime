using WebServer.Dtos;

namespace WebServer.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> GetUserByIdAsync(int userId);
        Task<UserDto> SearchUsersByEmailAsync(string email, int limit = 20);
        Task<UserDto> UpdateUserProfileAsync(UserDto profileData);
        Task<UserDto> UploadAvatarAsync(IFormFile file);
        Task<UserDto> UploadCoverAsync(IFormFile file);
    }
}
