using System;
using System.Collections.Generic;

namespace Imaj.Web.Models
{
    public class InvoiceDisplayViewModel
    {
        public List<InvoiceDetailViewModel> Invoices { get; set; } = new List<InvoiceDetailViewModel>();
        public List<string> SelectedReferences { get; set; } = new List<string>();
        public int CurrentIndex { get; set; }
        public string SourceView { get; set; } = "Detail";
        public string? ReturnUrl { get; set; }
    }

    public class InvoiceDetailViewModel
    {
        public string Reference { get; set; } = string.Empty;
        public string JobCustomerCode { get; set; } = string.Empty;
        public string JobCustomer { get; set; } = string.Empty;
        public string InvoiceCustomerCode { get; set; } = string.Empty;
        public string InvoiceCustomer { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string RelatedPerson { get; set; } = string.Empty;
        public DateTime? IssueDate { get; set; }
        public decimal StateId { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool Evaluated { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string FooterNote { get; set; } = string.Empty;

        public List<InvoiceDetailLineViewModel> Lines { get; set; } = new List<InvoiceDetailLineViewModel>();
        public List<InvoiceWorkItemViewModel> WorkItems { get; set; } = new List<InvoiceWorkItemViewModel>();
        public List<InvoiceCategorySummaryViewModel> ProductCategories { get; set; } = new List<InvoiceCategorySummaryViewModel>();
        public List<InvoiceTaxSummaryViewModel> Taxes { get; set; } = new List<InvoiceTaxSummaryViewModel>();
    }

    public class InvoiceUpdateViewModel
    {
        public int Reference { get; set; }
        public int CurrentIndex { get; set; }
        public string? ReturnUrl { get; set; }
        public List<string> SelectedReferences { get; set; } = new List<string>();
        public string? InvoiceCustomerCode { get; set; }
        public string? InvoiceCustomerName { get; set; }
        public string? Name { get; set; }
        public string? RelatedPerson { get; set; }
        public string? Notes { get; set; }
        public string? FooterNote { get; set; }
        public List<InvoiceUpdateLineViewModel> Lines { get; set; } = new List<InvoiceUpdateLineViewModel>();
        public List<InvoiceUpdateFreeLineViewModel> NewFreeLines { get; set; } = new List<InvoiceUpdateFreeLineViewModel>();
        public List<InvoiceUpdateCategoryViewModel> ProductCategories { get; set; } = new List<InvoiceUpdateCategoryViewModel>();
        public List<InvoiceUpdateTaxViewModel> Taxes { get; set; } = new List<InvoiceUpdateTaxViewModel>();
    }

    public class InvoiceUpdateLineViewModel
    {
        public decimal Id { get; set; }
        public string? Notes { get; set; }
        public decimal Amount { get; set; }
        public decimal VatRate { get; set; }
    }

    public class InvoiceUpdateFreeLineViewModel
    {
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public decimal VatRate { get; set; }
    }

    public class InvoiceUpdateCategoryViewModel
    {
        public decimal ProdCatId { get; set; }
        public decimal NetTotal { get; set; }
    }

    public class InvoiceDetailLineViewModel
    {
        public decimal Id { get; set; }
        public bool Selected { get; set; }
        public bool FreeFormat { get; set; }
        public int LineNo { get; set; }
        public string Notes { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public string TaxType { get; set; } = string.Empty;
        public decimal? TaxTypeId { get; set; }
        public decimal VatRate { get; set; }
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
        public decimal ProdCatId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal NetTotal { get; set; }
    }

    public class InvoiceTaxSummaryViewModel
    {
        public decimal TaxTypeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal Rate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetTotal { get; set; }
    }

    public class InvoiceUpdateTaxViewModel
    {
        public decimal TaxTypeId { get; set; }
        public decimal Rate { get; set; }
    }
}
