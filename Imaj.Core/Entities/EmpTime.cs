namespace Imaj.Core.Entities
{
    /// <summary>
    /// Çalışan-Zaman Tipi ilişki tablosu.
    /// Employee ve TimeType arasında many-to-many ilişki kurar.
    /// </summary>
    public class EmpTime : BaseEntity
    {
        // Id is inherited from BaseEntity
        public decimal EmployeeID { get; set; }
        public decimal TimeTypeID { get; set; }
        public bool Default { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
        
        // Navigation properties
        public virtual Employee? Employee { get; set; }
        public virtual TimeType? TimeType { get; set; }
    }
}
