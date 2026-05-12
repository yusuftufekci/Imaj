using System;

namespace Imaj.Service.DTOs
{
    public class InvoicePricedJobFilterDto
    {
        public string? JobCustomerCode { get; set; }
        public string? Search { get; set; }
        public int First { get; set; } = 100;
    }

    public class InvoicePricedJobDto
    {
        public int Reference { get; set; }
        public string? Name { get; set; }
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal WorkAmount { get; set; }
        public decimal ProductAmount { get; set; }
    }
}
