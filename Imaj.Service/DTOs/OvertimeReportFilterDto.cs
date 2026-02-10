namespace Imaj.Service.DTOs
{
    public class OvertimeReportFilterDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? CustomerCode { get; set; }
        public List<string> EmployeeCodes { get; set; } = new();
        public decimal LanguageId { get; set; } = 1;
    }
}
