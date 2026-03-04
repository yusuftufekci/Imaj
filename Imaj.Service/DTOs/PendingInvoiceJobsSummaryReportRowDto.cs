namespace Imaj.Service.DTOs
{
    public class PendingInvoiceJobsSummaryReportRowDto
    {
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }
}
