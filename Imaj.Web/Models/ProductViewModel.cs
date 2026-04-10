using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Imaj.Web.Models
{
    public class ProductReportViewModel
    {
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(7);

        public string? SelectedProductCode { get; set; }
        public string? SelectedProductName { get; set; }

        public string? SelectedCustomerCode { get; set; } // Reusing for Customer selection
        public string? SelectedCustomerName { get; set; }

        public string? ProductGroup { get; set; } // For the dropdown on main page
    }

    public class ProductReportDownloadRequest
    {
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        public string? ProductGroup { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
    }

    public class ProductFilterModel
    {
        public string? Code { get; set; }
        public string? Category { get; set; } // Ürün Kategorisi
        public string? ProductGroup { get; set; } // Ürün Grubu
        public string? Function { get; set; } // Fonksiyon
        public bool? IsInvalid { get; set; }
        
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16; // Matching image
    }

    public class ProductSearchResult
    {
        public decimal Id { get; set; }
        public decimal CategoryId { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; } // Ad / Tanım
        public string? Category { get; set; }
        public string? ProductGroup { get; set; }
        public decimal Price { get; set; }
    }
}
