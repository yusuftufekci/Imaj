namespace Imaj.Core.Entities
{
    public class Role : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty; // ntext
        public bool AllPropRead { get; set; }
        public bool AllPropWrite { get; set; }
        public bool AllMethRead { get; set; }
        public bool AllMethWrite { get; set; }
        public bool AllMenu { get; set; }
        public bool Invisible { get; set; }
        public bool SelectFlag { get; set; }
        public bool Global { get; set; }
        public short Stamp { get; set; }
    }
}
