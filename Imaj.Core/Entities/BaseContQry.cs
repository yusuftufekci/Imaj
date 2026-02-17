namespace Imaj.Core.Entities
{
    public class BaseContQry : BaseEntity
    {
        public string ExceptIDList { get; set; } = string.Empty;
        public short Stamp { get; set; }
    }
}
