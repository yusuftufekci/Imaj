namespace Imaj.Core.Entities
{
    public class XReason : BaseEntity
    {
        public decimal ReasonID { get; set; }
        public decimal LanguageID { get; set; }
        public string Name { get; set; } = string.Empty;
        public short Stamp { get; set; }

        public virtual Reason? Reason { get; set; }
        public virtual Language? Language { get; set; }
    }
}
