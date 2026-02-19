namespace Imaj.Service.Options
{
    /// <summary>
    /// Authentication modülü için yapılandırma ayarları.
    /// appsettings.json dosyasından okunur.
    /// </summary>
    public class AuthSettings
    {
        /// <summary>
        /// appsettings.json'daki section adı
        /// </summary>
        public const string SectionName = "AuthSettings";

        /// <summary>
        /// Oturum zaman aşımı süresi (dakika)
        /// </summary>
        public int SessionTimeoutMinutes { get; set; } = 45;
    }
}
