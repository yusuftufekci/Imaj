namespace Imaj.Core.Entities
{
    public class InvoiceLog : BaseEntity
    {
        public decimal InvoiceID { get; set; }
        public DateTime LogDT { get; set; } // smalldatetime
        public decimal LogActionID { get; set; }
        public decimal UserID { get; set; }
        public string Machine { get; set; } = string.Empty;
        public short Stamp { get; set; }

        public virtual Invoice? Invoice { get; set; }
        public virtual LogAction? LogAction { get; set; }
        public virtual User? User { get; set; }
    }
}
