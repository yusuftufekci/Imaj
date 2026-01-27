namespace Imaj.Core.Entities
{
    public class InvoLine : BaseEntity
    {
        public decimal InvoiceID { get; set; }
        public bool FreeFormat { get; set; }
        public short Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public string Notes { get; set; } = string.Empty; // ntext
        public short Sequence { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
        public decimal? TaxTypeID { get; set; }
        
        public virtual Invoice? Invoice { get; set; }
        public virtual TaxType? TaxType { get; set; }
    }
}
