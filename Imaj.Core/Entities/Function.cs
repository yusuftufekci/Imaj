namespace Imaj.Core.Entities
{
    /// <summary>
    /// Fonksiyon (iş türü) tanımları.
    /// Company ve Interval'a FK var.
    /// </summary>
    public class Function : BaseEntity
    {
        // Id is inherited from BaseEntity
        public decimal CompanyID { get; set; }
        public bool Reservable { get; set; }
        public bool WorkMandatory { get; set; }
        public bool ProdMandatory { get; set; }
        public bool Invisible { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
        public decimal? IntervalID { get; set; }
        
        // Navigation properties
        public virtual Company? Company { get; set; }
        public virtual Interval? Interval { get; set; }
        public virtual ICollection<EmpFunc> EmpFuncs { get; set; } = new List<EmpFunc>();
    }
}
