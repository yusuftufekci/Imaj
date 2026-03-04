using Imaj.Service.DTOs;

namespace Imaj.Web.Services.Reports
{
    public interface IPendingInvoiceJobsReportExcelService
    {
        byte[] BuildDetailedReport(List<PendingInvoiceJobsDetailedReportRowDto> rows, PendingInvoiceJobsReportExcelContext context);
        byte[] BuildSummaryReport(List<PendingInvoiceJobsSummaryReportRowDto> rows, PendingInvoiceJobsReportExcelContext context);
    }

    public class PendingInvoiceJobsReportExcelContext
    {
        public string CustomerDisplay { get; set; } = string.Empty;
    }
}
