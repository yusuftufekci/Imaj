namespace Imaj.Core.Entities
{
    public class RoleMeth : BaseEntity
    {
        public decimal BaseMethID { get; set; }
        public decimal RoleContID { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual RoleCont? RoleCont { get; set; }
        public virtual BaseMeth? BaseMeth { get; set; }
    }
}
