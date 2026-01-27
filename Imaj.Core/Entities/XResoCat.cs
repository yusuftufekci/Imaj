namespace Imaj.Core.Entities
{
    public class XResoCat : BaseEntity
    {
        public decimal ResoCatID { get; set; }
        public decimal LanguageID { get; set; }
        public string Name { get; set; } = string.Empty;
        public short Stamp { get; set; }

        public virtual ResoCat? ResoCat { get; set; }
        public virtual Language? Language { get; set; }
    }
}
