namespace Imaj.Core.Entities
{
    public class XFunction : BaseEntity
    {
        public decimal FunctionID { get; set; }
        public decimal LanguageID { get; set; }
        public string Name { get; set; } = string.Empty;
        public short Stamp { get; set; }

        public virtual Function? Function { get; set; }
        public virtual Language? Language { get; set; }
    }
}
