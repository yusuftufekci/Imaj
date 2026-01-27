namespace Imaj.Core.Entities
{
    public class FuncProd : BaseEntity
    {
        public decimal FunctionID { get; set; }
        public decimal ProductID { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual Function? Function { get; set; }
        public virtual Product? Product { get; set; }
    }
}
