using System;

namespace Imaj.Service.DTOs
{
    public class EmployeeFilterDto
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public decimal? FunctionID { get; set; }
        // 0: All, 1: Valid (Invisible=false), 2: Invalid (Invisible=true)
        public int Status { get; set; } = 1;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        
        // Sorting
        public string? SortBy { get; set; }
        public bool SortDesc { get; set; }
    }
}
