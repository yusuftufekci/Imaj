namespace Imaj.Core.Entities
{
    public class ReserveQry : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public decimal OwnUserID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public short Stamp { get; set; }
        public decimal? ResourceID { get; set; }
        public decimal? EvaluatedID { get; set; }
        public decimal? CustomerID { get; set; }
        public decimal? ReasonID { get; set; }
        public decimal? StateID { get; set; }
        public decimal? AbsenceID { get; set; }
        public decimal? FunctionID { get; set; }
        public DateTime? StartDate1 { get; set; }
        public DateTime? StartDate2 { get; set; }
        public DateTime? EndDate1 { get; set; }
        public DateTime? EndDate2 { get; set; }
    }
}
