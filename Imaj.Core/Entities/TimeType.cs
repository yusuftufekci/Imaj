namespace Imaj.Core.Entities
{
    /// <summary>
    /// Zaman tipi (çalışma zamanı türü) tanımları.
    /// Company'ye FK var.
    /// </summary>
    public class TimeType : BaseEntity
    {
        // Id is inherited from BaseEntity
        public decimal CompanyID { get; set; }
        public bool Invisible { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
        
        // Navigation properties
        public virtual Company? Company { get; set; }
        public virtual ICollection<EmpTime> EmpTimes { get; set; } = new List<EmpTime>();
    }
}
