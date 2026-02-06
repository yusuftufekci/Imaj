using System;

namespace Imaj.Service.DTOs
{
    public class InvoiceFilterDto
    {
        public string? JobCustomerCode { get; set; }
        public string? JobCustomerName { get; set; }
        public string? InvoiceCustomerCode { get; set; }
        public string? InvoiceCustomerName { get; set; }
        public int? ReferenceStart { get; set; }
        public int? ReferenceEnd { get; set; }
        public string? Name { get; set; }
        public string? RelatedPerson { get; set; }
        public DateTime? IssueDateStart { get; set; }
        public DateTime? IssueDateEnd { get; set; }
        public decimal? StateId { get; set; }
        public bool? Evaluated { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
