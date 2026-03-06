using System.Collections.Generic;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface IAbsenceService
    {
        Task<ServiceResult<PagedResultDto<AbsenceListItemDto>>> GetAbsencesAsync(AbsenceFilterDto filter);
        Task<ServiceResult<AbsenceDetailDto>> GetAbsenceDetailAsync(decimal id);
        Task<ServiceResult<List<AbsenceLogDto>>> GetAbsenceHistoryAsync(decimal id);
        Task<ServiceResult<List<AbsenceFunctionOptionDto>>> GetFunctionOptionsAsync();
        Task<ServiceResult<List<AbsenceReasonOptionDto>>> GetReasonOptionsAsync();
        Task<ServiceResult<List<AbsenceStateOptionDto>>> GetStateOptionsAsync();
        Task<ServiceResult<PagedResultDto<AbsenceResourceItemDto>>> SearchResourcesAsync(AbsenceResourceLookupFilterDto filter);
        Task<ServiceResult> CreateAbsenceAsync(AbsenceCreateDto input);
        Task<ServiceResult> UpdateAbsenceScheduleAsync(decimal id, DateTime newDate, AbsenceScheduleAction action);
        Task<ServiceResult> ExecuteWorkflowActionAsync(decimal id, AbsenceWorkflowAction action);
    }
}
