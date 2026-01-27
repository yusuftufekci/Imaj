namespace Imaj.Core.Entities
{
    public class FuncRule : BaseEntity
    {
        public decimal FunctionID { get; set; }
        public string Name { get; set; } = string.Empty;
        public short MinValue { get; set; }
        public short MaxValue { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual Function? Function { get; set; }
    }
}
