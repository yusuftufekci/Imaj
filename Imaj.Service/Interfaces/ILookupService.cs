using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    /// <summary>
    /// Ortak dropdown ve lookup verileri için service interface.
    /// State, Function, WorkType, TimeType gibi referans tablolarını tek noktadan yönetir.
    /// </summary>
    public interface ILookupService
    {
        /// <summary>
        /// Belirtilen kategoriye göre State listesini getirir.
        /// </summary>
        /// <param name="category">State kategorisi (örn: "Job", "Invoice")</param>
        Task<ServiceResult<List<StateDto>>> GetStatesAsync(string category);

        /// <summary>
        /// Fonksiyon listesini getirir.
        /// </summary>
        Task<ServiceResult<List<FunctionDto>>> GetFunctionsAsync();

        /// <summary>
        /// Görev Tipi (WorkType) listesini getirir.
        /// </summary>
        Task<ServiceResult<List<WorkTypeDto>>> GetWorkTypesAsync();

        /// <summary>
        /// Mesai Tipi (TimeType) listesini getirir.
        /// </summary>
        Task<ServiceResult<List<TimeTypeDto>>> GetTimeTypesAsync();

        /// <summary>
        /// Ürün Kategorileri listesini getirir.
        /// </summary>
        Task<ServiceResult<List<ProductCategoryDto>>> GetProductCategoriesAsync();
    }
}
