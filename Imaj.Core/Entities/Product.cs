namespace Imaj.Core.Entities
{
    public class Product : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public decimal ProdCatID { get; set; }
        public decimal ProdGrpID { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool Invisible { get; set; }
        public bool SelectFlag { get; set; }
        public short SelectQty { get; set; }
        public short Stamp { get; set; }
        
        public virtual Company? Company { get; set; }
        public virtual ProdCat? ProdCat { get; set; }
        public virtual ProdGrp? ProdGrp { get; set; }
    }
}
