namespace Imaj.Core.Entities
{
    public class XTriplet : BaseEntity
    {
        public decimal TripletID { get; set; }
        public decimal LanguageID { get; set; }
        public string Name { get; set; } = string.Empty;
        public short Stamp { get; set; }

        public virtual Triplet? Triplet { get; set; }
        public virtual Language? Language { get; set; }
    }
}
