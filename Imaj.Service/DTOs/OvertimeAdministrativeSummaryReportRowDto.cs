namespace Imaj.Service.DTOs
{
    public class OvertimeAdministrativeSummaryReportRowDto
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
    }
}
