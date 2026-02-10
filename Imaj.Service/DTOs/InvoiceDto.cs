using System;

namespace Imaj.Service.DTOs
{
    public class InvoiceDto
    {
        public decimal Id { get; set; }
        public int Reference { get; set; }
        public string? JobCustomerCode { get; set; }
        public string? JobCustomerName { get; set; }
        public string? InvoiceCustomerCode { get; set; }
        public string? InvoiceCustomerName { get; set; }
        public string? Name { get; set; }
        public DateTime? IssueDate { get; set; }
        public decimal GrossAmount { get; set; }
        public string? StateName { get; set; }
        public bool Evaluated { get; set; }
    }
}
