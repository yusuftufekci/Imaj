namespace Imaj.Core.Entities
{
    public class IDCounter : BaseEntity
    {
        public string TableName { get; set; } = string.Empty;
        public decimal Counter { get; set; }
    }
}
