namespace Imaj.Core.Entities
{
    public class InvoProdCat : BaseEntity
    {
        public decimal InvoiceID { get; set; }
        public decimal ProdCatID { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual Invoice? Invoice { get; set; }
        public virtual ProdCat? ProdCat { get; set; }
    }
}
