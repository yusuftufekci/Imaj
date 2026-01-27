namespace Imaj.Core.Entities
{
    public class JobLog : BaseEntity
    {
        public decimal JobID { get; set; }
        public DateTime LogDT { get; set; } // smalldatetime
        public decimal LogActionID { get; set; }
        public decimal UserID { get; set; }
        public string Machine { get; set; } = string.Empty;
        public short Stamp { get; set; }

        public virtual Job? Job { get; set; }
        public virtual LogAction? LogAction { get; set; }
        public virtual User? User { get; set; }
    }
}
