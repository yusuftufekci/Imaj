namespace Imaj.Core.Entities
{
    public class XWorkType : BaseEntity
    {
        public decimal WorkTypeID { get; set; }
        public decimal LanguageID { get; set; }
        public string Name { get; set; } = string.Empty;
        public short Stamp { get; set; }

        public virtual WorkType? WorkType { get; set; }
        public virtual Language? Language { get; set; }
    }
}
