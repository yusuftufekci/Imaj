namespace Imaj.Core.Entities
{
    public class XReasonCat : BaseEntity
    {
        public decimal ReasonCatID { get; set; }
        public decimal LanguageID { get; set; }
        public string Name { get; set; } = string.Empty;
        public short Stamp { get; set; }

        public virtual ReasonCat? ReasonCat { get; set; }
        public virtual Language? Language { get; set; }
    }
}
