namespace Imaj.Core.Entities
{
    public class Reason : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public decimal ReasonCatID { get; set; }
        public string Code { get; set; } = string.Empty;
        public bool Invisible { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual Company? Company { get; set; }
        public virtual ReasonCat? ReasonCat { get; set; }
    }
}
