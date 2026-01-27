namespace Imaj.Core.Entities
{
    /// <summary>
    /// Çalışan-Fonksiyon ilişki tablosu.
    /// Employee ve Function arasında many-to-many ilişki kurar.
    /// </summary>
    public class EmpFunc : BaseEntity
    {
        // Id is inherited from BaseEntity
        public decimal EmployeeID { get; set; }
        public decimal FunctionID { get; set; }
        public bool WorkAmountUpdate { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
        
        // Navigation properties
        public virtual Employee? Employee { get; set; }
        public virtual Function? Function { get; set; }
    }
}
