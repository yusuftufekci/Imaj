namespace Imaj.Core.Entities
{
    public class Match : BaseEntity
    {
        public decimal ReserveID { get; set; }
        public decimal ResourceID { get; set; }
        public decimal AllocateID { get; set; }
        public decimal FunctionID { get; set; }
        public DateTime AtomicDT { get; set; } // smalldatetime
        public decimal Deleted { get; set; }
        public short Stamp { get; set; }
        
        public virtual Reserve? Reserve { get; set; }
        public virtual Resource? Resource { get; set; }
        public virtual Function? Function { get; set; }
        // Allocate navigation property is missing because Allocate entity is not created yet. 
        // Will create Allocate entity next.
        public virtual Allocate? Allocate { get; set; }
    }
}
