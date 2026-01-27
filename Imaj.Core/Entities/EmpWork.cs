namespace Imaj.Core.Entities
{
    /// <summary>
    /// Çalışan-Çalışma Tipi ilişki tablosu.
    /// Employee ve WorkType arasında many-to-many ilişki kurar.
    /// </summary>
    public class EmpWork : BaseEntity
    {
        // Id is inherited from BaseEntity
        public decimal EmployeeID { get; set; }
        public decimal WorkTypeID { get; set; }
        public bool Default { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
        
        // Navigation properties
        public virtual Employee? Employee { get; set; }
        public virtual WorkType? WorkType { get; set; }
    }
}
