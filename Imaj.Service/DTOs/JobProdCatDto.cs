namespace Imaj.Service.DTOs
{
    public class JobProdCatDto
    {
        public decimal CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public decimal GrossAmount { get; set; }
        public byte DiscPercentage { get; set; }
        public decimal DiscAmount { get; set; }
        public decimal NetAmount { get; set; }
    }
}
