namespace Imaj.Core.Entities
{
    public class UserRole : BaseEntity
    {
        public decimal UserID { get; set; }
        public decimal RoleID { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual User? User { get; set; }
        public virtual Role? Role { get; set; }
    }
}
