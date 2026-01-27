namespace Imaj.Core.Entities
{
    public class Allocate : BaseEntity
    {
        public decimal ReserveID { get; set; }
        public decimal ResourceID { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
        
        public virtual Reserve? Reserve { get; set; }
        public virtual Resource? Resource { get; set; }
    }
}
