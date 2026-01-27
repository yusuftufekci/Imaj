namespace Imaj.Core.Entities
{
    public class JobProd : BaseEntity
    {
        public decimal JobID { get; set; }
        public decimal ProductID { get; set; }
        public short Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal NetAmount { get; set; }
        public string Notes { get; set; } = string.Empty; // ntext
        public decimal Deleted { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
        
        public virtual Job? Job { get; set; }
        public virtual Product? Product { get; set; }
    }
}
