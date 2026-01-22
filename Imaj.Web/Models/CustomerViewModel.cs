using System.ComponentModel.DataAnnotations;

namespace Imaj.Web.Models
{
    public class CustomerViewModel
    {
        // This can serve as the Detail/Edit model too
        public string Code { get; set; }
        public string Name { get; set; }
        public bool IsInvalid { get; set; }

        public string City { get; set; }
        public string Address { get; set; } // Added
        public string AreaCode { get; set; }
        public string Country { get; set; }

        public string Owner { get; set; }
        public string RelatedPerson { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }

        public string InvoiceName { get; set; } // Added
        public string TaxOffice { get; set; }
        public string TaxNumber { get; set; }

        public string JobStatus { get; set; }
        public string Notes { get; set; } // Added
    }

    public class CustomerListViewModel
    {
        public List<CustomerViewModel> Items { get; set; } = new List<CustomerViewModel>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public CustomerFilterModel Filter { get; set; } // To maintain filter state in pagination links
    }
}
