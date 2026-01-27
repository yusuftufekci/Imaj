namespace Imaj.Core.Entities
{
    public class InvoJob : BaseEntity
    {
        public decimal InvoiceID { get; set; }
        public decimal JobID { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual Invoice? Invoice { get; set; }
        public virtual Job? Job { get; set; }
    }
}
