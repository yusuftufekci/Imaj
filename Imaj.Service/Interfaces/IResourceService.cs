using System.Collections.Generic;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface IResourceService
    {
        Task<ServiceResult<PagedResultDto<ResourceListItemDto>>> GetResourcesAsync(ResourceFilterDto filter);
        Task<ServiceResult<ResourceDetailDto>> GetResourceDetailAsync(decimal id);
        Task<ServiceResult<List<ResourceLanguageDto>>> GetLanguagesAsync();
        Task<ServiceResult<List<ResourceFunctionOptionDto>>> GetFunctionOptionsAsync();
        Task<ServiceResult<List<ResourceResoCatOptionDto>>> GetResoCatOptionsAsync();
        Task<ServiceResult> CreateResourceAsync(ResourceUpsertDto input);
        Task<ServiceResult> UpdateResourceAsync(ResourceUpsertDto input);
    }
}
