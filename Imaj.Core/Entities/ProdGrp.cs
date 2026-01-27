namespace Imaj.Core.Entities
{
    public class ProdGrp : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public bool Invisible { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual Company? Company { get; set; }
    }
}
