using System;
using System.Collections.Generic;

namespace Imaj.Web.Models
{
    public class InvoiceDisplayViewModel
    {
        public List<InvoiceDetailViewModel> Invoices { get; set; } = new List<InvoiceDetailViewModel>();
        public List<string> SelectedReferences { get; set; } = new List<string>();
        public int CurrentIndex { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class InvoiceDetailViewModel
    {
        public string Reference { get; set; } = string.Empty;
        public string JobCustomer { get; set; } = string.Empty;
        public string InvoiceCustomer { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string RelatedPerson { get; set; } = string.Empty;
        public DateTime? IssueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool Evaluated { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string FooterNote { get; set; } = string.Empty;

        public List<InvoiceDetailLineViewModel> Lines { get; set; } = new List<InvoiceDetailLineViewModel>();
        public List<InvoiceWorkItemViewModel> WorkItems { get; set; } = new List<InvoiceWorkItemViewModel>();
        public List<InvoiceCategorySummaryViewModel> ProductCategories { get; set; } = new List<InvoiceCategorySummaryViewModel>();
        public List<InvoiceTaxSummaryViewModel> Taxes { get; set; } = new List<InvoiceTaxSummaryViewModel>();
    }

    public class InvoiceDetailLineViewModel
    {
        public bool Selected { get; set; }
        public int LineNo { get; set; }
        public string Notes { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public string TaxType { get; set; } = string.Empty;
    }

    public class InvoiceWorkItemViewModel
    {
        public bool Selected { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class InvoiceCategorySummaryViewModel
    {
        public string Name { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal NetTotal { get; set; }
    }

    public class InvoiceTaxSummaryViewModel
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal Rate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetTotal { get; set; }
    }
}
