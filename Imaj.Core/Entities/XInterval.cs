namespace Imaj.Core.Entities
{
    public class XInterval : BaseEntity
    {
        public decimal IntervalID { get; set; }
        public decimal LanguageID { get; set; }
        public string Name { get; set; } = string.Empty;
        public short Stamp { get; set; }

        public virtual Interval? Interval { get; set; }
        public virtual Language? Language { get; set; }
    }
}
