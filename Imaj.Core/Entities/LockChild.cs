namespace Imaj.Core.Entities
{
    public class LockChild : BaseEntity
    {
        public Guid ID { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal ServiceID { get; set; }
    }
}
