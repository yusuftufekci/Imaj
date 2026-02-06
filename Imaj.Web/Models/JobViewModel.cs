namespace Imaj.Web.Models
{
    public class JobViewModel
    {
        public JobFilterModel Filter { get; set; } = new JobFilterModel();
        public List<JobSearchResult> Items { get; set; } = new List<JobSearchResult>();
        public int TotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        
        // Helper for Dropdowns (could be populated from controller)
        public string? SelectedFunction { get; set; } 
    }

    public class JobFilterModel
    {
        // Job Criteria (İş Kriteri)
        public string? Function { get; set; }
        public string? CustomerCode { get; set; }
        public decimal? CustomerId { get; set; } // Added for ID binding
        public string? CustomerName { get; set; }
        
        public string? ReferenceStart { get; set; }
        public string? ReferenceEnd { get; set; }
        public string? ReferenceList { get; set; }
        
        public string? JobName { get; set; } // Ad
        public string? RelatedPerson { get; set; } // İlgili
        
        public DateTime? StartDateStart { get; set; }
        public DateTime? StartDateEnd { get; set; }
        
        public DateTime? EndDateStart { get; set; }
        public DateTime? EndDateEnd { get; set; }
        
        public string? Status { get; set; }
        public string? EmailSent { get; set; } // E-Posta Yollandı (string to support "Tümü" as null/empty vs "true"/"false")
        public string? Evaluated { get; set; } // Değerlendirildi
        public string? InvoiceStatus { get; set; } // Fatura

        // Overtime Criteria (Mesai Kriteri)
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? TaskType { get; set; } // Görev Tipi
        public string? OvertimeType { get; set; } // Mesai Tipi

        // Product Criteria (Ürün Kriteri)
        public decimal? ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Sort { get; set; }
        public bool Ascending { get; set; }
    }

    public class JobSearchResult
    {
        public string? Code { get; set; } // Reference
        public string? Function { get; set; }
        public string? Name { get; set; }
        public string? CustomerName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public bool IsEmailSent { get; set; }
        public bool IsEvaluated { get; set; }
    }

    public class JobDetailViewModel
    {
        // Navigation Props
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new List<string>();

        // Basic Info
        public string? Code { get; set; } // Referans
        public string? Function { get; set; }
        public string? CustomerName { get; set; }
        public string? RelatedPerson { get; set; }
        public string? Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        
        public bool IsEmailSent { get; set; }
        public bool IsEvaluated { get; set; }
        public string? InvoiceStatus { get; set; }

        // Notes
        public string? AdminNotes { get; set; }
        public string? CustomerNotes { get; set; }

        // Collections
        public List<JobOvertimeItem> Overtimes { get; set; } = new List<JobOvertimeItem>();
        public List<JobProductItem> Products { get; set; } = new List<JobProductItem>();
        public List<JobCategorySummaryItem> ProductCategories { get; set; } = new List<JobCategorySummaryItem>();
        
        public decimal TotalOvertimeAmount { get; set; }
        public decimal TotalProductAmount { get; set; }
        public decimal TotalCategoryAmount { get; set; }
    }
    
    public class JobOvertimeItem 
    {
        public bool IsSelected { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? TaskType { get; set; }
        public string? OvertimeType { get; set; }
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
    }

    public class JobProductItem
    {
         public string? Code { get; set; }
         public string? Name { get; set; }
         
         public decimal Quantity { get; set; }
         public decimal Price { get; set; }
         public decimal SubTotal { get; set; } // Ara Tutar (Gross)
         public decimal NetTotal { get; set; } // Net
         public string? Notes { get; set; }
         public bool IsSelected { get; set; }
    }

    public class JobCategorySummaryItem
    {
        public string? Name { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; } // Oran?
        public decimal DiscountAmount { get; set; }
        public decimal NetTotal { get; set; }
    }

    public class DetailNavigatorViewModel
    {
        public int CurrentIndex { get; set; }
        public int TotalCount { get; set; }
        public string FirstUrl { get; set; } = "#";
        public string Header { get; set; } = "Kayıt Navigasyonu";
        public string PreviousUrl { get; set; } = "#";
        public string NextUrl { get; set; } = "#";
        public string LastUrl { get; set; } = "#";
        public string FormAction { get; set; } = "";
        public List<string> SelectedIds { get; set; } = new List<string>();
    }

    public class JobCreateViewModel
    {
        public string Function { get; set; } = "Mesai";
        public string? Reference { get; set; } // Generated or Input
        public string? PageSize { get; set; } = "16";

        public decimal CustomerId { get; set; } // New
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }

        public string? Name { get; set; }
        public string? RelatedPerson { get; set; }
        
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now;
        
        public string Status { get; set; } = "Açık";
        public bool IsEmailSent { get; set; }
        public bool IsEvaluated { get; set; }
        
        public string? AdminNotes { get; set; }
        public string? CustomerNotes { get; set; }

        public List<JobOvertimeInput> Overtimes { get; set; } = new List<JobOvertimeInput>();
        public List<JobProductInput> Products { get; set; } = new List<JobProductInput>();
    }

    public class JobOvertimeInput
    {
        public decimal EmployeeId { get; set; } // New
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public decimal WorkTypeId { get; set; } // TaskType
        public decimal TimeTypeId { get; set; } // OvertimeType
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
    }

    public class JobProductInput
    {
        public decimal ProductId { get; set; } // New
        public string? Code { get; set; } // To find ProductId
        public string? Name { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        // Amount calculated?
        public string? Notes { get; set; }
    }
}
