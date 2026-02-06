namespace Imaj.Service.DTOs
{
    /// <summary>
    /// İş (Job) filtreleme için DTO.
    /// Liste sorgularında kullanılır.
    /// </summary>
    public class JobFilterDto
    {
        // İş Kriteri
        public decimal? FunctionId { get; set; }
        public string? CustomerCode { get; set; }
        public decimal? CustomerId { get; set; }
        
        public int? ReferenceStart { get; set; }
        public int? ReferenceEnd { get; set; }
        public string? ReferenceList { get; set; } // Virgülle ayrılmış liste
        
        public string? JobName { get; set; }
        public string? RelatedPerson { get; set; } // Contact
        
        public DateTime? StartDateStart { get; set; }
        public DateTime? StartDateEnd { get; set; }
        
        public DateTime? EndDateStart { get; set; }
        public DateTime? EndDateEnd { get; set; }
        
        public decimal? StateId { get; set; }
        public bool? IsEmailSent { get; set; }
        public bool? IsEvaluated { get; set; }
        public bool? HasInvoice { get; set; } // InvoLineID null kontrolü
        
        // Mesai Kriteri (ileri seviye - şimdilik basit tutalım)
        public string? EmployeeCode { get; set; }
        public decimal? WorkTypeId { get; set; }
        public decimal? TimeTypeId { get; set; }
        
        // Ürün Kriteri
        public decimal? ProductId { get; set; }
        public string? ProductCode { get; set; }
        
        // Sayfalama
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Sort { get; set; }
        public bool Ascending { get; set; }
    }
}
