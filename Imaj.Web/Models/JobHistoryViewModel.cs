using System;
using System.Collections.Generic;

namespace Imaj.Web.Models
{
    /// <summary>
    /// İş geçmişi log kaydı.
    /// </summary>
    public class JobHistoryItem
    {
        public DateTime Date { get; set; }
        public string UserCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    public class JobHistoryViewModel
    {
        public decimal JobId { get; set; }
        
        // Temel Bilgiler
        public string Function { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string RelatedPerson { get; set; } = string.Empty; // İlgili
        public string ContactName { get; set; } = string.Empty; // Ad
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsEmailSent { get; set; }
        public bool IsEvaluated { get; set; }
        public string InvoiceStatus { get; set; } = string.Empty;

        // Notlar
        public string AdminNotes { get; set; } = string.Empty;
        public string CustomerNotes { get; set; } = string.Empty;

        public List<JobHistoryItem> Items { get; set; } = new List<JobHistoryItem>();
    }
}
