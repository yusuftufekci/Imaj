namespace Imaj.Core.Entities
{
    /// <summary>
    /// Interval (zaman aralığı) tanımları.
    /// Function tablosuna referans veriyor.
    /// </summary>
    public class Interval : BaseEntity
    {
        // Id is inherited from BaseEntity
        public short Stamp { get; set; }
        
        // Navigation properties
        public virtual ICollection<Function> Functions { get; set; } = new List<Function>();
    }
}
