namespace Imaj.Core.Entities
{
    public class Session : BaseEntity
    {
        public Guid SessionID { get; set; }
        public decimal UserID { get; set; }
        public decimal LanguageID { get; set; }
        public decimal CultureID { get; set; }
        public decimal StateID { get; set; }
        public DateTime LastAccessDT { get; set; }
        public string UserAll { get; set; } = string.Empty;
        public string UserCont { get; set; } = string.Empty;
        public string UserMenu { get; set; } = string.Empty;
        public short Timeout { get; set; }
        public short Stamp { get; set; }
    }
}
