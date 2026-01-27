namespace Imaj.Core.Entities
{
    public class ProdCat : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public decimal TaxTypeID { get; set; }
        public bool Invisible { get; set; }
        public bool SelectFlag { get; set; }
        public short Sequence { get; set; }
        public short Stamp { get; set; }

        public virtual Company? Company { get; set; }
        public virtual TaxType? TaxType { get; set; }
    }
}
