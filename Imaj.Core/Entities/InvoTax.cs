namespace Imaj.Core.Entities
{
    public class InvoTax : BaseEntity
    {
        public decimal InvoiceID { get; set; }
        public decimal TaxTypeID { get; set; }
        public decimal Amount { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual Invoice? Invoice { get; set; }
        public virtual TaxType? TaxType { get; set; }
    }
}
