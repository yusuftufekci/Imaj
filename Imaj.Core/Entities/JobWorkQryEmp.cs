namespace Imaj.Core.Entities
{
    public class JobWorkQryEmp : BaseEntity
    {
        public decimal JobWorkQryID { get; set; }
        public decimal EmployeeID { get; set; }
        public decimal Deleted { get; set; }
        public short Stamp { get; set; }
    }
}
