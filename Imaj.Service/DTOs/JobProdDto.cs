using System;

namespace Imaj.Service.DTOs
{
    public class JobProdDto
    {
        public decimal Id { get; set; }
        public decimal ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        
        public decimal CategoryId { get; set; }
        public string? CategoryName { get; set; }

        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal GrossAmount { get; set; } // Ara Tutar
        public decimal NetAmount { get; set; } // Net Tutar (After potential discount)
        // Note: Discount info is not in JobProd directly, unless derived from Gross - Net.
        // Assuming NetAmount is final. 

        public string? Notes { get; set; }
        public bool SelectFlag { get; set; }
    }
}
