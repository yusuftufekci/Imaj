namespace Imaj.Core.Entities
{
    public class XState : BaseEntity
    {
        public decimal StateID { get; set; }
        public decimal LanguageID { get; set; }
        public string Name { get; set; } = string.Empty;
        public short Stamp { get; set; }

        public virtual State? State { get; set; }
        public virtual Language? Language { get; set; }
    }
}
