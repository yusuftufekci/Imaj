namespace Imaj.Core.Entities
{
    public class LockMaster : BaseEntity
    {
        public Guid ID { get; set; }
        public Guid InstanceID { get; set; }
        public Guid SessionID { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public DateTime LastAccess { get; set; }
        public short Timeout { get; set; }
    }
}
