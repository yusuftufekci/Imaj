using System.Collections.Generic;

namespace Imaj.Service.DTOs
{
    public class InvoiceHistoryDto
    {
        public InvoiceDetailDto? Detail { get; set; }
        public List<InvoiceLogDto> Items { get; set; } = new List<InvoiceLogDto>();
    }
}
