namespace Imaj.Core.Entities
{
    public class Sort : BaseEntity
    {
        public string Category { get; set; } = string.Empty;
        public short Stamp { get; set; }
    }
}
