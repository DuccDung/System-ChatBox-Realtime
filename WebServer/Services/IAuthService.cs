using WebServer.Dtos;
using WebServer.ViewModels.Auth;

namespace WebServer.Services
{
    public interface IAuthService
    {
        Task<ResLoginDto> LoginAsync(LoginVm vm, CancellationToken ct = default);
        Task RegisterAsync(RegisterVm vm, CancellationToken ct = default);
    }
}
