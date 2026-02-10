using System;
using System.Collections.Generic;

namespace Imaj.Web.Models
{
    public class InvoiceHistoryItem
    {
        public DateTime Date { get; set; }
        public string UserCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    public class InvoiceHistoryViewModel
    {
        public InvoiceDetailViewModel Invoice { get; set; } = new InvoiceDetailViewModel();
        public List<InvoiceHistoryItem> Items { get; set; } = new List<InvoiceHistoryItem>();
        public string ReturnUrl { get; set; } = "/Invoice/Results";
    }
}
