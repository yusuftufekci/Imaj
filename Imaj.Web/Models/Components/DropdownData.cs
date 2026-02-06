using Imaj.Service.DTOs;

namespace Imaj.Web.Models.Components
{
    /// <summary>
    /// Dropdown verileri için ortak ViewModel.
    /// ViewBag yerine typed ViewModel kullanarak type safety sağlar.
    /// </summary>
    public class DropdownData
    {
        /// <summary>
        /// İş durumları listesi (Job kategorisi)
        /// </summary>
        public List<StateDto> States { get; set; } = new();

        /// <summary>
        /// Fonksiyon listesi
        /// </summary>
        public List<FunctionDto> Functions { get; set; } = new();

        /// <summary>
        /// Görev tipi listesi
        /// </summary>
        public List<WorkTypeDto> WorkTypes { get; set; } = new();

        /// <summary>
        /// Mesai tipi listesi
        /// </summary>
        public List<TimeTypeDto> TimeTypes { get; set; } = new();

        /// <summary>
        /// Ürün kategorileri listesi
        /// </summary>
        public List<ProductCategoryDto> ProductCategories { get; set; } = new();
    }
}
