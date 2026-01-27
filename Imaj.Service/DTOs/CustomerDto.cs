using System.ComponentModel.DataAnnotations;

namespace Imaj.Service.DTOs
{
    /// <summary>
    /// Müşteri veri transfer nesnesi.
    /// </summary>
    public class CustomerDto
    {
        public decimal Id { get; set; }
        
        // Zorunlu alanlar (DB: NOT NULL, kullanıcı girmeli)
        [Required(ErrorMessage = "Kod alanı zorunludur.")]
        [MaxLength(8, ErrorMessage = "Kod en fazla 8 karakter olabilir.")]
        public string Code { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [MaxLength(32, ErrorMessage = "Ad en fazla 32 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;
        
        // Opsiyonel alanlar (DB: NOT NULL ama boş string kabul eder)
        public string? City { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Contact { get; set; }
        public string? TaxOffice { get; set; }
        public string? TaxNumber { get; set; }
        public string? InvoiceName { get; set; }
        public string? Country { get; set; }
        public string? Address { get; set; }
        public string? AreaCode { get; set; }
        public string? Fax { get; set; }
        public string? Notes { get; set; }
        public string? Owner { get; set; }
        public bool SelectFlag { get; set; }
    }
}
