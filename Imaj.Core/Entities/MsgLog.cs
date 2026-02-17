namespace Imaj.Core.Entities
{
    public class MsgLog : BaseEntity
    {
        public decimal UserID { get; set; }
        public DateTime LogDT { get; set; }
        public Guid MsgSessionID { get; set; }
        public Guid MsgInstanceID { get; set; }
        public string Interface { get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;
        public int Number { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public short CallCount { get; set; }
        public decimal MemberID { get; set; }
        public bool ActionMethod { get; set; }
        public bool Turkuaz { get; set; }
        public byte MsgType { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
    }
}
