namespace Imaj.Core.Entities
{
    public class Job : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public decimal FunctionID { get; set; }
        public decimal CustomerID { get; set; }
        public decimal StateID { get; set; }
        public decimal ProdSum { get; set; }
        public decimal WorkSum { get; set; }
        public int Reference { get; set; }
        public DateTime StartDT { get; set; } // smalldatetime
        public DateTime EndDT { get; set; } // smalldatetime
        public string Name { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string IntNotes { get; set; } = string.Empty; // ntext
        public string ExtNotes { get; set; } = string.Empty; // ntext
        public bool Evaluated { get; set; }
        public bool Mailed { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
        public decimal? InvoLineID { get; set; }
        
        public virtual Company? Company { get; set; }
        public virtual Function? Function { get; set; }
        public virtual Customer? Customer { get; set; }
        public virtual State? State { get; set; }
        public virtual InvoLine? InvoLine { get; set; }
    }
}
