using System.Collections.Generic;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface IUserService
    {
        Task<ServiceResult<PagedResultDto<UserListItemDto>>> GetUsersAsync(UserFilterDto filter);
        Task<ServiceResult<UserDetailDto>> GetUserDetailAsync(string userCodeOrId);
        Task<ServiceResult<List<UserLanguageDto>>> GetLanguagesAsync();
        Task<ServiceResult<UserCompanyContextDto>> GetCompanyContextAsync();
        Task<ServiceResult<PagedResultDto<RoleLookupItemDto>>> SearchRolesAsync(RoleLookupFilterDto filter);
        Task<ServiceResult<PagedResultDto<FunctionLookupItemDto>>> SearchFunctionsAsync(FunctionLookupFilterDto filter);
        Task<ServiceResult> CreateUserAsync(UserUpsertDto input);
        Task<ServiceResult> UpdateUserAsync(UserUpsertDto input);
        Task<ServiceResult> ChangeCurrentUserPasswordAsync(ChangeCurrentUserPasswordDto input);
    }
}
