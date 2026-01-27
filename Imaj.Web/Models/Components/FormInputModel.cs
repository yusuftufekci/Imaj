using Microsoft.AspNetCore.Mvc.Rendering;

namespace Imaj.Web.Models.Components
{
    /// <summary>
    /// Form input component için model.
    /// _FormInput.cshtml partial view'ı ile kullanılır.
    /// </summary>
    public class FormInputModel
    {
        /// <summary>
        /// Input label metni
        /// </summary>
        public string Label { get; set; } = string.Empty;
        
        /// <summary>
        /// Input name attribute'u
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Input tipi (text, email, password, date, select, textarea)
        /// </summary>
        public string Type { get; set; } = "text";
        
        /// <summary>
        /// Mevcut değer
        /// </summary>
        public string? Value { get; set; }
        
        /// <summary>
        /// Placeholder metni
        /// </summary>
        public string? Placeholder { get; set; }
        
        /// <summary>
        /// Zorunlu alan mı
        /// </summary>
        public bool IsRequired { get; set; }
        
        /// <summary>
        /// Salt okunur mu
        /// </summary>
        public bool IsReadOnly { get; set; }
        
        /// <summary>
        /// Select tipi için seçenekler
        /// </summary>
        public IEnumerable<SelectListItem>? Options { get; set; }
        
        /// <summary>
        /// Textarea için satır sayısı
        /// </summary>
        public int Rows { get; set; }
    }
}
