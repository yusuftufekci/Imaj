using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    /// <summary>
    /// İş (Job) işlemleri için service interface.
    /// Dropdown verilerini sağlar.
    /// </summary>
    public interface IJobService
    {
        /// <summary>
        /// State (Durum) listesini veritabanından getirir.
        /// State tablosunda Category='Job' olan kayıtların XState'teki LanguageID=1 isimlerini çeker.
        /// </summary>
        Task<ServiceResult<List<StateDto>>> GetStatesAsync();

        /// <summary>
        /// Fonksiyon listesini veritabanından getirir.
        /// Function tablosunu XFunction ile join edip LanguageID=1 olanları çeker.
        /// </summary>
        Task<ServiceResult<List<FunctionDto>>> GetFunctionsAsync();

        /// <summary>
        /// Görev Tipi (WorkType) listesini veritabanından getirir.
        /// XWorkType tablosundan LanguageID=1 olanları çeker.
        /// </summary>
        Task<ServiceResult<List<WorkTypeDto>>> GetWorkTypesAsync();

        /// <summary>
        /// Mesai Tipi (TimeType) listesini veritabanından getirir.
        /// XTimeType tablosundan LanguageID=1 olanları çeker.
        /// </summary>
        Task<ServiceResult<List<TimeTypeDto>>> GetTimeTypesAsync();

        /// <summary>
        /// İş kayıtlarını filtreleyerek getirir.
        /// Sayfalama destekli.
        /// </summary>
        Task<ServiceResult<PagedResult<JobDto>>> GetByFilterAsync(JobFilterDto filter);

        /// <summary>
        /// Referans numarasına göre tekil iş detayını getirir.
        /// JobWorks (Mesai) ve diğer detayları içerir.
        /// </summary>
        Task<ServiceResult<JobDto>> GetByReferenceAsync(int reference);

        /// <summary>
        /// Yeni iş kaydı oluşturur.
        /// Mesai ve Ürün detayları ile birlikte.
        /// </summary>
        Task<ServiceResult<JobDto>> AddAsync(JobDto jobDto);

        /// <summary>
        /// İş geçmişini (loglarını) getirir.
        /// </summary>
        Task<ServiceResult<List<JobLogDto>>> GetJobHistoryAsync(decimal jobId);
        Task<List<string>> GetTableColumnsAsync(string tableName);
    }
}
