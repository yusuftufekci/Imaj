namespace Imaj.Service.DTOs
{
    public class OvertimeSummaryReportRowDto
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string TimeTypeName { get; set; } = string.Empty;
        public string WorkTypeName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
    }
}
