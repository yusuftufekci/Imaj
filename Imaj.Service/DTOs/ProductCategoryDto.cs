namespace Imaj.Service.DTOs
{
    public class ProductCategoryDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string TaxName { get; set; } = string.Empty;
        public decimal TaxTypeId { get; set; }
        public short Sequence { get; set; }
        public bool IsSelected { get; set; }
        public decimal Discount { get; set; } // For UI binding on selected items
    }
}
