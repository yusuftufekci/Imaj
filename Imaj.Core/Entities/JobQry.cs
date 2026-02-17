namespace Imaj.Core.Entities
{
    public class JobQry : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public decimal OwnUserID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string ReferenceList { get; set; } = string.Empty;
        public short Stamp { get; set; }
        public bool FixedCustomer { get; set; }
        public bool FixedState { get; set; }
        public bool FixedEvaluated { get; set; }
        public decimal? WorkTypeID { get; set; }
        public decimal? TimeTypeID { get; set; }
        public decimal? EmployeeID { get; set; }
        public decimal? ProductID { get; set; }
        public decimal? FunctionID { get; set; }
        public decimal? CustomerID { get; set; }
        public decimal? StateID { get; set; }
        public decimal? EvaluatedID { get; set; }
        public decimal? MailedID { get; set; }
        public int? Reference1 { get; set; }
        public int? Reference2 { get; set; }
        public DateTime? StartDate1 { get; set; }
        public DateTime? EndDate1 { get; set; }
        public DateTime? StartDate2 { get; set; }
        public DateTime? EndDate2 { get; set; }
    }
}
