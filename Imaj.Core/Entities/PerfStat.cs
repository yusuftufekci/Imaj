namespace Imaj.Core.Entities
{
    public class PerfStat : BaseEntity
    {
        public DateTime StartDT { get; set; }
        public string Controller { get; set; } = string.Empty;
        public string Interface { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int FullDuration { get; set; }
        public int MethodDuration { get; set; }
        public decimal? UserID { get; set; }
    }
}
