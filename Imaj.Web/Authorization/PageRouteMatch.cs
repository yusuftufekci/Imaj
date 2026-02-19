namespace Imaj.Web.Authorization
{
    public class PageRouteMatch
    {
        public bool IsMapped { get; set; }
        public string MatchStatus { get; set; } = string.Empty;
        public string? AspPage { get; set; }
        public decimal? BaseIntfId { get; set; }
        public string? Reason { get; set; }
    }
}
