namespace Imaj.Core.Entities
{
    public class RoleMeth : BaseEntity
    {
        public decimal RoleID { get; set; }
        // BaseMeth entity
        public decimal BaseMethID { get; set; }
        public bool Executable { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual Role? Role { get; set; }
        public virtual BaseMeth? BaseMeth { get; set; }
    }
}
