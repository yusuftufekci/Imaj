namespace Imaj.Service.DTOs
{
    public class InvoiceSummaryReportRowDto
    {
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal SubTotal { get; set; }
        public decimal NetTotal { get; set; }
    }
}
