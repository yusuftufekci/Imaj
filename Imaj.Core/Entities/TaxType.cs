namespace Imaj.Core.Entities
{
    public class TaxType : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public string Code { get; set; } = string.Empty;
        public short TaxPercentage { get; set; }
        public bool Invisible { get; set; }
        public bool SelectFlag { get; set; }
        public short SelectQty { get; set; }
        public short Stamp { get; set; }

        public virtual Company? Company { get; set; }
    }
}
