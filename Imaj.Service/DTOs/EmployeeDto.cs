using System;

namespace Imaj.Service.DTOs
{
    public class EmployeeDto
    {
        public decimal Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }
}
