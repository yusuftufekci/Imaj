using Imaj.Service.DTOs;

namespace Imaj.Web.Services.Reports
{
    public interface IJobReportExcelService
    {
        byte[] BuildDetailedReport(List<JobDetailedReportRowDto> rows);
        byte[] BuildSummaryReport(List<JobSummaryReportRowDto> rows);
    }
}
