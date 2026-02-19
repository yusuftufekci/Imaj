using System.Collections.Generic;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface IReasonService
    {
        Task<ServiceResult<PagedResultDto<ReasonListItemDto>>> GetReasonsAsync(ReasonFilterDto filter);
        Task<ServiceResult<ReasonDetailDto>> GetReasonDetailAsync(decimal id);
        Task<ServiceResult<List<ReasonLanguageDto>>> GetLanguagesAsync();
        Task<ServiceResult<List<ReasonCatOptionDto>>> GetReasonCatOptionsAsync();
        Task<ServiceResult> CreateReasonAsync(ReasonUpsertDto input);
        Task<ServiceResult> UpdateReasonAsync(ReasonUpsertDto input);
    }
}
