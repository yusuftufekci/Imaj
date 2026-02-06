
using System;

namespace Imaj.Service.DTOs
{
    public class ProductDto
    {
        public decimal Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public decimal CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public decimal GroupId { get; set; }
        public string? GroupName { get; set; }
        public decimal Price { get; set; }
    }
}
