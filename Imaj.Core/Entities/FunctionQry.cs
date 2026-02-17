namespace Imaj.Core.Entities
{
    public class FunctionQry : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public short Stamp { get; set; }
        public string ExceptIDList { get; set; } = string.Empty;
        public decimal? ReservableID { get; set; }
        public decimal? IntervalID { get; set; }
        public decimal? InvisibleID { get; set; }
        public decimal? SortLangID { get; set; }
    }
}
