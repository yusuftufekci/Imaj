using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    /// <summary>
    /// İş (Job) işlemleri için service interface.
    /// CRUD ve iş-spesifik operasyonları sağlar.
    /// NOTE: Dropdown verileri (States, Functions vb.) artık ILookupService'den alınıyor.
    /// </summary>
    public interface IJobService
    {
        /// <summary>
        /// İş kayıtlarını filtreleyerek getirir.
        /// Sayfalama destekli.
        /// </summary>
        Task<ServiceResult<PagedResult<JobDto>>> GetByFilterAsync(JobFilterDto filter, CancellationToken cancellationToken = default);

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
        Task<ServiceResult> ExecuteWorkflowActionAsync(int reference, JobWorkflowAction action);
        Task<ServiceResult<JobEmailDraftDto>> PrepareEmailDraftAsync(IReadOnlyCollection<int> references, CancellationToken cancellationToken = default);
        Task<ServiceResult> MarkEmailSentAsync(IReadOnlyCollection<int> references, CancellationToken cancellationToken = default);

        /// <summary>
        /// Detaylı mesai raporu için filtrelenmiş satırları getirir.
        /// </summary>
        Task<ServiceResult<List<OvertimeReportRowDto>>> GetDetailedOvertimeReportAsync(OvertimeReportFilterDto filter);

        /// <summary>
        /// Özet mesai raporu için filtrelenmiş ve gruplanmış satırları getirir.
        /// </summary>
        Task<ServiceResult<List<OvertimeSummaryReportRowDto>>> GetSummaryOvertimeReportAsync(OvertimeReportFilterDto filter);

        /// <summary>
        /// İdari özet mesai raporu için çalışan bazlı gruplanmış satırları getirir.
        /// </summary>
        Task<ServiceResult<List<OvertimeAdministrativeSummaryReportRowDto>>> GetAdministrativeSummaryOvertimeReportAsync(OvertimeReportFilterDto filter);

        /// <summary>
        /// Detaylı fatura bekleyen işler raporu için satırları getirir.
        /// </summary>
        Task<ServiceResult<List<PendingInvoiceJobsDetailedReportRowDto>>> GetDetailedPendingInvoiceJobsReportAsync(PendingInvoiceJobsReportFilterDto filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Özet fatura bekleyen işler raporu için müşteri bazlı satırları getirir.
        /// </summary>
        Task<ServiceResult<List<PendingInvoiceJobsSummaryReportRowDto>>> GetSummaryPendingInvoiceJobsReportAsync(PendingInvoiceJobsReportFilterDto filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Detaylı iş raporu için filtrelenmiş satırları getirir.
        /// </summary>
        Task<ServiceResult<List<JobDetailedReportRowDto>>> GetDetailedJobReportAsync(JobFilterDto filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Özet iş raporu için müşteri bazlı satırları getirir.
        /// </summary>
        Task<ServiceResult<List<JobSummaryReportRowDto>>> GetSummaryJobReportAsync(JobFilterDto filter, CancellationToken cancellationToken = default);

        Task<List<string>> GetTableColumnsAsync(string tableName);
    }
}
