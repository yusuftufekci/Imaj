using System.Globalization;

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
        public string? ReferenceList { get; set; }

        public string? Name { get; set; }
        public string? RelatedPerson { get; set; }

        // HTML <input type="date"> her zaman ISO 8601 (yyyy-MM-dd) formatında değer gönderir.
        // tr-TR kültüründe ASP.NET Core model binding bu formatı DateTime? olarak parse edemez;
        // bu yüzden alanlar string olarak tutulup Controller'da InvariantCulture ile parse edilir.
        public string? IssueDateStart { get; set; }
        public string? IssueDateEnd { get; set; }

        /// <summary>
        /// IssueDateStart string'ini InvariantCulture ile DateTime'a çevirir.
        /// </summary>
        public DateTime? ParsedIssueDateStart =>
            DateTime.TryParseExact(IssueDateStart, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d) ? d : (DateTime?)null;

        /// <summary>
        /// IssueDateEnd string'ini InvariantCulture ile DateTime'a çevirir.
        /// </summary>
        public DateTime? ParsedIssueDateEnd =>
            DateTime.TryParseExact(IssueDateEnd, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d) ? d : (DateTime?)null;

        public string? Status { get; set; }
        public string? Evaluated { get; set; }

        // Pagination
        public int? First { get; set; }
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
        public string? Name { get; set; }
        public DateTime? IssueDate { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public bool Evaluated { get; set; }
    }
}
