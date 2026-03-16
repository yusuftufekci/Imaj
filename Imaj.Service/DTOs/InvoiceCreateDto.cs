using System;
using System.Collections.Generic;

namespace Imaj.Service.DTOs
{
    public class InvoiceCreateDto
    {
        public string? JobCustomerCode { get; set; }
        public string? JobCustomerName { get; set; }
        public string? InvoiceCustomerCode { get; set; }
        public string? InvoiceCustomerName { get; set; }
        public string? Name { get; set; }
        public string? RelatedPerson { get; set; }
        public DateTime IssueDate { get; set; }
        public bool Evaluated { get; set; }
        public string? Notes { get; set; }
        public string? FooterNote { get; set; }
        public List<InvoiceCreateLineDto> Lines { get; set; } = new();
    }

    public class InvoiceCreateLineDto
    {
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public decimal VatRate { get; set; }
    }
}
