
using System;

namespace Imaj.Service.DTOs
{
    public class ProductFilterDto
    {
        public string? Code { get; set; }
        public string? Category { get; set; }
        public string? ProductGroup { get; set; }
        public string? Function { get; set; }
        public bool IsInvalid { get; set; }
        
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        
        // Sorting? Default by Code/Name usually.
    }
}
