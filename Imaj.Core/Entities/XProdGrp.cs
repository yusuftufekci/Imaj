namespace Imaj.Core.Entities
{
    public class XProdGrp : BaseEntity
    {
        public decimal ProdGrpID { get; set; }
        public decimal LanguageID { get; set; }
        public string Name { get; set; } = string.Empty;
        public short Stamp { get; set; }

        public virtual ProdGrp? ProdGrp { get; set; }
        public virtual Language? Language { get; set; }
    }
}
