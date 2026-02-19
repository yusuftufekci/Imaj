using System.Collections.Generic;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface IFunctionService
    {
        Task<ServiceResult<PagedResultDto<FunctionListItemDto>>> GetFunctionsAsync(FunctionFilterDto filter);
        Task<ServiceResult<FunctionDetailDto>> GetFunctionDetailAsync(decimal id);
        Task<ServiceResult<List<FunctionLanguageDto>>> GetLanguagesAsync();
        Task<ServiceResult<List<FunctionIntervalOptionDto>>> GetIntervalsAsync();
        Task<ServiceResult<PagedResultDto<FunctionProductLookupItemDto>>> SearchProductsAsync(FunctionProductLookupFilterDto filter);
        Task<ServiceResult<PagedResultDto<FunctionResoCatLookupItemDto>>> SearchResoCategoriesAsync(FunctionResoCatLookupFilterDto filter);
        Task<ServiceResult> CreateFunctionAsync(FunctionUpsertDto input);
        Task<ServiceResult> UpdateFunctionAsync(FunctionUpsertDto input);
    }
}
