namespace Imaj.Core.Entities
{
    public class JobProdCat : BaseEntity
    {
        public decimal JobID { get; set; }
        public decimal ProdCatID { get; set; }
        public decimal GrossAmount { get; set; }
        public byte DiscPercentage { get; set; }
        public decimal DiscAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal Deleted { get; set; }
        public short Stamp { get; set; }

        public virtual Job? Job { get; set; }
        public virtual ProdCat? ProdCat { get; set; }
    }
}
