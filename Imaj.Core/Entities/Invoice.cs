namespace Imaj.Core.Entities
{
    public class Invoice : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public decimal JobCustomerID { get; set; }
        public decimal InvoCustomerID { get; set; }
        public decimal StateID { get; set; }
        public int Reference { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty; // ntext
        public string Footer { get; set; } = string.Empty; // ntext
        public decimal NetAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrossAmount { get; set; }
        public bool Evaluated { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
        public DateTime? IssueDate { get; set; } // smalldatetime
        
        public virtual Company? Company { get; set; }
        public virtual Customer? JobCustomer { get; set; }
        public virtual Customer? InvoCustomer { get; set; }
        public virtual State? State { get; set; }
    }
}
