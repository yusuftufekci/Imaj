namespace Imaj.Core.Entities
{
    public class RoleProp : BaseEntity
    {
        public decimal BasePropID { get; set; }
        public decimal RoleContID { get; set; }
        public bool Read { get; set; }
        public bool Write { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual RoleCont? RoleCont { get; set; }
        public virtual BaseProp? BaseProp { get; set; }
    }
}
