namespace Imaj.Core.Entities
{
    public class XResource : BaseEntity
    {
        public decimal ResourceID { get; set; }
        public decimal LanguageID { get; set; }
        public string Name { get; set; } = string.Empty;
        public short Stamp { get; set; }

        public virtual Resource? Resource { get; set; }
        public virtual Language? Language { get; set; }
    }
}
