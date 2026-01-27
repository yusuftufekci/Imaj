namespace Imaj.Core.Entities
{
    public class Resource : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public decimal FunctionID { get; set; }
        public decimal ResoCatID { get; set; }
        public int Sequence { get; set; }
        public string Code { get; set; } = string.Empty;
        public bool Invisible { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
        
        public virtual Company? Company { get; set; }
        public virtual Function? Function { get; set; }
        public virtual ResoCat? ResoCat { get; set; }
    }
}
