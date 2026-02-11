using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface IProductReportService
    {
        Task<ServiceResult<List<ProductReportRowDto>>> GetDetailedReportAsync(ProductReportFilterDto filter);
    }
}
