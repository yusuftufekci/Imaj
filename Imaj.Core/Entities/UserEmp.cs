namespace Imaj.Core.Entities
{
    public class UserEmp : BaseEntity
    {
        public decimal UserID { get; set; }
        public decimal EmployeeID { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual User? User { get; set; }
        public virtual Employee? Employee { get; set; }
    }
}
