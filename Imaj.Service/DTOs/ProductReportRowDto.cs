namespace Imaj.Service.DTOs
{
    public class ProductReportRowDto
    {
        public string ProductGroupName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Reference { get; set; }
        public DateTime JobDate { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
    }
}
