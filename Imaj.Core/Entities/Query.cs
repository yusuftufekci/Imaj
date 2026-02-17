namespace Imaj.Core.Entities
{
    public class Query : BaseEntity
    {
        public decimal UserID { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string ExceptIDList { get; set; } = string.Empty;
        public string OwnName { get; set; } = string.Empty;
        public short Stamp { get; set; }
        public decimal? UsageID { get; set; }
    }
}
