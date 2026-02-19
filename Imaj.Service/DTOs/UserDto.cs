using System.Collections.Generic;

namespace Imaj.Service.DTOs
{
    public class UserDto
    {
        public decimal Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? Role { get; set; }
        public decimal? CompanyId { get; set; }
        public bool AllEmployee { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
