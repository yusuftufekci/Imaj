using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface IInvoiceService
    {
        Task<ServiceResult<PagedResult<InvoiceDto>>> GetByFilterAsync(InvoiceFilterDto filter);
        Task<ServiceResult<List<InvoiceDetailedReportRowDto>>> GetDetailedInvoiceReportAsync(InvoiceFilterDto filter, CancellationToken cancellationToken = default);
        Task<ServiceResult<List<InvoiceSummaryReportRowDto>>> GetSummaryInvoiceReportAsync(InvoiceFilterDto filter, CancellationToken cancellationToken = default);
        Task<ServiceResult<List<InvoiceDetailDto>>> GetDetailsByReferencesAsync(List<int> references);
        Task<ServiceResult<InvoiceHistoryDto>> GetHistoryByReferenceAsync(int reference);
    }
}
