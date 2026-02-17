namespace Imaj.Core.Entities
{
    public class RoleMenu : BaseEntity
    {
        public decimal RoleID { get; set; }
        public decimal BaseIntfID { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual Role? Role { get; set; }
        public virtual BaseIntf? BaseIntf { get; set; }
    }
}
