namespace Imaj.Core.Entities
{
    public class InvoiceLog : BaseEntity
    {
        public decimal InvoiceID { get; set; }
        public decimal UserID { get; set; }
        public decimal LogActionID { get; set; }
        public DateTime ActionDT { get; set; }
        public short Stamp { get; set; }

        public virtual Invoice? Invoice { get; set; }
        public virtual User? User { get; set; }
    }
}
