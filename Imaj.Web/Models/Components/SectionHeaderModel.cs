namespace Imaj.Web.Models.Components
{
    /// <summary>
    /// Section header component için model.
    /// _SectionHeader.cshtml partial view'ı ile kullanılır.
    /// </summary>
    public class SectionHeaderModel
    {
        /// <summary>
        /// Başlık metni
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Alt başlık metni (opsiyonel)
        /// </summary>
        public string? Subtitle { get; set; }
        
        /// <summary>
        /// Alt border gösterilsin mi
        /// </summary>
        public bool HasBorder { get; set; } = true;
        
        /// <summary>
        /// Ortaya hizalansın mı
        /// </summary>
        public bool IsCentered { get; set; } = true;
    }
}
