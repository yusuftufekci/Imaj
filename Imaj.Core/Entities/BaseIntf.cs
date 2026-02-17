namespace Imaj.Core.Entities
{
    public class BaseIntf : BaseEntity
    {
        public decimal BaseContID { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual BaseCont? BaseCont { get; set; }
    }
}
