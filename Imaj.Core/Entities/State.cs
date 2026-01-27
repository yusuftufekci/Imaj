namespace Imaj.Core.Entities
{
    public class State : BaseEntity
    {
        public string Category { get; set; } = string.Empty;
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
    }
}
