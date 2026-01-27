namespace Imaj.Core.Entities
{
    public class XProdCat : BaseEntity
    {
        public decimal ProdCatID { get; set; }
        public decimal LanguageID { get; set; }
        public string Name { get; set; } = string.Empty;
        public short Stamp { get; set; }

        public virtual ProdCat? ProdCat { get; set; }
        public virtual Language? Language { get; set; }
    }
}
