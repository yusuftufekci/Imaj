namespace Imaj.Service.Options
{
    public class SecurityHeadersSettings
    {
        public const string SectionName = "SecurityHeaders";

        public bool CspReportOnly { get; set; } = true;

        public string CspValue { get; set; } =
            "default-src 'self'; " +
            "script-src 'self' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com; " +
            "img-src 'self' data:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self';";
    }
}
