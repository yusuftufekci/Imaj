namespace Imaj.Core.Entities
{
    public class ReasonQry : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public string Code { get; set; } = string.Empty;
        public short Stamp { get; set; }
        public decimal? ReasonCatID { get; set; }
        public decimal? InvisibleID { get; set; }
        public decimal? SortLangID { get; set; }
    }
}
