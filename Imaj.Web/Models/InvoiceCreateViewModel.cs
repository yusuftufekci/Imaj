using System;
using System.Collections.Generic;

namespace Imaj.Web.Models
{
    public class InvoiceCreateViewModel
    {
        // Header Info
        public string? Reference { get; set; }
        public string Source { get; set; } = "101PROD - 101 PRODUCTION"; // This seems to be fixed/default in the screenshot context or current user context
        
        // Job Customer (Readonly context)
        public string? JobCustomerCode { get; set; }
        public string? JobCustomerName { get; set; }

        // Invoice Customer (Selectable)
        public string? InvoiceCustomerCode { get; set; }
        public string? InvoiceCustomerName { get; set; }

        // Other Fields
        public string? Ad { get; set; } // 'Ad' field from screenshot
        public string? RelatedPerson { get; set; } // 'Ilgili'
        public DateTime IssueDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Açık"; // Default Open
        public bool IsEvaluated { get; set; }

        // Notes
        public string? Notes { get; set; }
        public string Footnote { get; set; } = "TV Filmleri Yapim ve Hizmet Giderleri Bedeli"; // Default text from screenshot

        // Lines
        public List<InvoiceLineViewModel> Lines { get; set; } = new List<InvoiceLineViewModel>();
    }

    public class InvoiceLineViewModel
    {
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public decimal VatRate { get; set; } = 18; // Default VAT
        public decimal Total => Amount + (Amount * VatRate / 100);
    }
}
