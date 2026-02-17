namespace Imaj.Core.Entities
{
    public class RoleIntf : BaseEntity
    {
        public decimal RoleContID { get; set; }
        public decimal BaseIntfID { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual RoleCont? RoleCont { get; set; }
        public virtual BaseIntf? BaseIntf { get; set; }
    }
}
