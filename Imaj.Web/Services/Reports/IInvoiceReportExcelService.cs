using Imaj.Service.DTOs;

namespace Imaj.Web.Services.Reports
{
    public interface IInvoiceReportExcelService
    {
        byte[] BuildDetailedReport(List<InvoiceDetailedReportRowDto> rows);
        byte[] BuildSummaryReport(List<InvoiceSummaryReportRowDto> rows);
    }
}
