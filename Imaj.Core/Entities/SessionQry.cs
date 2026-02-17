namespace Imaj.Core.Entities
{
    public class SessionQry : BaseEntity
    {
        public short Stamp { get; set; }
        public decimal? StateID { get; set; }
        public Guid? SessionID { get; set; }
    }
}
