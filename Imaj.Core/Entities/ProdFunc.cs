namespace Imaj.Core.Entities
{
    public class ProdFunc : BaseEntity
    {
        public decimal ProductID { get; set; }
        public decimal FunctionID { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
    }
}
