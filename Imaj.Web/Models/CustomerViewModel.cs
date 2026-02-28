using System.ComponentModel.DataAnnotations;

namespace Imaj.Web.Models
{
    public class CustomerViewModel
    {
        public decimal CustomerId { get; set; } // Renamed from Id to avoid binding conflict with route 'id' (which is code)
        
        // This can serve as the Detail/Edit model too
        [Required(ErrorMessage = "Kod alanı zorunludur.")]
        [StringLength(8, ErrorMessage = "Kod en fazla 8 karakter olabilir.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [StringLength(32, ErrorMessage = "Ad en fazla 32 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;

        public bool IsInvalid { get; set; }

        [Required(ErrorMessage = "Şehir seçimi zorunludur.")]
        [StringLength(32, ErrorMessage = "Şehir en fazla 32 karakter olabilir.")]
        public string? City { get; set; }

        [Required(ErrorMessage = "Adres alanı zorunludur.")]
        public string? Address { get; set; } // ntext
        
        [StringLength(32, ErrorMessage = "Alan kodu en fazla 32 karakter olabilir.")]
        public string? AreaCode { get; set; } // Zip

        [Required(ErrorMessage = "Ülke zorunludur.")]
        [StringLength(32, ErrorMessage = "Ülke en fazla 32 karakter olabilir.")]
        public string? Country { get; set; }

        [StringLength(32, ErrorMessage = "Sahibi en fazla 32 karakter olabilir.")]
        public string? Owner { get; set; }
        
        [StringLength(32, ErrorMessage = "İlgili kişi en fazla 32 karakter olabilir.")]
        public string? RelatedPerson { get; set; } // Contact
        
        [StringLength(32, ErrorMessage = "Telefon en fazla 32 karakter olabilir.")]
        public string? Phone { get; set; }
        
        [StringLength(32, ErrorMessage = "Faks en fazla 32 karakter olabilir.")]
        public string? Fax { get; set; }
        
        [StringLength(64, ErrorMessage = "E-Mail en fazla 64 karakter olabilir.")]
        public string? Email { get; set; }

        [StringLength(64, ErrorMessage = "Fatura Adı en fazla 64 karakter olabilir.")]
        public string? InvoiceName { get; set; }
        
        [Required(ErrorMessage = "Vergi Dairesi zorunludur.")]
        [StringLength(32, ErrorMessage = "Vergi Dairesi en fazla 32 karakter olabilir.")]
        public string? TaxOffice { get; set; }
        
        [Required(ErrorMessage = "Vergi Numarası zorunludur.")]
        [StringLength(32, ErrorMessage = "Vergi Numarası en fazla 32 karakter olabilir.")]
        public string? TaxNumber { get; set; }

        public string? JobStatus { get; set; }
        public string? Notes { get; set; } // ntext

        public List<ProductCategoryViewModel> ProductCategories { get; set; } = new List<ProductCategoryViewModel>();
    }

    public class ProductCategoryViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Discount { get; set; }
    }

    public class CustomerListViewModel
    {
        public List<CustomerViewModel> Items { get; set; } = new List<CustomerViewModel>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int? First { get; set; }
        public int TotalCount { get; set; }
        public CustomerFilterModel? Filter { get; set; } // To maintain filter state in pagination links
    }
}
