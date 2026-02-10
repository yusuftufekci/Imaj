using System;

namespace Imaj.Service.DTOs
{
    public class InvoiceLogDto
    {
        public decimal Id { get; set; }
        public DateTime LogDate { get; set; }

        public decimal UserId { get; set; }
        public string UserCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public decimal LogActionId { get; set; }
        public string ActionName { get; set; } = string.Empty;
    }
}
