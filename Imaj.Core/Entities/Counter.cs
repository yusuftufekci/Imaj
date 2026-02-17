namespace Imaj.Core.Entities
{
    public class Counter : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public decimal CounterValue { get; set; }
    }
}
