namespace Imaj.Core.Entities
{
    public class ResoCatQry : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public short Stamp { get; set; }
        public string ExceptIDList { get; set; } = string.Empty;
        public decimal? InvisibleID { get; set; }
        public decimal? SortLangID { get; set; }
    }
}
