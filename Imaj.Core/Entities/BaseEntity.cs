using System;

namespace Imaj.Core.Entities
{
    public abstract class BaseEntity
    {
        public decimal Id { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}
