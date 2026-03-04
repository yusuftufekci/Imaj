namespace Imaj.Service.DTOs
{
    public class JobSummaryReportRowDto
    {
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal WorkAmount { get; set; }
        public decimal ProductAmount { get; set; }
    }
}
