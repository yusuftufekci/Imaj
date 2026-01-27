namespace Imaj.Core.Entities
{
    /// <summary>
    /// Çalışan (personel) bilgilerini tutan entity.
    /// </summary>
    public class Employee : BaseEntity
    {
        // Id is inherited from BaseEntity
        public decimal CompanyID { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
        public bool SelectFlag { get; set; }
        public short SelectQty { get; set; }
        public short Stamp { get; set; }
        
        // Navigation properties
        public virtual Company? Company { get; set; }
        public virtual ICollection<EmpFunc> EmpFuncs { get; set; } = new List<EmpFunc>();
        public virtual ICollection<EmpTime> EmpTimes { get; set; } = new List<EmpTime>();
        public virtual ICollection<EmpWork> EmpWorks { get; set; } = new List<EmpWork>();
    }
}
