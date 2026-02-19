using System.Collections.Generic;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface IResoCatService
    {
        Task<ServiceResult<PagedResultDto<ResoCatListItemDto>>> GetResoCatsAsync(ResoCatFilterDto filter);
        Task<ServiceResult<ResoCatDetailDto>> GetResoCatDetailAsync(decimal id);
        Task<ServiceResult<List<ResoCatLanguageDto>>> GetLanguagesAsync();
        Task<ServiceResult> CreateResoCatAsync(ResoCatUpsertDto input);
        Task<ServiceResult> UpdateResoCatAsync(ResoCatUpsertDto input);
    }
}
