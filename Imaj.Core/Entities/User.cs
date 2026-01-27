namespace Imaj.Core.Entities
{
    public class User : BaseEntity
    {
        public decimal LanguageID { get; set; }
        public decimal? CompanyID { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool AllEmployee { get; set; }
        public bool Invisible { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual Language? Language { get; set; }
        public virtual Company? Company { get; set; }
    }
}
