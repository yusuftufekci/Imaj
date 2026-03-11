namespace Imaj.Service.Options
{
    public class DeploymentSettings
    {
        public const string SectionName = "Deployment";

        public bool RequireHttps { get; set; } = true;

        public bool UseHsts { get; set; } = true;

        public bool SecureCookies { get; set; } = true;
    }
}
