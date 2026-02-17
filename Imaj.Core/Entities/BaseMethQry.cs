namespace Imaj.Core.Entities
{
    public class BaseMethQry : BaseEntity
    {
        public string ExceptIDList { get; set; } = string.Empty;
        public short Stamp { get; set; }
        public decimal? BaseContID { get; set; }
    }
}
