namespace Imaj.Service.DTOs
{
    public class CustomerDto
    {
        public decimal Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? City { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Contact { get; set; }
        public string? TaxOffice { get; set; }
        public string? TaxNumber { get; set; }
        public string? InvoiceName { get; set; } // Added
        public string? Country { get; set; }
        public string? Address { get; set; }
        public string? AreaCode { get; set; } // Added (maps to Zip)
        public string? Fax { get; set; } // Added
        public string? Notes { get; set; }
        public string? Owner { get; set; }
        public bool SelectFlag { get; set; }
    }
}
