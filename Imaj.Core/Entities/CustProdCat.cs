namespace Imaj.Core.Entities
{
    public class CustProdCat : BaseEntity
    {
        public decimal CustomerID { get; set; }
        public decimal ProdCatID { get; set; }
        public byte DiscPercentage { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual Customer? Customer { get; set; }
        public virtual ProdCat? ProdCat { get; set; }
    }
}
