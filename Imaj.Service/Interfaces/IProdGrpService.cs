using System.Collections.Generic;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface IProdGrpService
    {
        Task<ServiceResult<PagedResultDto<ProdGrpListItemDto>>> GetProdGrpsAsync(ProdGrpFilterDto filter);
        Task<ServiceResult<ProdGrpDetailDto>> GetProdGrpDetailAsync(decimal id);
        Task<ServiceResult<List<ProdGrpLanguageDto>>> GetLanguagesAsync();
        Task<ServiceResult> CreateProdGrpAsync(ProdGrpUpsertDto input);
        Task<ServiceResult> UpdateProdGrpAsync(ProdGrpUpsertDto input);
    }
}
