namespace Imaj.Core.Entities
{
    public class TimeTypeQry : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public string ExceptIDList { get; set; } = string.Empty;
        public short Stamp { get; set; }
        public decimal? SortLangID { get; set; }
        public decimal? InvisibleID { get; set; }
    }
}
