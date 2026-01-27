namespace Imaj.Web.Models.Components
{
    /// <summary>
    /// Pagination component için model.
    /// _Pagination.cshtml partial view'ı ile kullanılır.
    /// </summary>
    public class PaginationModel
    {
        /// <summary>
        /// Mevcut sayfa numarası
        /// </summary>
        public int CurrentPage { get; set; } = 1;
        
        /// <summary>
        /// Toplam kayıt sayısı
        /// </summary>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// Sayfa başına kayıt sayısı
        /// </summary>
        public int PageSize { get; set; } = 20;
        
        /// <summary>
        /// Sayfa linklerinin base URL'i (örn: /Customer/List)
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Toplam sayfa sayısını hesaplar
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
