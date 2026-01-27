namespace Imaj.Core.Entities
{
    public class XTrans : BaseEntity
    {
        public decimal TransID { get; set; }
        public decimal LanguageID { get; set; }
        public string Descr { get; set; } = string.Empty; // ntext
        public string Help { get; set; } = string.Empty; // ntext
        public string Remark { get; set; } = string.Empty; // ntext
        public short Stamp { get; set; }

        // Trans entity henüz yok. Hemen oluşturacağım.
        public virtual Trans? Trans { get; set; }
        public virtual Language? Language { get; set; }
    }
}
