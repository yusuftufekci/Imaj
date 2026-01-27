namespace Imaj.Core.Entities
{
    public class Trans : BaseEntity
    {
        public int Reference { get; set; }
        public decimal TransCatID { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal TransTypeID { get; set; }
        public string Size { get; set; } = string.Empty;
        public string Keyword { get; set; } = string.Empty;
        public byte ParamCount { get; set; }
        public string ParamHelp { get; set; } = string.Empty; // ntext
        public bool Invisible { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
        
        // TransCat ve TransType entity'leri henüz yok. İleride oluşturulacak ama şimdilik comment out yapmayacağım, hemen oluşturacağım.
        // Hata almamak için TransCat ve TransType'ı da bu batch'e dahil etmeliyim.
        public virtual TransCat? TransCat { get; set; }
        public virtual TransType? TransType { get; set; }
    }
}
