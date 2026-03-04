namespace Imaj.Service.DTOs
{
    public class PendingInvoiceJobsDetailedReportRowDto
    {
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int Reference { get; set; }
        public string JobName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal Amount { get; set; }
    }
}
