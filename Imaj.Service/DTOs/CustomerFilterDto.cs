namespace Imaj.Service.DTOs
{
    public class CustomerFilterDto
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? City { get; set; }
        public string? AreaCode { get; set; }
        public string? Country { get; set; }
        public string? Owner { get; set; }
        public string? RelatedPerson { get; set; } // Maps to Contact
        public string? Phone { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? TaxOffice { get; set; }
        public string? TaxNumber { get; set; }
        public string? JobStatus { get; set; } // Active, Completed
        public decimal? JobStateId { get; set; } // Job State ID filter
        public bool? IsInvalid { get; set; } // Maps to Invisible or !IsActive
        
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
