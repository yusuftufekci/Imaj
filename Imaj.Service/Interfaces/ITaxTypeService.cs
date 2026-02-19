using System.Collections.Generic;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface ITaxTypeService
    {
        Task<ServiceResult<PagedResultDto<TaxTypeListItemDto>>> GetTaxTypesAsync(TaxTypeFilterDto filter);
        Task<ServiceResult<TaxTypeDetailDto>> GetTaxTypeDetailAsync(decimal id);
        Task<ServiceResult<List<TaxTypeLanguageDto>>> GetLanguagesAsync();
        Task<ServiceResult> CreateTaxTypeAsync(TaxTypeUpsertDto input);
        Task<ServiceResult> UpdateTaxTypeAsync(TaxTypeUpsertDto input);
    }
}
