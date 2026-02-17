namespace Imaj.Core.Entities
{
    public class MsgLogQry : BaseEntity
    {
        public string Interface { get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public string MsgSessionID { get; set; } = string.Empty;
        public string MsgInstanceID { get; set; } = string.Empty;
        public short Stamp { get; set; }
        public int? Number1 { get; set; }
        public int? Number2 { get; set; }
        public decimal? ActionMethodID { get; set; }
        public decimal? TurkuazID { get; set; }
        public DateTime? LogDT1 { get; set; }
        public DateTime? LogDT2 { get; set; }
    }
}
