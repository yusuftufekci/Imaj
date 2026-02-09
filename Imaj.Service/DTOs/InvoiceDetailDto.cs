using System;
using System.Collections.Generic;

namespace Imaj.Service.DTOs
{
    public class InvoiceDetailDto
    {
        public int Reference { get; set; }
        public string? JobCustomerName { get; set; }
        public string? InvoiceCustomerName { get; set; }
        public string? Name { get; set; }
        public string? RelatedPerson { get; set; }
        public DateTime? IssueDate { get; set; }
        public string? StateName { get; set; }
        public bool Evaluated { get; set; }
        public string? Notes { get; set; }
        public string? FooterNote { get; set; }

        public List<InvoiceLineDto> Lines { get; set; } = new List<InvoiceLineDto>();
        public List<InvoiceJobDto> Jobs { get; set; } = new List<InvoiceJobDto>();
        public List<InvoiceProdCatSummaryDto> ProductCategories { get; set; } = new List<InvoiceProdCatSummaryDto>();
        public List<InvoiceTaxSummaryDto> Taxes { get; set; } = new List<InvoiceTaxSummaryDto>();
    }

    public class InvoiceLineDto
    {
        public bool Selected { get; set; }
        public short Sequence { get; set; }
        public string? Notes { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public string? TaxType { get; set; }
        public decimal? TaxTypeId { get; set; }
    }

    public class InvoiceJobDto
    {
        public bool Selected { get; set; }
        public int Reference { get; set; }
        public string? Name { get; set; }
        public decimal Amount { get; set; }
    }

    public class InvoiceProdCatSummaryDto
    {
        public decimal ProdCatId { get; set; }
        public string? Name { get; set; }
        public decimal SubTotal { get; set; }
        public decimal NetTotal { get; set; }
    }

    public class InvoiceTaxSummaryDto
    {
        public decimal TaxTypeId { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Rate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetTotal { get; set; }
    }
}
