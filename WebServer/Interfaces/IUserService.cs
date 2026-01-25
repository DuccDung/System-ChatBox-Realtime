using WebServer.Dtos;

namespace WebServer.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> GetUserByIdAsync(int userId); 
    }
}
