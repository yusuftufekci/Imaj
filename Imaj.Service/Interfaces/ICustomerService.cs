using System.Collections.Generic;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface ICustomerService
    {
        Task<ServiceResult<List<CustomerDto>>> GetAllAsync();
        Task<ServiceResult<PagedResult<CustomerDto>>> GetByFilterAsync(CustomerFilterDto filter);
        Task<ServiceResult<CustomerDto>> GetByIdAsync(decimal id);
        Task<ServiceResult<CustomerDto>> GetByCodeAsync(string code);
        Task<ServiceResult> AddAsync(CustomerDto customerDto);
        Task<ServiceResult> UpdateAsync(CustomerDto customerDto);
        
        // State (Durum) listesini veritabanından getir
        Task<ServiceResult<List<StateDto>>> GetStatesAsync();

        // Ürün Kategorilerini getir (TaxTypeID=6 ve XProdCat/TaxType ilişkili)
        Task<ServiceResult<List<ProductCategoryDto>>> GetProductCategoriesAsync();
    }
}
