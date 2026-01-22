using System.ComponentModel.DataAnnotations;

namespace Imaj.Web.Models
{
    public class InvoiceViewModel
    {
        // Filters
        public string? JobCustomerCode { get; set; }
        public string? JobCustomerName { get; set; }

        public string? InvoiceCustomerCode { get; set; }
        public string? InvoiceCustomerName { get; set; }

        public string? ReferenceStart { get; set; }
        public string? ReferenceEnd { get; set; }

        public string? Name { get; set; }
        public string? RelatedPerson { get; set; }

        [DataType(DataType.Date)]
        public DateTime? IssueDateStart { get; set; }

        [DataType(DataType.Date)]
        public DateTime? IssueDateEnd { get; set; }

        public string? Status { get; set; }
        public string? Evaluated { get; set; }

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }

        public List<InvoiceSearchResult> Items { get; set; } = new List<InvoiceSearchResult>();

        // For Creation Section
        public string? NewJobCustomerCode { get; set; }
        public string? NewJobCustomerName { get; set; }
    }

    public class InvoiceSearchResult
    {
        public string? Reference { get; set; }
        public string? JobCustomer { get; set; }
        public string? InvoiceCustomer { get; set; }
        public DateTime IssueDate { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
    }
}
