namespace Imaj.Core.Entities
{
    public class ReserveLog : BaseEntity
    {
        public decimal ReserveID { get; set; }
        public decimal UserID { get; set; }
        public decimal LogActionID { get; set; }
        public DateTime ActionDT { get; set; }
        public short Stamp { get; set; }

        public virtual Reserve? Reserve { get; set; }
        public virtual User? User { get; set; }
    }
}
