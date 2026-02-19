using System.Collections.Generic;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface IProdCatService
    {
        Task<ServiceResult<PagedResultDto<ProdCatListItemDto>>> GetProdCatsAsync(ProdCatFilterDto filter);
        Task<ServiceResult<ProdCatDetailDto>> GetProdCatDetailAsync(decimal id);
        Task<ServiceResult<List<ProdCatLanguageDto>>> GetLanguagesAsync();
        Task<ServiceResult<List<ProdCatTaxTypeOptionDto>>> GetTaxTypeOptionsAsync();
        Task<ServiceResult> CreateProdCatAsync(ProdCatUpsertDto input);
        Task<ServiceResult> UpdateProdCatAsync(ProdCatUpsertDto input);
    }
}
