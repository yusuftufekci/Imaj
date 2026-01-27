namespace Imaj.Core.Entities
{
    public class FuncReso : BaseEntity
    {
        public decimal FuncRuleID { get; set; }
        public decimal ResoCatID { get; set; }
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }

        public virtual FuncRule? FuncRule { get; set; }
        public virtual ResoCat? ResoCat { get; set; }
    }
}
