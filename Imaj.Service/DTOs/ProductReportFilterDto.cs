namespace Imaj.Service.DTOs
{
    public class ProductReportFilterDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? ProductGroupName { get; set; }
        public string? ProductCode { get; set; }
        public string? CustomerCode { get; set; }
        public decimal LanguageId { get; set; } = 1;
    }
}
