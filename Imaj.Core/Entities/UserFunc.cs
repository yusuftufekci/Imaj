namespace Imaj.Core.Entities
{
    public class UserFunc : BaseEntity
    {
        public decimal UserID { get; set; }
        public decimal FunctionID { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual User? User { get; set; }
        public virtual Function? Function { get; set; }
    }
}
