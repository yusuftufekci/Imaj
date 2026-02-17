namespace Imaj.Core.Entities
{
    public class CompanyQry : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public short Stamp { get; set; }
        public decimal? InvisibleID { get; set; }
    }
}
