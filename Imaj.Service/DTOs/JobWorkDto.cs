using System;

namespace Imaj.Service.DTOs
{
    public class JobWorkDto
    {
        public decimal Id { get; set; }
        public decimal EmployeeId { get; set; } // Kod (AAKSOY)
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; } // Ad (Adnan Aksoy)
        public decimal WorkTypeId { get; set; }
        public string? WorkTypeName { get; set; } // Görev Tipi
        public decimal TimeTypeId { get; set; }
        public string? TimeTypeName { get; set; } // Mesai Tipi
        public decimal Quantity { get; set; } // Miktar
        public decimal Amount { get; set; } // Tutar
        public string? Notes { get; set; } // Notlar
        public bool SelectFlag { get; set; } // Seçili Checkbox
    }
}
