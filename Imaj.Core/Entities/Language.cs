namespace Imaj.Core.Entities
{
    public class Language : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public short Sequence { get; set; }
        public bool Base { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
    }
}
