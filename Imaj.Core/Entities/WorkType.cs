namespace Imaj.Core.Entities
{
    /// <summary>
    /// Çalışma tipi (iş türü) tanımları.
    /// Company'ye FK var.
    /// </summary>
    public class WorkType : BaseEntity
    {
        // Id is inherited from BaseEntity
        public decimal CompanyID { get; set; }
        public bool Invisible { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
        
        // Navigation properties
        public virtual Company? Company { get; set; }
        public virtual ICollection<EmpWork> EmpWorks { get; set; } = new List<EmpWork>();
    }
}
