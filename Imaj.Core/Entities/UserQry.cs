namespace Imaj.Core.Entities
{
    public class UserQry : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public short Stamp { get; set; }
        public decimal? InvisibleID { get; set; }
        public decimal? CompanyID { get; set; }
    }
}
