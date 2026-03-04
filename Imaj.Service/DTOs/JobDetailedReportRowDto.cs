namespace Imaj.Service.DTOs
{
    public class JobDetailedReportRowDto
    {
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
        public int Reference { get; set; }
        public string JobName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public bool IsEvaluated { get; set; }
        public decimal WorkAmount { get; set; }
        public decimal ProductAmount { get; set; }
    }
}
