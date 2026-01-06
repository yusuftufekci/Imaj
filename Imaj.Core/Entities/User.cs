using System;

namespace Imaj.Core.Entities
{
    public class User : BaseEntity
    {
        public string? Username { get; set; }
        public string? PasswordHash { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? Role { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }
}
