namespace Imaj.Core.Entities
{
    public class LogAction : BaseEntity
    {
        public string SrvName { get; set; } = string.Empty;
        public string Descr { get; set; } = string.Empty; // ntext
        public short Stamp { get; set; }
    }
}
