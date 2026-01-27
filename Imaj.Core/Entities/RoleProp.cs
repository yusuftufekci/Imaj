namespace Imaj.Core.Entities
{
    public class RoleProp : BaseEntity
    {
        public decimal RoleID { get; set; }
        // BaseProp entity
        public decimal BasePropID { get; set; }
        public bool Readable { get; set; }
        public bool Writable { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual Role? Role { get; set; }
        public virtual BaseProp? BaseProp { get; set; }
    }
}
