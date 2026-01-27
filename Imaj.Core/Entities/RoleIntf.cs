namespace Imaj.Core.Entities
{
    public class RoleIntf : BaseEntity
    {
        public decimal RoleID { get; set; }
        // BaseIntf entity yok ama scriptte referans var. Basit bir BaseIntf entity oluşturmak gerekebilir.
        // Hata vermemesi için BaseIntf entity'sini de bu batch'e ekleyeceğim.
        public decimal BaseIntfID { get; set; }
        public bool Visible { get; set; }
        public bool Enabled { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual Role? Role { get; set; }
        public virtual BaseIntf? BaseIntf { get; set; }
    }
}
