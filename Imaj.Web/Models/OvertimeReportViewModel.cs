using System.ComponentModel.DataAnnotations;

namespace Imaj.Web.Models
{
    public class OvertimeReportViewModel
    {
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(7);

        public string? SelectedEmployeeName { get; set; }
        public List<string> SelectedEmployeeCodes { get; set; } = new List<string>();

        public string? SelectedCustomerName { get; set; }
        public string? SelectedCustomerCode { get; set; }
    }

    public class CustomerFilterModel
    {
        // Main Criteria
        public string? Code { get; set; }
        public string? Name { get; set; }
        public bool? IsInvalid { get; set; } // Nullable: Tümü seçeneği için null olabilir

        // Address
        public string? City { get; set; }
        public string? AreaCode { get; set; }
        public string? Country { get; set; }

        // Contact
        public string? Owner { get; set; }
        public string? RelatedPerson { get; set; }
        public string? Phone { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }

        // Invoice
        public string? TaxOffice { get; set; }
        public string? TaxNumber { get; set; }

        // Job
        public string? JobStatus { get; set; }
        
        // Paging
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class CustomerSearchResult
    {
        public decimal Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? City { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
    public class EmployeeSearchResult
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
    }
}
