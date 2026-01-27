namespace Imaj.Core.Entities
{
    public class RoleMenu : BaseEntity
    {
        public decimal RoleID { get; set; }
        // BaseMenu entity
        public decimal BaseMenuID { get; set; }
        public bool Visible { get; set; }
        public bool Enabled { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual Role? Role { get; set; }
        public virtual BaseMenu? BaseMenu { get; set; }
    }
}
