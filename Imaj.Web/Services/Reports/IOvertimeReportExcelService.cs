using Imaj.Service.DTOs;

namespace Imaj.Web.Services.Reports
{
    public interface IOvertimeReportExcelService
    {
        byte[] BuildDetailedReport(List<OvertimeReportRowDto> rows, OvertimeReportExcelContext context);
        byte[] BuildSummaryReport(List<OvertimeSummaryReportRowDto> rows, OvertimeReportExcelContext context);
        byte[] BuildAdministrativeSummaryReport(List<OvertimeAdministrativeSummaryReportRowDto> rows, OvertimeReportExcelContext context);
    }

    public class OvertimeReportExcelContext
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string EmployeeDisplay { get; set; } = string.Empty;
        public string CustomerDisplay { get; set; } = string.Empty;
    }
}
