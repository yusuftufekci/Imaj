using System.Collections.Generic;

namespace Imaj.Service.DTOs
{
    public class InvoiceUpdateDto
    {
        public int Reference { get; set; }
        public string? InvoiceCustomerCode { get; set; }
        public string? Name { get; set; }
        public string? RelatedPerson { get; set; }
        public string? Notes { get; set; }
        public string? FooterNote { get; set; }
        public List<InvoiceUpdateLineDto> Lines { get; set; } = new();
        public List<InvoiceUpdateFreeLineDto> NewFreeLines { get; set; } = new();
        public List<InvoiceUpdateProductCategoryDto> ProductCategories { get; set; } = new();
        public List<InvoiceUpdateTaxDto> Taxes { get; set; } = new();
    }

    public class InvoiceUpdateLineDto
    {
        public decimal Id { get; set; }
        public string? Notes { get; set; }
        public decimal Amount { get; set; }
        public decimal VatRate { get; set; }
    }

    public class InvoiceUpdateFreeLineDto
    {
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public decimal VatRate { get; set; }
    }

    public class InvoiceUpdateProductCategoryDto
    {
        public decimal ProdCatId { get; set; }
        public decimal NetTotal { get; set; }
    }

    public class InvoiceUpdateTaxDto
    {
        public decimal TaxTypeId { get; set; }
        public decimal Rate { get; set; }
    }
}
