using System.Collections.Generic;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface ITimeTypeService
    {
        Task<ServiceResult<PagedResultDto<TimeTypeListItemDto>>> GetTimeTypesAsync(TimeTypeFilterDto filter);
        Task<ServiceResult<TimeTypeDetailDto>> GetTimeTypeDetailAsync(decimal id);
        Task<ServiceResult<List<TimeTypeLanguageDto>>> GetLanguagesAsync();
        Task<ServiceResult> CreateTimeTypeAsync(TimeTypeUpsertDto input);
        Task<ServiceResult> UpdateTimeTypeAsync(TimeTypeUpsertDto input);
    }
}
