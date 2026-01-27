namespace Imaj.Core.Entities
{
    public class RoleCont : BaseEntity
    {
        public decimal RoleID { get; set; }
        public decimal BaseContID { get; set; }
        public bool AllPropRead { get; set; }
        public bool AllPropWrite { get; set; }
        public bool AllMethRead { get; set; }
        public bool AllMethWrite { get; set; }
        public bool AllIntf { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
        
        public virtual Role? Role { get; set; }
        // BaseCont entity henüz yok, oluşturmalıyım
        public virtual BaseCont? BaseCont { get; set; }
    }
}
