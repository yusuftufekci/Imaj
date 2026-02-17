namespace Imaj.Core.Entities
{
    public class JobProdQry : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public short Stamp { get; set; }
        public string JobStateIDList { get; set; } = string.Empty;
        public DateTime? StartDate1 { get; set; }
        public DateTime? StartDate2 { get; set; }
        public decimal? ProductID { get; set; }
        public decimal? CustomerID { get; set; }
        public decimal? ProdGrpID { get; set; }
    }
}
