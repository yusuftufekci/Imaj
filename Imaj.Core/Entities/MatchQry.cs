namespace Imaj.Core.Entities
{
    public class MatchQry : BaseEntity
    {
        public decimal FunctionID { get; set; }
        public DateTime AtomicDT1 { get; set; }
        public DateTime AtomicDT2 { get; set; }
        public string ResourceIDList { get; set; } = string.Empty;
        public string ExceptReserveIDList { get; set; } = string.Empty;
        public short Stamp { get; set; }
    }
}
