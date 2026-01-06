using System.Security.Claims;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResult<UserDto>> LoginAsync(LoginDto loginDto);
        ClaimsPrincipal CreatePrincipal(UserDto user);
        string HashPassword(string password);
    }
}
