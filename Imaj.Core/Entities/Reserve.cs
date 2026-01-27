namespace Imaj.Core.Entities
{
    public class Reserve : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public decimal FunctionID { get; set; }
        public decimal StateID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime StartDT { get; set; }
        public DateTime EndDT { get; set; }
        public bool Absence { get; set; }
        public bool Evaluated { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
        public decimal? CustomerID { get; set; }
        public decimal? ReasonID { get; set; }
        
        public virtual Company? Company { get; set; }
        public virtual Function? Function { get; set; }
        public virtual State? State { get; set; }
        public virtual Customer? Customer { get; set; }
        public virtual Reason? Reason { get; set; }
    }
}
