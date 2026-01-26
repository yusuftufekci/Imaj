namespace Imaj.Service.Options
{
    /// <summary>
    /// Müşteri modülü için yapılandırma ayarları.
    /// appsettings.json dosyasından okunur.
    /// </summary>
    public class CustomerSettings
    {
        /// <summary>
        /// appsettings.json'daki section adı
        /// </summary>
        public const string SectionName = "CustomerSettings";

        /// <summary>
        /// Varsayılan şirket ID'si (FK constraint için kullanılır)
        /// </summary>
        public decimal DefaultCompanyId { get; set; } = 7;

        /// <summary>
        /// Varsayılan sayfa boyutu (pagination için)
        /// </summary>
        public int DefaultPageSize { get; set; } = 20;
    }
}
