using System.Collections.Generic;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface IProductPageService
    {
        Task<ServiceResult<PagedResultDto<ProductPageListItemDto>>> GetProductsAsync(ProductPageFilterDto filter);
        Task<ServiceResult<ProductPageDetailDto>> GetProductDetailAsync(decimal id);
        Task<ServiceResult<List<ProductPageLanguageDto>>> GetLanguagesAsync();
        Task<ServiceResult<List<ProductPageCategoryOptionDto>>> GetProductCategoryOptionsAsync();
        Task<ServiceResult<List<ProductPageGroupOptionDto>>> GetProductGroupOptionsAsync();
        Task<ServiceResult<PagedResultDto<ProductPageFunctionOptionDto>>> SearchFunctionsAsync(ProductPageFunctionLookupFilterDto filter);
        Task<ServiceResult> CreateProductAsync(ProductPageUpsertDto input);
        Task<ServiceResult> UpdateProductAsync(ProductPageUpsertDto input);
    }
}
