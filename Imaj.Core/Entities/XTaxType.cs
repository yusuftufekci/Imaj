namespace Imaj.Core.Entities
{
    public class XTaxType : BaseEntity
    {
        public decimal TaxTypeID { get; set; }
        public decimal LanguageID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string InvoLinePostfix { get; set; } = string.Empty;
        public short Stamp { get; set; }

        public virtual TaxType? TaxType { get; set; }
        public virtual Language? Language { get; set; }
    }
}
