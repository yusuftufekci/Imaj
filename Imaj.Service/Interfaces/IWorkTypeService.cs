using System.Collections.Generic;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface IWorkTypeService
    {
        Task<ServiceResult<PagedResultDto<WorkTypeListItemDto>>> GetWorkTypesAsync(WorkTypeFilterDto filter);
        Task<ServiceResult<WorkTypeDetailDto>> GetWorkTypeDetailAsync(decimal id);
        Task<ServiceResult<List<WorkTypeLanguageDto>>> GetLanguagesAsync();
        Task<ServiceResult> CreateWorkTypeAsync(WorkTypeUpsertDto input);
        Task<ServiceResult> UpdateWorkTypeAsync(WorkTypeUpsertDto input);
    }
}
