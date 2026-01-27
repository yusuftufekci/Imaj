namespace Imaj.Core.Entities
{
    public class BaseCont : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
    }
}
