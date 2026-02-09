namespace Imaj.Core.Entities
{
    public class InvoJob : BaseEntity
    {
        public decimal InvoLineID { get; set; }
        public decimal JobID { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual InvoLine? InvoLine { get; set; }
        public virtual Job? Job { get; set; }
    }
}
