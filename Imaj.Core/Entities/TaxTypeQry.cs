namespace Imaj.Core.Entities
{
    public class TaxTypeQry : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ExceptIDList { get; set; } = string.Empty;
        public short Stamp { get; set; }
        public decimal? InvisibleID { get; set; }
    }
}
