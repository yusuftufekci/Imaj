namespace Imaj.Core.Entities
{
    public class ProductQry : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public string ExceptIDList { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public short Stamp { get; set; }
        public bool FixedInvisible { get; set; }
        public bool FixedFunction { get; set; }
        public decimal? ProdGrpID { get; set; }
        public decimal? InvisibleID { get; set; }
        public decimal? FunctionID { get; set; }
        public decimal? ProdCatID { get; set; }
    }
}
