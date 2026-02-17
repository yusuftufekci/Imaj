namespace Imaj.Core.Entities
{
    public class Dtproperties : BaseEntity
    {
        public int id { get; set; }
        public int? objectid { get; set; }
        public string property { get; set; } = string.Empty;
        public string? value { get; set; }
        public string? uvalue { get; set; }
        public byte[]? lvalue { get; set; }
        public int version { get; set; }
    }
}
