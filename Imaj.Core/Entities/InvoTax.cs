namespace Imaj.Core.Entities
{
    public class InvoTax : BaseEntity
    {
        public decimal InvoiceID { get; set; }
        public decimal TaxTypeID { get; set; }
        public decimal GrossAmount { get; set; }
        public short TaxPercentage { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal Deleted { get; set; }
        public short Stamp { get; set; }

        public virtual Invoice? Invoice { get; set; }
        public virtual TaxType? TaxType { get; set; }
    }
}
