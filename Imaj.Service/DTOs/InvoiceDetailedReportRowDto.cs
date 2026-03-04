using System;

namespace Imaj.Service.DTOs
{
    public class InvoiceDetailedReportRowDto
    {
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int Reference { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime? IssueDate { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public bool Evaluated { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal SubTotal { get; set; }
        public decimal NetTotal { get; set; }
    }
}
