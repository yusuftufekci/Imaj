namespace Imaj.Core.Entities
{
    public class JobWorkQry : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public decimal OwnUserID { get; set; }
        public bool AllEmployee { get; set; }
        public string JobStateIDList { get; set; } = string.Empty;
        public short? Stamp { get; set; }
        public decimal? CustomerID { get; set; }
        public decimal? EmployeeID { get; set; }
        public DateTime? StartDate1 { get; set; }
        public DateTime? StartDate2 { get; set; }
    }
}
