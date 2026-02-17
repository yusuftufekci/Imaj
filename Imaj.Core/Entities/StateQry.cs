namespace Imaj.Core.Entities
{
    public class StateQry : BaseEntity
    {
        public string Category { get; set; } = string.Empty;
        public string ExceptIDList { get; set; } = string.Empty;
        public short Stamp { get; set; }
    }
}
