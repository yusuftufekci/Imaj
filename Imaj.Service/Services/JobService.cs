using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Service.DTOs;
using Imaj.Service.DTOs.Security;
using Imaj.Service.Interfaces;
using Imaj.Service.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // Added for GetConnectionString
using Microsoft.Extensions.Logging;

namespace Imaj.Service.Services
{
    /// <summary>
    /// İş (Job) işlemleri için business service.
    /// CRUD ve iş-spesifik operasyonları sağlar.
    /// NOTE: Dropdown verileri (States, Functions vb.) artık LookupService'den alınıyor.
    /// </summary>
    public class JobService : BaseService, IJobService
    {
        private readonly ICurrentPermissionContext _currentPermissionContext;

        public JobService(
            IUnitOfWork unitOfWork,
            ILogger<JobService> logger,
            IConfiguration configuration,
            ICurrentPermissionContext currentPermissionContext)
            : base(unitOfWork, logger, configuration)
        {
            _currentPermissionContext = currentPermissionContext;
        }

        /// <summary>
        /// İş kayıtlarını filtreleyerek getirir.
        /// Customer, Function, State tabloları ile join yaparak isim bilgilerini alır.
        /// </summary>
        public async Task<ServiceResult<PagedResult<JobDto>>> GetByFilterAsync(JobFilterDto filter, CancellationToken cancellationToken = default)
        {
            // ... (existing code) ...
            try
            {
                var languageId = CurrentLanguageId;
                var snapshot = await _currentPermissionContext.GetSnapshotAsync();
                if (IsDataScopeDenied(snapshot))
                {
                    return ServiceResult<PagedResult<JobDto>>.Success(new PagedResult<JobDto>
                    {
                        Items = new List<JobDto>(),
                        TotalCount = 0,
                        PageNumber = filter.Page > 0 ? filter.Page : 1,
                        PageSize = filter.PageSize > 0 ? filter.PageSize : 20
                    });
                }

                var activeSnapshot = snapshot!;

                // ... (implementation of GetByFilterAsync) ...
                // Ana sorgu - Job'lardan başla
                var scopedJobs = ApplyJobDataScope(_unitOfWork.Repository<Job>().Query(), activeSnapshot);
                var query = from j in scopedJobs
                            join c in _unitOfWork.Repository<Customer>().Query() on j.CustomerID equals c.Id into cGroup
                            from customer in cGroup.DefaultIfEmpty()
                            join xf in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == languageId)
                                on j.FunctionID equals xf.FunctionID into fGroup
                            from xFunc in fGroup.DefaultIfEmpty()
                            join xs in _unitOfWork.Repository<XState>().Query().Where(x => x.LanguageID == languageId)
                                on j.StateID equals xs.StateID into sGroup
                            from xState in sGroup.DefaultIfEmpty()
                            select new { Job = j, CustomerCode = customer != null ? customer.Code : null, CustomerName = customer != null ? customer.Name : null, FunctionName = xFunc != null ? xFunc.Name : null, StateName = xState != null ? xState.Name : null };

                // FİLTRELERİ AKTİF EDİYORUZ

                // Fonksiyon filtresi
                if (filter.FunctionId.HasValue)
                {
                    query = query.Where(x => x.Job.FunctionID == filter.FunctionId.Value);
                }

                // Müşteri ID filtresi
                if (filter.CustomerId.HasValue)
                {
                    query = query.Where(x => x.Job.CustomerID == filter.CustomerId.Value);
                }

                // Referans aralığı filtresi
                if (filter.ReferenceStart.HasValue)
                {
                    query = query.Where(x => x.Job.Reference >= filter.ReferenceStart.Value);
                }
                if (filter.ReferenceEnd.HasValue)
                {
                    query = query.Where(x => x.Job.Reference <= filter.ReferenceEnd.Value);
                }

                // Referans listesi filtresi (virgülle ayrılmış)
                if (!string.IsNullOrWhiteSpace(filter.ReferenceList))
                {
                    var refList = filter.ReferenceList.Split(',')
                        .Select(r => int.TryParse(r.Trim(), out var val) ? val : (int?)null)
                        .Where(r => r.HasValue)
                        .Select(r => r!.Value)
                        .ToList();
                    
                    if (refList.Any())
                    {
                        query = query.Where(x => refList.Contains(x.Job.Reference));
                    }
                }

                // İş adı filtresi
                if (!string.IsNullOrWhiteSpace(filter.JobName))
                {
                    query = query.Where(x => x.Job.Name.Contains(filter.JobName));
                }

                // İlgili kişi (Contact) filtresi
                if (!string.IsNullOrWhiteSpace(filter.RelatedPerson))
                {
                    query = query.Where(x => x.Job.Contact.Contains(filter.RelatedPerson));
                }

                // Başlangıç tarihi aralığı
                if (filter.StartDateStart.HasValue)
                {
                    query = query.Where(x => x.Job.StartDT >= filter.StartDateStart.Value);
                }
                if (filter.StartDateEnd.HasValue)
                {
                    query = query.Where(x => x.Job.StartDT <= filter.StartDateEnd.Value);
                }

                // Bitiş tarihi aralığı
                if (filter.EndDateStart.HasValue)
                {
                    query = query.Where(x => x.Job.EndDT >= filter.EndDateStart.Value);
                }
                if (filter.EndDateEnd.HasValue)
                {
                    query = query.Where(x => x.Job.EndDT <= filter.EndDateEnd.Value);
                }

                // Durum filtresi
                if (filter.StateId.HasValue)
                {
                    query = query.Where(x => x.Job.StateID == filter.StateId.Value);
                }

                // E-Posta gönderildi filtresi
                if (filter.IsEmailSent.HasValue)
                {
                    query = query.Where(x => x.Job.Mailed == filter.IsEmailSent.Value);
                }

                // Değerlendirildi filtresi
                if (filter.IsEvaluated.HasValue)
                {
                    query = query.Where(x => x.Job.Evaluated == filter.IsEvaluated.Value);
                }

                // Fatura durumu filtresi
                if (filter.HasInvoice.HasValue)
                {
                    if (filter.HasInvoice.Value)
                    {
                        query = query.Where(x => x.Job.InvoLineID != null);
                    }
                    else
                    {
                        query = query.Where(x => x.Job.InvoLineID == null);
                    }
                }

                // Ürün filtresi - belirli bir ürünü içeren Job'ları getir
                if (filter.ProductId.HasValue)
                {
                    _logger.LogInformation("Ürün filtresi uygulanıyor. ProductId: {ProductId}", filter.ProductId.Value);
                    
                    var jobProdRepo = _unitOfWork.Repository<JobProd>();
                    var jobIdsWithProduct = jobProdRepo.Query()
                        .Where(jp => jp.ProductID == filter.ProductId.Value && jp.Deleted == 0)
                        .Select(jp => jp.JobID);
                    
                    var matchingJobIds = await jobIdsWithProduct.ToListAsync(cancellationToken);
                    _logger.LogInformation("Ürün filtresi: {Count} iş bulundu. JobID'ler: {JobIds}", matchingJobIds.Count, string.Join(", ", matchingJobIds.Take(10)));
                    
                    query = query.Where(x => matchingJobIds.Contains(x.Job.Id));
                }

                // ============ MESAİ KRİTERİ FİLTRELERİ ============
                // Bu filtreler JobWork tablosuyla ilişkili Job'ları getirir
                // NOT: Memory'e çekmeden doğrudan SQL subquery kullanıyoruz (OPENJSON sorunu için)

                // Çalışan filtresi - EmployeeCode'a göre JobWork tablosundan ilgili Job'ları getir
                if (!string.IsNullOrWhiteSpace(filter.EmployeeCode))
                {
                    _logger.LogInformation("Çalışan filtresi uygulanıyor. EmployeeCode: {EmployeeCode}", filter.EmployeeCode);

                    // Subquery olarak Employee ve JobWork tablolarını birleştir
                    var jobWorkRepo = _unitOfWork.Repository<JobWork>();
                    var employeeRepo = _unitOfWork.Repository<Employee>();

                    if (!activeSnapshot.EmployeeScopeBypass)
                    {
                        var requestedEmployeeAllowed = await employeeRepo.Query()
                            .AnyAsync(e =>
                                e.Code == filter.EmployeeCode
                                && activeSnapshot.AllowedEmployeeIds.Contains(e.Id), cancellationToken);

                        if (!requestedEmployeeAllowed)
                        {
                            return ServiceResult<PagedResult<JobDto>>.Success(new PagedResult<JobDto>
                            {
                                Items = new List<JobDto>(),
                                TotalCount = 0,
                                PageNumber = filter.Page > 0 ? filter.Page : 1,
                                PageSize = filter.PageSize > 0 ? filter.PageSize : 20
                            });
                        }
                    }

                    // Subquery: Bu çalışanın mesai kaydı olan Job ID'leri
                    var jobIdsWithEmployee = from jw in jobWorkRepo.Query()
                                              join e in employeeRepo.Query() on jw.EmployeeID equals e.Id
                                              where e.Code == filter.EmployeeCode
                                                    && jw.Deleted == 0
                                                    && (activeSnapshot.EmployeeScopeBypass || activeSnapshot.AllowedEmployeeIds.Contains(jw.EmployeeID))
                                              select jw.JobID;

                    // Ana sorguya subquery ile filtre uygula
                    query = query.Where(x => jobIdsWithEmployee.Contains(x.Job.Id));
                }

                // Görev Tipi (WorkType) filtresi
                if (filter.WorkTypeId.HasValue)
                {
                    _logger.LogInformation("Görev Tipi filtresi uygulanıyor. WorkTypeId: {WorkTypeId}", filter.WorkTypeId.Value);
                    
                    // Subquery: Bu görev tipiyle mesai kaydı olan Job ID'leri
                    var jobIdsWithWorkType = _unitOfWork.Repository<JobWork>().Query()
                        .Where(jw => jw.WorkTypeID == filter.WorkTypeId.Value && jw.Deleted == 0)
                        .Select(jw => jw.JobID);
                    
                    // Ana sorguya subquery ile filtre uygula
                    query = query.Where(x => jobIdsWithWorkType.Contains(x.Job.Id));
                }

                // Mesai Tipi (TimeType) filtresi
                if (filter.TimeTypeId.HasValue)
                {
                    _logger.LogInformation("Mesai Tipi filtresi uygulanıyor. TimeTypeId: {TimeTypeId}", filter.TimeTypeId.Value);
                    
                    // Subquery: Bu mesai tipiyle kayıt olan Job ID'leri
                    var jobIdsWithTimeType = _unitOfWork.Repository<JobWork>().Query()
                        .Where(jw => jw.TimeTypeID == filter.TimeTypeId.Value && jw.Deleted == 0)
                        .Select(jw => jw.JobID);
                    
                    // Ana sorguya subquery ile filtre uygula
                    query = query.Where(x => jobIdsWithTimeType.Contains(x.Job.Id));
                }

                // Toplam kayıt sayısı
                var page = filter.Page > 0 ? filter.Page : 1;
                var pageSize = filter.PageSize > 0 ? filter.PageSize : 20;
                var first = filter.First.HasValue && filter.First.Value > 0 ? filter.First.Value : (int?)null;

                var orderedQuery = query
                    .OrderByDescending(x => x.Job.StartDT);

                var scopedQuery = first.HasValue
                    ? orderedQuery.Take(first.Value)
                    : orderedQuery;

                var totalCount = await scopedQuery.CountAsync(cancellationToken);
                var skip = (page - 1) * pageSize;

                var items = await scopedQuery
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(x => new JobDto
                    {
                        Id = x.Job.Id,
                        Reference = x.Job.Reference,
                        FunctionId = x.Job.FunctionID,
                        FunctionName = x.FunctionName,
                        CustomerId = x.Job.CustomerID,
                        CustomerCode = x.CustomerCode,
                        CustomerName = x.CustomerName,
                        Name = x.Job.Name,
                        Contact = x.Job.Contact,
                        StartDate = x.Job.StartDT,
                        EndDate = x.Job.EndDT,
                        StateId = x.Job.StateID,
                        StatusName = x.StateName,
                        IsEmailSent = x.Job.Mailed,
                        IsEvaluated = x.Job.Evaluated,
                        InvoLineId = x.Job.InvoLineID,
                        WorkAmount = x.Job.WorkSum,
                        ProductAmount = x.Job.ProdSum
                    })
                    .ToListAsync(cancellationToken);

                var result = new PagedResult<JobDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                };

                return ServiceResult<PagedResult<JobDto>>.Success(result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş listesi alınırken hata oluştu");
                return ServiceResult<PagedResult<JobDto>>.Fail("İş listesi alınırken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Referans numarasına göre tekil iş detayını getirir.
        /// JobWorks (Mesai) ve diğer detayları içerir.
        /// </summary>
        public async Task<ServiceResult<JobDto>> GetByReferenceAsync(int reference)
        {
            try
            {
                var languageId = CurrentLanguageId;
                var snapshot = await _currentPermissionContext.GetSnapshotAsync();
                if (IsDataScopeDenied(snapshot))
                {
                    return ServiceResult<JobDto>.Fail("İş bulunamadı.");
                }

                var activeSnapshot = snapshot!;

                // 1. Ana İş Bilgisi
                var scopedJobs = ApplyJobDataScope(_unitOfWork.Repository<Job>().Query(), activeSnapshot);
                var jobQuery = from j in scopedJobs
                            where j.Reference == reference // Referansa göre filtrele
                            join c in _unitOfWork.Repository<Customer>().Query() on j.CustomerID equals c.Id into cGroup
                            from customer in cGroup.DefaultIfEmpty()
                            join xf in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == languageId)
                                on j.FunctionID equals xf.FunctionID into fGroup
                            from xFunc in fGroup.DefaultIfEmpty()
                            join xs in _unitOfWork.Repository<XState>().Query().Where(x => x.LanguageID == languageId)
                                on j.StateID equals xs.StateID into sGroup
                            from xState in sGroup.DefaultIfEmpty()
                            select new JobDto
                            {
                                Id = j.Id,
                                Reference = j.Reference,
                                FunctionId = j.FunctionID,
                                FunctionName = xFunc != null ? xFunc.Name : null,
                                CustomerId = j.CustomerID,
                                CustomerName = customer != null ? customer.Name : null,
                                Name = j.Name,
                                Contact = j.Contact,
                                StartDate = j.StartDT,
                                EndDate = j.EndDT,
                                StateId = j.StateID,
                                StatusName = xState != null ? xState.Name : null,
                                IsEmailSent = j.Mailed,
                                IsEvaluated = j.Evaluated,
                                InvoLineId = j.InvoLineID,
                                IntNotes = j.IntNotes, // Detaylar da gelsin
                                ExtNotes = j.ExtNotes
                            };

                var jobDto = await jobQuery.FirstOrDefaultAsync();

                if (jobDto == null)
                {
                    return ServiceResult<JobDto>.Fail("İş bulunamadı.");
                }

                // 2. Mesai (JobWork) Bilgileri
                var workQuery = from jw in _unitOfWork.Repository<JobWork>().Query()
                                where jw.JobID == jobDto.Id
                                      && jw.Deleted == 0
                                      && (activeSnapshot.EmployeeScopeBypass || activeSnapshot.AllowedEmployeeIds.Contains(jw.EmployeeID))
                                join e in _unitOfWork.Repository<Employee>().Query() on jw.EmployeeID equals e.Id
                                join xw in _unitOfWork.Repository<XWorkType>().Query().Where(x => x.LanguageID == languageId)
                                    on jw.WorkTypeID equals xw.WorkTypeID into wGroup
                                from xWork in wGroup.DefaultIfEmpty()
                                join xt in _unitOfWork.Repository<XTimeType>().Query().Where(x => x.LanguageID == languageId)
                                    on jw.TimeTypeID equals xt.TimeTypeID into tGroup
                                from xTime in tGroup.DefaultIfEmpty()
                                select new JobWorkDto
                                {
                                    Id = jw.Id,
                                    EmployeeId = jw.EmployeeID,
                                    EmployeeCode = e.Code,
                                    EmployeeName = e.Name, // Ad Soyad birleştirme kaldırıldı (Sadece Name var)
                                    WorkTypeName = xWork != null ? xWork.Name : null,
                                    TimeTypeName = xTime != null ? xTime.Name : null,
                                    Quantity = jw.Quantity,
                                    Amount = jw.Amount,
                                    Notes = jw.Notes,
                                    SelectFlag = jw.SelectFlag
                                };
                
                jobDto.JobWorks = await workQuery.ToListAsync();

                // 3. Ürün (JobProd) Bilgileri
                var prodQuery = from jp in _unitOfWork.Repository<JobProd>().Query()
                                where jp.JobID == jobDto.Id && jp.Deleted == 0
                                join p in _unitOfWork.Repository<Product>().Query() on jp.ProductID equals p.Id
                                join xp in _unitOfWork.Repository<XProduct>().Query().Where(x => x.LanguageID == languageId)
                                    on jp.ProductID equals xp.ProductID into xpGroup
                                from xProduct in xpGroup.DefaultIfEmpty()
                                join xpc in _unitOfWork.Repository<XProdCat>().Query().Where(x => x.LanguageID == languageId)
                                    on p.ProdCatID equals xpc.ProdCatID into xpcGroup
                                from xProdCat in xpcGroup.DefaultIfEmpty()
                                select new JobProdDto
                                {
                                    Id = jp.Id,
                                    ProductId = jp.ProductID,
                                    ProductCode = p.Code,
                                    ProductName = xProduct != null ? xProduct.Name : null,
                                    CategoryId = p.ProdCatID,
                                    CategoryName = xProdCat != null ? xProdCat.Name : "Diğer",
                                    Quantity = jp.Quantity,
                                    Price = jp.Price,
                                    GrossAmount = jp.GrossAmount,
                                    NetAmount = jp.NetAmount,
                                    Notes = jp.Notes,
                                    SelectFlag = jp.SelectFlag
                                };

                jobDto.JobProds = await prodQuery.ToListAsync();

                // 4. Ürün Kategorileri (JobProdCat) Bilgileri
                var prodCatQuery = from jpc in _unitOfWork.Repository<JobProdCat>().Query()
                                   where jpc.JobID == jobDto.Id && jpc.Deleted == 0
                                   join pc in _unitOfWork.Repository<ProdCat>().Query() on jpc.ProdCatID equals pc.Id
                                   join xpc in _unitOfWork.Repository<XProdCat>().Query().Where(x => x.LanguageID == languageId)
                                       on pc.Id equals xpc.ProdCatID into xpcGroup
                                   from xProdCat in xpcGroup.DefaultIfEmpty()
                                   select new JobProdCatDto
                                   {
                                       CategoryId = jpc.ProdCatID,
                                       CategoryName = xProdCat != null ? xProdCat.Name : pc.Id.ToString(),
                                       GrossAmount = jpc.GrossAmount,
                                       DiscPercentage = jpc.DiscPercentage,
                                       DiscAmount = jpc.DiscAmount,
                                       NetAmount = jpc.NetAmount
                                   };

                jobDto.JobProdCats = await prodCatQuery.ToListAsync();

                return ServiceResult<JobDto>.Success(jobDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş detayı getirilirken hata oluştu: {Reference}", reference);
                return ServiceResult<JobDto>.Fail("İş detayı yüklenirken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Detaylı mesai raporu için filtrelenmiş satırları getirir.
        /// </summary>
        public async Task<ServiceResult<List<OvertimeReportRowDto>>> GetDetailedOvertimeReportAsync(OvertimeReportFilterDto filter)
        {
            try
            {
                var snapshot = await _currentPermissionContext.GetSnapshotAsync();
                if (IsDataScopeDenied(snapshot))
                {
                    return ServiceResult<List<OvertimeReportRowDto>>.Success(new List<OvertimeReportRowDto>());
                }

                var activeSnapshot = snapshot!;
                if (IsEmployeeScopeDenied(activeSnapshot))
                {
                    return ServiceResult<List<OvertimeReportRowDto>>.Success(new List<OvertimeReportRowDto>());
                }

                var items = await BuildOvertimeReportBaseQuery(filter, activeSnapshot)
                    .OrderBy(x => x.EmployeeName)
                    .ThenBy(x => x.JobDate)
                    .ThenBy(x => x.Reference)
                    .Select(x => new OvertimeReportRowDto
                    {
                        EmployeeCode = x.EmployeeCode,
                        EmployeeName = x.EmployeeName,
                        TimeTypeName = x.TimeTypeName,
                        WorkTypeName = x.WorkTypeName,
                        Reference = x.Reference,
                        JobDate = x.JobDate,
                        CustomerCode = x.CustomerCode,
                        CustomerName = x.CustomerName,
                        JobName = x.JobName,
                        Notes = x.Notes,
                        Quantity = x.Quantity,
                        Amount = x.Amount
                    })
                    .ToListAsync();

                return ServiceResult<List<OvertimeReportRowDto>>.Success(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detaylı mesai raporu alınırken hata oluştu.");
                return ServiceResult<List<OvertimeReportRowDto>>.Fail("Detaylı mesai raporu alınırken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Özet mesai raporu için filtrelenmiş ve gruplanmış satırları getirir.
        /// </summary>
        public async Task<ServiceResult<List<OvertimeSummaryReportRowDto>>> GetSummaryOvertimeReportAsync(OvertimeReportFilterDto filter)
        {
            try
            {
                var snapshot = await _currentPermissionContext.GetSnapshotAsync();
                if (IsDataScopeDenied(snapshot))
                {
                    return ServiceResult<List<OvertimeSummaryReportRowDto>>.Success(new List<OvertimeSummaryReportRowDto>());
                }

                var activeSnapshot = snapshot!;
                if (IsEmployeeScopeDenied(activeSnapshot))
                {
                    return ServiceResult<List<OvertimeSummaryReportRowDto>>.Success(new List<OvertimeSummaryReportRowDto>());
                }

                var items = await BuildOvertimeReportBaseQuery(filter, activeSnapshot)
                    .GroupBy(x => new { x.EmployeeCode, x.EmployeeName, x.TimeTypeName, x.WorkTypeName })
                    .Select(g => new OvertimeSummaryReportRowDto
                    {
                        EmployeeCode = g.Key.EmployeeCode,
                        EmployeeName = g.Key.EmployeeName,
                        TimeTypeName = g.Key.TimeTypeName,
                        WorkTypeName = g.Key.WorkTypeName,
                        Quantity = g.Sum(x => x.Quantity),
                        Amount = g.Sum(x => x.Amount)
                    })
                    .OrderBy(x => x.EmployeeName)
                    .ThenBy(x => x.TimeTypeName)
                    .ThenBy(x => x.WorkTypeName)
                    .ToListAsync();

                return ServiceResult<List<OvertimeSummaryReportRowDto>>.Success(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Özet mesai raporu alınırken hata oluştu.");
                return ServiceResult<List<OvertimeSummaryReportRowDto>>.Fail("Özet mesai raporu alınırken bir hata oluştu.");
            }
        }

        /// <summary>
        /// İdari özet mesai raporu için çalışan bazlı gruplanmış satırları getirir.
        /// </summary>
        public async Task<ServiceResult<List<OvertimeAdministrativeSummaryReportRowDto>>> GetAdministrativeSummaryOvertimeReportAsync(OvertimeReportFilterDto filter)
        {
            try
            {
                var snapshot = await _currentPermissionContext.GetSnapshotAsync();
                if (IsDataScopeDenied(snapshot))
                {
                    return ServiceResult<List<OvertimeAdministrativeSummaryReportRowDto>>.Success(new List<OvertimeAdministrativeSummaryReportRowDto>());
                }

                var activeSnapshot = snapshot!;
                if (IsEmployeeScopeDenied(activeSnapshot))
                {
                    return ServiceResult<List<OvertimeAdministrativeSummaryReportRowDto>>.Success(new List<OvertimeAdministrativeSummaryReportRowDto>());
                }

                var items = await BuildOvertimeReportBaseQuery(filter, activeSnapshot)
                    .GroupBy(x => new { x.EmployeeCode, x.EmployeeName })
                    .Select(g => new OvertimeAdministrativeSummaryReportRowDto
                    {
                        EmployeeCode = g.Key.EmployeeCode,
                        EmployeeName = g.Key.EmployeeName,
                        Quantity = g.Sum(x => x.Quantity),
                        Amount = g.Sum(x => x.Amount)
                    })
                    .OrderBy(x => x.EmployeeName)
                    .ToListAsync();

                return ServiceResult<List<OvertimeAdministrativeSummaryReportRowDto>>.Success(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İdari özet mesai raporu alınırken hata oluştu.");
                return ServiceResult<List<OvertimeAdministrativeSummaryReportRowDto>>.Fail("İdari özet mesai raporu alınırken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Detaylı iş raporu için filtrelenmiş satırları getirir.
        /// </summary>
        public async Task<ServiceResult<List<JobDetailedReportRowDto>>> GetDetailedJobReportAsync(JobFilterDto filter, CancellationToken cancellationToken = default)
        {
            try
            {
                var itemsResult = await GetAllJobsForReportAsync(filter, cancellationToken);
                if (!itemsResult.IsSuccess || itemsResult.Data == null)
                {
                    return ServiceResult<List<JobDetailedReportRowDto>>.Fail(itemsResult.Message ?? "Detaylı iş raporu alınırken bir hata oluştu.");
                }

                var items = itemsResult.Data;
                if (!items.Any())
                {
                    return ServiceResult<List<JobDetailedReportRowDto>>.Success(new List<JobDetailedReportRowDto>());
                }

                var rows = items
                    .OrderBy(x => x.CustomerName)
                    .ThenBy(x => x.StartDate)
                    .ThenBy(x => x.Reference)
                    .Select(x =>
                    {
                        return new JobDetailedReportRowDto
                        {
                            CustomerCode = x.CustomerCode ?? string.Empty,
                            CustomerName = x.CustomerName ?? string.Empty,
                            FunctionName = x.FunctionName ?? string.Empty,
                            Reference = x.Reference,
                            JobName = x.Name ?? string.Empty,
                            StartDate = x.StartDate,
                            EndDate = x.EndDate,
                            StatusName = x.StatusName ?? string.Empty,
                            IsEvaluated = x.IsEvaluated,
                            WorkAmount = x.WorkAmount,
                            ProductAmount = x.ProductAmount
                        };
                    })
                    .ToList();

                return ServiceResult<List<JobDetailedReportRowDto>>.Success(rows);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detaylı iş raporu alınırken hata oluştu.");
                return ServiceResult<List<JobDetailedReportRowDto>>.Fail("Detaylı iş raporu alınırken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Özet iş raporu için müşteri bazlı satırları getirir.
        /// </summary>
        public async Task<ServiceResult<List<JobSummaryReportRowDto>>> GetSummaryJobReportAsync(JobFilterDto filter, CancellationToken cancellationToken = default)
        {
            try
            {
                var detailedResult = await GetDetailedJobReportAsync(filter, cancellationToken);
                if (!detailedResult.IsSuccess || detailedResult.Data == null)
                {
                    return ServiceResult<List<JobSummaryReportRowDto>>.Fail(detailedResult.Message ?? "Özet iş raporu alınırken bir hata oluştu.");
                }

                var rows = detailedResult.Data
                    .GroupBy(x => new { x.CustomerCode, x.CustomerName })
                    .Select(g => new JobSummaryReportRowDto
                    {
                        CustomerCode = g.Key.CustomerCode,
                        CustomerName = g.Key.CustomerName,
                        Count = g.Count(),
                        WorkAmount = g.Sum(x => x.WorkAmount),
                        ProductAmount = g.Sum(x => x.ProductAmount)
                    })
                    .OrderBy(x => x.CustomerName)
                    .ToList();

                return ServiceResult<List<JobSummaryReportRowDto>>.Success(rows);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Özet iş raporu alınırken hata oluştu.");
                return ServiceResult<List<JobSummaryReportRowDto>>.Fail("Özet iş raporu alınırken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Detaylı fatura bekleyen işler raporu için filtrelenmiş satırları getirir.
        /// </summary>
        public async Task<ServiceResult<List<PendingInvoiceJobsDetailedReportRowDto>>> GetDetailedPendingInvoiceJobsReportAsync(PendingInvoiceJobsReportFilterDto filter, CancellationToken cancellationToken = default)
        {
            try
            {
                var snapshot = await _currentPermissionContext.GetSnapshotAsync();
                if (IsDataScopeDenied(snapshot))
                {
                    return ServiceResult<List<PendingInvoiceJobsDetailedReportRowDto>>.Success(new List<PendingInvoiceJobsDetailedReportRowDto>());
                }

                var activeSnapshot = snapshot!;

                var items = await BuildPendingInvoiceJobsReportBaseQuery(filter, activeSnapshot)
                    .OrderBy(x => x.CustomerName)
                    .ThenBy(x => x.Reference)
                    .Select(x => new PendingInvoiceJobsDetailedReportRowDto
                    {
                        CustomerCode = x.CustomerCode,
                        CustomerName = x.CustomerName,
                        Reference = x.Reference,
                        JobName = x.JobName,
                        StartDate = x.StartDate,
                        EndDate = x.EndDate,
                        Amount = x.Amount
                    })
                    .ToListAsync(cancellationToken);

                return ServiceResult<List<PendingInvoiceJobsDetailedReportRowDto>>.Success(items);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detaylı fatura bekleyen işler raporu alınırken hata oluştu.");
                return ServiceResult<List<PendingInvoiceJobsDetailedReportRowDto>>.Fail("Detaylı fatura bekleyen işler raporu alınırken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Özet fatura bekleyen işler raporu için müşteri bazlı gruplanmış satırları getirir.
        /// </summary>
        public async Task<ServiceResult<List<PendingInvoiceJobsSummaryReportRowDto>>> GetSummaryPendingInvoiceJobsReportAsync(PendingInvoiceJobsReportFilterDto filter, CancellationToken cancellationToken = default)
        {
            try
            {
                var snapshot = await _currentPermissionContext.GetSnapshotAsync();
                if (IsDataScopeDenied(snapshot))
                {
                    return ServiceResult<List<PendingInvoiceJobsSummaryReportRowDto>>.Success(new List<PendingInvoiceJobsSummaryReportRowDto>());
                }

                var activeSnapshot = snapshot!;

                var items = await BuildPendingInvoiceJobsReportBaseQuery(filter, activeSnapshot)
                    .GroupBy(x => new { x.CustomerCode, x.CustomerName })
                    .Select(g => new PendingInvoiceJobsSummaryReportRowDto
                    {
                        CustomerCode = g.Key.CustomerCode,
                        CustomerName = g.Key.CustomerName,
                        Count = g.Count(),
                        Amount = g.Sum(x => x.Amount)
                    })
                    .OrderBy(x => x.CustomerName)
                    .ToListAsync(cancellationToken);

                return ServiceResult<List<PendingInvoiceJobsSummaryReportRowDto>>.Success(items);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Özet fatura bekleyen işler raporu alınırken hata oluştu.");
                return ServiceResult<List<PendingInvoiceJobsSummaryReportRowDto>>.Fail("Özet fatura bekleyen işler raporu alınırken bir hata oluştu.");
            }
        }

        private IQueryable<PendingInvoiceJobsReportBaseRow> BuildPendingInvoiceJobsReportBaseQuery(
            PendingInvoiceJobsReportFilterDto filter,
            PermissionSnapshotDto snapshot)
        {
            var customerCode = filter.CustomerCode?.Trim();

            var query = from j in _unitOfWork.Repository<Job>().Query()
                        join c in _unitOfWork.Repository<Customer>().Query() on j.CustomerID equals c.Id into cGroup
                        from customer in cGroup.DefaultIfEmpty()
                        where j.InvoLineID == null
                              && snapshot.AllowedFunctionIds.Contains(j.FunctionID)
                              && (snapshot.CompanyScopeMode != CompanyScopeMode.CompanyBound
                                  || !snapshot.CompanyId.HasValue
                                  || j.CompanyID == snapshot.CompanyId.Value)
                        select new PendingInvoiceJobsReportBaseRow
                        {
                            CustomerId = j.CustomerID,
                            CustomerCode = customer != null ? customer.Code : string.Empty,
                            CustomerName = customer != null ? customer.Name : string.Empty,
                            Reference = j.Reference,
                            JobName = j.Name,
                            StartDate = j.StartDT,
                            EndDate = j.EndDT,
                            Amount = j.ProdSum
                        };

            if (filter.CustomerId.HasValue)
            {
                query = query.Where(x => x.CustomerId == filter.CustomerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(customerCode))
            {
                query = query.Where(x => x.CustomerCode == customerCode);
            }

            return query;
        }

        private async Task<ServiceResult<List<JobDto>>> GetAllJobsForReportAsync(JobFilterDto filter, CancellationToken cancellationToken = default)
        {
            const int reportPageSize = 500;

            var reportFilter = new JobFilterDto
            {
                FunctionId = filter.FunctionId,
                CustomerCode = filter.CustomerCode,
                CustomerId = filter.CustomerId,
                ReferenceStart = filter.ReferenceStart,
                ReferenceEnd = filter.ReferenceEnd,
                ReferenceList = filter.ReferenceList,
                JobName = filter.JobName,
                RelatedPerson = filter.RelatedPerson,
                StartDateStart = filter.StartDateStart,
                StartDateEnd = filter.StartDateEnd,
                EndDateStart = filter.EndDateStart,
                EndDateEnd = filter.EndDateEnd,
                StateId = filter.StateId,
                IsEmailSent = filter.IsEmailSent,
                IsEvaluated = filter.IsEvaluated,
                HasInvoice = filter.HasInvoice,
                EmployeeCode = filter.EmployeeCode,
                WorkTypeId = filter.WorkTypeId,
                TimeTypeId = filter.TimeTypeId,
                ProductId = filter.ProductId,
                ProductCode = filter.ProductCode,
                First = null,
                Page = 1,
                PageSize = reportPageSize,
                Sort = filter.Sort,
                Ascending = filter.Ascending
            };

            var allItems = new List<JobDto>();

            while (true)
            {
                var pageResult = await GetByFilterAsync(reportFilter, cancellationToken);
                if (!pageResult.IsSuccess || pageResult.Data == null)
                {
                    return ServiceResult<List<JobDto>>.Fail(pageResult.Message ?? "İş raporu verileri alınırken bir hata oluştu.");
                }

                var pageItems = pageResult.Data.Items ?? new List<JobDto>();
                allItems.AddRange(pageItems);

                if (pageItems.Count == 0 || allItems.Count >= pageResult.Data.TotalCount)
                {
                    break;
                }

                reportFilter.Page++;
            }

            return ServiceResult<List<JobDto>>.Success(allItems);
        }

        private IQueryable<OvertimeReportBaseRow> BuildOvertimeReportBaseQuery(
            OvertimeReportFilterDto filter,
            PermissionSnapshotDto snapshot)
        {
            var startDate = filter.StartDate.Date;
            var endDate = filter.EndDate.Date.AddDays(1);
            var customerCode = filter.CustomerCode?.Trim();
            var employeeCodes = NormalizeEmployeeCodes(filter.EmployeeCodes);
            var languageId = filter.LanguageId > 0 ? filter.LanguageId : CurrentLanguageId;

            var query = from jw in _unitOfWork.Repository<JobWork>().Query()
                        join j in _unitOfWork.Repository<Job>().Query() on jw.JobID equals j.Id
                        join e in _unitOfWork.Repository<Employee>().Query() on jw.EmployeeID equals e.Id into eGroup
                        from employee in eGroup.DefaultIfEmpty()
                        join c in _unitOfWork.Repository<Customer>().Query() on j.CustomerID equals c.Id into cGroup
                        from customer in cGroup.DefaultIfEmpty()
                        join xw in _unitOfWork.Repository<XWorkType>().Query().Where(x => x.LanguageID == languageId)
                            on jw.WorkTypeID equals xw.WorkTypeID into wGroup
                        from workType in wGroup.DefaultIfEmpty()
                        join xt in _unitOfWork.Repository<XTimeType>().Query().Where(x => x.LanguageID == languageId)
                            on jw.TimeTypeID equals xt.TimeTypeID into tGroup
                        from timeType in tGroup.DefaultIfEmpty()
                        where jw.Deleted == 0
                              && j.StartDT >= startDate
                              && j.StartDT < endDate
                              && snapshot.AllowedFunctionIds.Contains(j.FunctionID)
                              && (snapshot.CompanyScopeMode != CompanyScopeMode.CompanyBound
                                  || !snapshot.CompanyId.HasValue
                                  || j.CompanyID == snapshot.CompanyId.Value)
                              && (snapshot.EmployeeScopeBypass || snapshot.AllowedEmployeeIds.Contains(jw.EmployeeID))
                        select new OvertimeReportBaseRow
                        {
                            EmployeeCode = employee != null ? employee.Code : string.Empty,
                            EmployeeName = employee != null ? employee.Name : string.Empty,
                            TimeTypeName = timeType != null ? timeType.Name : string.Empty,
                            WorkTypeName = workType != null ? workType.Name : string.Empty,
                            Reference = j.Reference,
                            JobDate = j.StartDT,
                            CustomerCode = customer != null ? customer.Code : string.Empty,
                            CustomerName = customer != null ? customer.Name : string.Empty,
                            JobName = j.Name,
                            Notes = jw.Notes,
                            Quantity = (decimal)jw.Quantity,
                            Amount = jw.Amount
                        };

            if (!string.IsNullOrWhiteSpace(customerCode))
            {
                query = query.Where(x => x.CustomerCode == customerCode);
            }

            if (employeeCodes.Any())
            {
                query = query.Where(x => employeeCodes.Contains(x.EmployeeCode));
            }

            return query;
        }

        private sealed class PendingInvoiceJobsReportBaseRow
        {
            public decimal CustomerId { get; init; }
            public string CustomerCode { get; init; } = string.Empty;
            public string CustomerName { get; init; } = string.Empty;
            public int Reference { get; init; }
            public string JobName { get; init; } = string.Empty;
            public DateTime StartDate { get; init; }
            public DateTime? EndDate { get; init; }
            public decimal Amount { get; init; }
        }

        private static List<string> NormalizeEmployeeCodes(List<string>? employeeCodes)
        {
            return (employeeCodes ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsDataScopeDenied(PermissionSnapshotDto? snapshot)
        {
            return snapshot == null
                   || snapshot.IsDenied
                   || snapshot.CompanyScopeMode == CompanyScopeMode.Deny
                   || snapshot.AllowedFunctionIds.Count == 0;
        }

        private static bool IsEmployeeScopeDenied(PermissionSnapshotDto snapshot)
        {
            return !snapshot.EmployeeScopeBypass && snapshot.AllowedEmployeeIds.Count == 0;
        }

        private static IQueryable<Job> ApplyJobDataScope(IQueryable<Job> query, PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound && snapshot.CompanyId.HasValue)
            {
                query = query.Where(j => j.CompanyID == snapshot.CompanyId.Value);
            }

            return query.Where(j => snapshot.AllowedFunctionIds.Contains(j.FunctionID));
        }

        private sealed class OvertimeReportBaseRow
        {
            public string EmployeeCode { get; init; } = string.Empty;
            public string EmployeeName { get; init; } = string.Empty;
            public string TimeTypeName { get; init; } = string.Empty;
            public string WorkTypeName { get; init; } = string.Empty;
            public int Reference { get; init; }
            public DateTime JobDate { get; init; }
            public string CustomerCode { get; init; } = string.Empty;
            public string CustomerName { get; init; } = string.Empty;
            public string JobName { get; init; } = string.Empty;
            public string Notes { get; init; } = string.Empty;
            public decimal Quantity { get; init; }
            public decimal Amount { get; init; }
        }

        public async Task<ServiceResult<JobDto>> AddAsync(JobDto jobDto)
        {
            if (jobDto == null) throw new ArgumentNullException(nameof(jobDto));
            
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // ============ VALİDASYON KONTROLLERI ============
                
                // Müşteri kontrolü - zorunlu alan
                if (jobDto.CustomerId <= 0)
                {
                    return ServiceResult<JobDto>.Fail("Müşteri seçimi zorunludur.");
                }
                
                // Müşterinin veritabanında var olup olmadığını kontrol et
                var customerRepo = _unitOfWork.Repository<Customer>();
                var customerExists = await customerRepo.Query().AnyAsync(x => x.Id == jobDto.CustomerId);
                if (!customerExists)
                {
                    return ServiceResult<JobDto>.Fail($"Seçilen müşteri (ID: {jobDto.CustomerId}) veritabanında bulunamadı.");
                }
                
                // İş adı kontrolü
                if (string.IsNullOrWhiteSpace(jobDto.Name))
                {
                    return ServiceResult<JobDto>.Fail("İş adı zorunludur.");
                }
                
                // ============ İŞ OLUŞTURMA ============
                
                // 1. Yeni Job ID ve Reference belirle
                var jobRepo = _unitOfWork.Repository<Job>();
                var nextJobId = (await jobRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                var nextReference = (await jobRepo.Query().MaxAsync(x => (decimal?)x.Reference) ?? 15000) + 1;

                // Get Valid CompanyID - veritabanında var olmalı
                var companyRepo = _unitOfWork.Repository<Company>();
                var companyId = await companyRepo.Query().Select(x => x.Id).FirstOrDefaultAsync();
                if (companyId == 0)
                {
                    return ServiceResult<JobDto>.Fail("Veritabanında şirket kaydı bulunamadı.");
                }

                // Get Valid FunctionID - frontend'den gelen veya veritabanından ilk kayıt
                var funcRepo = _unitOfWork.Repository<Function>();
                decimal functionIdToUse;
                
                if (jobDto.FunctionId > 0)
                {
                    // Frontend'den gelen FunctionID'nin veritabanında var olup olmadığını kontrol et
                    var functionExists = await funcRepo.Query().AnyAsync(x => x.Id == jobDto.FunctionId);
                    if (!functionExists)
                    {
                        // Gelen değer geçersiz, veritabanından ilk geçerli değeri al
                        functionIdToUse = await funcRepo.Query().Select(x => x.Id).FirstOrDefaultAsync();
                        if (functionIdToUse == 0)
                        {
                            return ServiceResult<JobDto>.Fail("Veritabanında fonksiyon kaydı bulunamadı.");
                        }
                        _logger.LogWarning("Frontend'den gelen FunctionId ({FunctionId}) veritabanında bulunamadı, {NewFunctionId} kullanılıyor.", jobDto.FunctionId, functionIdToUse);
                    }
                    else
                    {
                        functionIdToUse = jobDto.FunctionId;
                    }
                }
                else
                {
                    // Frontend'den FunctionID gelmedi, veritabanından ilk değeri al
                    functionIdToUse = await funcRepo.Query().Select(x => x.Id).FirstOrDefaultAsync();
                    if (functionIdToUse == 0)
                    {
                        return ServiceResult<JobDto>.Fail("Veritabanında fonksiyon kaydı bulunamadı.");
                    }
                }

                // StateID kontrolü - veritabanında var olmalı
                var stateRepo = _unitOfWork.Repository<State>();
                var stateExists = await stateRepo.Query().AnyAsync(x => x.Id == 1);
                decimal stateIdToUse = stateExists ? 1 : await stateRepo.Query().Select(x => x.Id).FirstOrDefaultAsync();
                if (stateIdToUse == 0)
                {
                    return ServiceResult<JobDto>.Fail("Veritabanında durum kaydı bulunamadı.");
                }

                // 2. Job Entity Oluştur
                var job = new Job
                {
                    Id = nextJobId,
                    Reference = (int)nextReference, 
                    CustomerID = jobDto.CustomerId,
                    Name = jobDto.Name ?? string.Empty,
                    Contact = jobDto.Contact ?? string.Empty,
                    FunctionID = functionIdToUse, 
                    
                    StartDT = jobDto.StartDate, 
                    EndDT = jobDto.EndDate, 
                    
                    StateID = stateIdToUse, 

                    Mailed = jobDto.IsEmailSent, 
                    Evaluated = jobDto.IsEvaluated, 
                    
                    IntNotes = jobDto.IntNotes ?? string.Empty,
                    ExtNotes = jobDto.ExtNotes ?? string.Empty,
                    
                    // Default fields
                    CompanyID = companyId, 
                    CreatedDate = DateTime.Now,
                    // CreatedBy removed
                    SelectFlag = true,
                    // TaxFree removed
                    // Deleted removed (BaseEntity has IsActive, maybe check configuraton if IsActive maps to Deleted?)
                    // JobWorkConfiguration mapped Deleted to decimal. Job might be different. 
                    // Let's assume IsActive handles logical delete if BaseEntity is used correctly, or just omit if database default handles it.
                    // Checking Job.cs again, it doesn't have Deleted property explicitly.
                    Stamp = 1,
                    ProdSum = 0,
                    WorkSum = 0
                };

                await jobRepo.AddAsync(job);
                
                // ÖNEMLİ: Job'u önce veritabanına kaydet ki FK referansları doğru çalışsın
                await _unitOfWork.CommitAsync();

                decimal workSum = 0;
                decimal prodSum = 0;

                // 3. JobWorks Ekle
                if (jobDto.JobWorks != null && jobDto.JobWorks.Any())
                {
                    var jwRepo = _unitOfWork.Repository<JobWork>();
                    var nextJwId = (await jwRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0);

                    foreach (var work in jobDto.JobWorks)
                    {
                        nextJwId++;
                        var jw = new JobWork
                        {
                            Id = nextJwId,
                            JobID = job.Id,
                            EmployeeID = work.EmployeeId,
                            WorkTypeID = work.WorkTypeId,
                            TimeTypeID = work.TimeTypeId,
                            Quantity = (short)work.Quantity,
                            Amount = work.Amount,
                            Notes = work.Notes ?? string.Empty,
                            
                            SelectFlag = true,
                            Deleted = 0, // JobWork has Deleted property explicitly in config? Yes.
                            Stamp = 1
                        };
                        
                        workSum += work.Amount;
                        await jwRepo.AddAsync(jw);
                    }
                }

                // 4. JobProds Ekle
                if (jobDto.JobProds != null && jobDto.JobProds.Any())
                {
                    var jpRepo = _unitOfWork.Repository<JobProd>();
                    var nextJpId = (await jpRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0);

                    foreach (var prod in jobDto.JobProds)
                    {
                        nextJpId++;
                        var jp = new JobProd
                        {
                            Id = nextJpId,
                            JobID = job.Id,
                            ProductID = prod.ProductId,
                            Quantity = (short)prod.Quantity, // Cast to short
                            Price = prod.Price,
                            GrossAmount = prod.Quantity * prod.Price, 
                            NetAmount = prod.Quantity * prod.Price,
                            Notes = prod.Notes ?? string.Empty,
                            
                            SelectFlag = true,
                            Deleted = 0,
                            Stamp = 1
                        };

                        prodSum += jp.GrossAmount; 
                        await jpRepo.AddAsync(jp);
                    }
                }

                // Update sums
                job.WorkSum = workSum;
                job.ProdSum = prodSum;

                // JobWork ve JobProd'u kaydet
                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();
                
                // Return updated DTO with new Reference
                jobDto.Reference = (int)job.Reference;
                jobDto.Id = job.Id;
                
                return ServiceResult<JobDto>.Success(jobDto);
            }
            catch (Exception ex)
            {
                 await transaction.RollbackAsync();
                _logger.LogError(ex, "İş yaratılırken hata oluştu.");
                return ServiceResult<JobDto>.Fail("İş yaratılırken bir hata oluştu.");
            }
        }
        public async Task<ServiceResult<List<JobLogDto>>> GetJobHistoryAsync(decimal jobId)
        {
            try
            {
                var languageId = CurrentLanguageId;
                var snapshot = await _currentPermissionContext.GetSnapshotAsync();
                if (IsDataScopeDenied(snapshot))
                {
                    return ServiceResult<List<JobLogDto>>.Success(new List<JobLogDto>());
                }

                var activeSnapshot = snapshot!;
                var canAccessJob = await ApplyJobDataScope(_unitOfWork.Repository<Job>().Query(), activeSnapshot)
                    .AnyAsync(j => j.Id == jobId);

                if (!canAccessJob)
                {
                    return ServiceResult<List<JobLogDto>>.Success(new List<JobLogDto>());
                }

                // JobLog tablosundan iş geçmişini getir
                // ActionDT: İşlem tarihi, Destination: E-posta adresi vb. hedef bilgisi
                var query = from log in _unitOfWork.Repository<JobLog>().Query()
                            where log.JobID == jobId
                            join u in _unitOfWork.Repository<User>().Query() on log.UserID equals u.Id
                            join xl in _unitOfWork.Repository<XLogAction>().Query().Where(x => x.LanguageID == languageId)
                                on log.LogActionID equals xl.LogActionID into xlGroup
                            from xLog in xlGroup.DefaultIfEmpty()
                            orderby log.ActionDT descending
                            select new JobLogDto
                            {
                                Id = log.Id,
                                LogDate = log.ActionDT,
                                Destination = log.Destination, // E-posta adresi vb.
                                UserId = log.UserID,
                                UserCode = u.Code,
                                UserName = u.Name,
                                UserEmail = string.Empty, // User tablosunda Email yok
                                LogActionId = log.LogActionID,
                                ActionName = xLog != null ? xLog.Name : "Bilinmiyor"
                            };

                var logs = await query.ToListAsync();
                return ServiceResult<List<JobLogDto>>.Success(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş geçmişi getirilirken hata oluştu. JobId: {JobId}", jobId);
                return ServiceResult<List<JobLogDto>>.Fail("İş geçmişi yüklenirken bir hata oluştu.");
            }
        }


        public async Task<List<string>> GetTableColumnsAsync(string tableName)
        {
            var columns = new List<string>();
            if (string.IsNullOrWhiteSpace(tableName))
            {
                _logger.LogWarning("Tablo kolonları alınamadı: tableName boş.");
                return columns;
            }

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogError("Tablo kolonları alınamadı: DefaultConnection tanımlı değil.");
                return columns;
            }

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await connection.OpenAsync(cts.Token);

                await using var command = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName",
                    connection)
                {
                    CommandTimeout = 15
                };

                command.Parameters.AddWithValue("@tableName", tableName);
                await using var reader = await command.ExecuteReaderAsync(cts.Token);
                while (await reader.ReadAsync(cts.Token))
                {
                    columns.Add(reader.GetString(0));
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Tablo kolonları sorgusu timeout. TableName: {TableName}", tableName);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex, "Tablo kolonları alınırken SQL hatası oluştu. TableName: {TableName}", tableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tablo kolonları alınırken beklenmeyen hata oluştu. TableName: {TableName}", tableName);
            }

            return columns;
        }
    }
}
