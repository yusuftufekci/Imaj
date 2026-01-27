namespace Imaj.Web.Models.Components
{
    /// <summary>
    /// Form panel component için model.
    /// Mavi arka planlı filtre/form grupları için kullanılır.
    /// </summary>
    public class FormPanelModel
    {
        /// <summary>
        /// Panel başlığı
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Panel içeriği (HTML olarak render edilir)
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }
}
