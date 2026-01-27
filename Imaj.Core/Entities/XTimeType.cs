namespace Imaj.Core.Entities
{
    public class XTimeType : BaseEntity
    {
        public decimal TimeTypeID { get; set; }
        public decimal LanguageID { get; set; }
        public string Name { get; set; } = string.Empty;
        public short Stamp { get; set; }

        public virtual TimeType? TimeType { get; set; }
        public virtual Language? Language { get; set; }
    }
}
