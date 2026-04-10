using Imaj.Core.Entities;
using Imaj.Service.DTOs;
using Imaj.Service.Results; // Added this
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Imaj.Service.Interfaces
{
    public interface IProductService
    {
        Task<ServiceResult<PagedResult<ProductDto>>> GetByFilterAsync(ProductFilterDto filter);
        Task<ServiceResult<List<ProductCategoryDto>>> GetCategoriesAsync();
        Task<ServiceResult<List<ProductGroupDto>>> GetProductGroupsAsync(decimal? functionId = null);
        Task<ServiceResult<List<FunctionDto>>> GetFunctionsAsync();
    }
}
