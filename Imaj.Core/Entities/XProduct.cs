namespace Imaj.Core.Entities
{
    public class XProduct : BaseEntity
    {
        public decimal ProductID { get; set; }
        public decimal LanguageID { get; set; }
        public string Name { get; set; } = string.Empty;
        public short Stamp { get; set; }

        public virtual Product? Product { get; set; }
        public virtual Language? Language { get; set; }
    }
}
