namespace Imaj.Core.Entities
{
    public class InvoiceQry : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public short Stamp { get; set; }
        public int? Reference1 { get; set; }
        public int? Reference2 { get; set; }
        public decimal? InvoCustomerID { get; set; }
        public decimal? JobCustomerID { get; set; }
        public decimal? StateID { get; set; }
        public decimal? EvaluatedID { get; set; }
        public DateTime? IssueDate1 { get; set; }
        public DateTime? IssueDate2 { get; set; }
    }
}
