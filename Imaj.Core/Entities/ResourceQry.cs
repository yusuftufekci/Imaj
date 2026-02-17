namespace Imaj.Core.Entities
{
    public class ResourceQry : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public decimal OwnUserID { get; set; }
        public string Code { get; set; } = string.Empty;
        public short Stamp { get; set; }
        public string ExceptIDList { get; set; } = string.Empty;
        public string ResoCatIDList { get; set; } = string.Empty;
        public bool FunctionSecurity { get; set; }
        public int? Sequence1 { get; set; }
        public int? Sequence2 { get; set; }
        public decimal? FunctionID { get; set; }
        public decimal? InvisibleID { get; set; }
        public decimal? ResoCatID { get; set; }
    }
}
