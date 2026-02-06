using System;

namespace Imaj.Core.Entities
{
    public class JobWork : BaseEntity
    {
        public decimal JobID { get; set; }
        public decimal EmployeeID { get; set; }
        public decimal WorkTypeID { get; set; }
        public decimal TimeTypeID { get; set; }
        public short Quantity { get; set; }
        public decimal Amount { get; set; }
        public string Notes { get; set; } = string.Empty;
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual Job? Job { get; set; }
        public virtual Employee? Employee { get; set; }
        public virtual WorkType? WorkType { get; set; }
        public virtual TimeType? TimeType { get; set; }
    }
}
