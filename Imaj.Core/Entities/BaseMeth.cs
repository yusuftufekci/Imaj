namespace Imaj.Core.Entities
{
    public class BaseMeth : BaseEntity
    {
        public decimal BaseContID { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool ReadOnly { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual BaseCont? BaseCont { get; set; }
    }
}
