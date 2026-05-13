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
        private const decimal LegacyActiveStateId = 1m;
        private const decimal OpenStateId = 110m;
        private const decimal CompletedStateId = 120m;
        private const decimal PricedStateId = 130m;
        private const decimal InvoicedStateId = 140m;
        private const decimal ClosedStateId = 150m;
        private const decimal DiscardedStateId = 160m;
        private const decimal OpenInvoiceStateId = 210m;
        private const decimal ConfirmedInvoiceStateId = 220m;
        private const decimal IssuedInvoiceStateId = 230m;
        private static readonly decimal[] LiveInvoiceStateIds = { OpenInvoiceStateId, ConfirmedInvoiceStateId, IssuedInvoiceStateId };

        private const decimal CreateLogActionId = 510m;
        private const decimal CompleteLogActionId = 610m;
        private const decimal UndoCompleteLogActionId = 620m;
        private const decimal PriceLogActionId = 630m;
        private const decimal UndoPriceLogActionId = 640m;
        private const decimal InvoiceLogActionId = 650m;
        private const decimal UndoInvoiceLogActionId = 660m;
        private const decimal CloseLogActionId = 670m;
        private const decimal UndoCloseLogActionId = 680m;
        private const decimal DiscardLogActionId = 690m;
        private const decimal UndoDiscardLogActionId = 700m;
        private const decimal EvaluateLogActionId = 710m;
        private const decimal UndoEvaluateLogActionId = 720m;

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
                var scopedJobs = ApplyJobDataScope(_unitOfWork.Repository<Job>().Query().AsNoTracking(), activeSnapshot);
                var query = from j in scopedJobs
                            join c in _unitOfWork.Repository<Customer>().Query().AsNoTracking() on j.CustomerID equals c.Id into cGroup
                            from customer in cGroup.DefaultIfEmpty()
                            join xf in _unitOfWork.Repository<XFunction>().Query().AsNoTracking().Where(x => x.LanguageID == languageId)
                                on j.FunctionID equals xf.FunctionID into fGroup
                            from xFunc in fGroup.DefaultIfEmpty()
                            join xs in _unitOfWork.Repository<XState>().Query().AsNoTracking().Where(x => x.LanguageID == languageId)
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
                    if (IsOpenJobState(filter.StateId.Value))
                    {
                        query = query.Where(x => x.Job.StateID == OpenStateId || x.Job.StateID == LegacyActiveStateId);
                    }
                    else
                    {
                        query = query.Where(x => x.Job.StateID == filter.StateId.Value);
                    }
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
                    var invoicedJobIds = _unitOfWork.Repository<InvoJob>().Query()
                        .Where(ij => ij.Deleted == 0)
                        .Select(ij => ij.JobID);

                    if (filter.HasInvoice.Value)
                    {
                        query = query.Where(x => x.Job.InvoLineID != null || invoicedJobIds.Contains(x.Job.Id));
                    }
                    else
                    {
                        query = query.Where(x => x.Job.InvoLineID == null && !invoicedJobIds.Contains(x.Job.Id));
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

                var orderedQuery = filter.ReferenceStart.HasValue
                    ? query
                        .OrderBy(x => x.Job.Reference)
                    : filter.ReferenceEnd.HasValue
                        ? query
                            .OrderByDescending(x => x.Job.Reference)
                        : query
                            .OrderByDescending(x => x.Job.StartDT)
                            .ThenByDescending(x => x.Job.Reference);

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
                        HasInvoiceLink = x.Job.InvoLineID != null,
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

        public async Task<ServiceResult<JobEmailDraftDto>> PrepareEmailDraftAsync(IReadOnlyCollection<int> references, CancellationToken cancellationToken = default)
        {
            var normalizedReferences = references?
                .Where(x => x > 0)
                .Distinct()
                .ToList() ?? new List<int>();

            if (normalizedReferences.Count == 0)
            {
                return ServiceResult<JobEmailDraftDto>.Fail("En az bir iş seçilmelidir.");
            }

            try
            {
                var languageId = CurrentLanguageId;
                var snapshot = await _currentPermissionContext.GetSnapshotAsync();
                if (IsDataScopeDenied(snapshot))
                {
                    return ServiceResult<JobEmailDraftDto>.Fail("Seçilen işler bulunamadı veya erişim yetkiniz yok.");
                }

                var activeSnapshot = snapshot!;
                var scopedJobs = ApplyJobDataScope(_unitOfWork.Repository<Job>().Query(), activeSnapshot);

                var items = await (from job in scopedJobs
                                   join customer in _unitOfWork.Repository<Customer>().Query()
                                       on job.CustomerID equals customer.Id into customerGroup
                                   from customer in customerGroup.DefaultIfEmpty()
                                   join function in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == languageId)
                                       on job.FunctionID equals function.FunctionID into functionGroup
                                   from function in functionGroup.DefaultIfEmpty()
                                   join state in _unitOfWork.Repository<XState>().Query().Where(x => x.LanguageID == languageId)
                                       on job.StateID equals state.StateID into stateGroup
                                   from state in stateGroup.DefaultIfEmpty()
                                   where normalizedReferences.Contains(job.Reference)
                                   select new JobEmailItemDto
                                   {
                                       JobId = job.Id,
                                       Reference = job.Reference,
                                       FunctionName = function != null ? function.Name : string.Empty,
                                       StateId = job.StateID,
                                       StatusName = state != null ? state.Name : string.Empty,
                                       CustomerCode = customer != null ? customer.Code : string.Empty,
                                       CustomerName = customer != null ? customer.Name : string.Empty,
                                       CustomerEmail = customer != null ? customer.EMail : string.Empty,
                                       Name = job.Name,
                                       RelatedPerson = job.Contact,
                                       StartDate = job.StartDT,
                                       EndDate = job.EndDT,
                                       WorkAmount = job.WorkSum,
                                       ProductAmount = job.ProdSum,
                                       IsEmailSent = job.Mailed,
                                       IsEvaluated = job.Evaluated
                                   }).ToListAsync(cancellationToken);

                if (items.Count != normalizedReferences.Count)
                {
                    return ServiceResult<JobEmailDraftDto>.Fail("Seçilen işler bulunamadı veya erişim yetkiniz yok.");
                }

                var orderedItems = normalizedReferences
                    .Join(items, reference => reference, item => item.Reference, (_, item) => item)
                    .ToList();

                if (orderedItems.Any(x => !IsEmailEligibleState(x.StateId)))
                {
                    return ServiceResult<JobEmailDraftDto>.Fail("E-posta operasyonu sadece fiyatlanmış, faturalanmış ya da tamamlanmış işler için geçerlidir.");
                }

                return ServiceResult<JobEmailDraftDto>.Success(new JobEmailDraftDto
                {
                    RecipientEmail = ResolveDefaultRecipientEmail(orderedItems),
                    Items = orderedItems
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş e-posta taslağı hazırlanırken hata oluştu.");
                return ServiceResult<JobEmailDraftDto>.Fail("E-posta taslağı hazırlanamadı.");
            }
        }

        public async Task<ServiceResult> MarkEmailSentAsync(IReadOnlyCollection<int> references, CancellationToken cancellationToken = default)
        {
            var normalizedReferences = references?
                .Where(x => x > 0)
                .Distinct()
                .ToList() ?? new List<int>();

            if (normalizedReferences.Count == 0)
            {
                return ServiceResult.Fail("En az bir iş seçilmelidir.");
            }

            try
            {
                var snapshot = await _currentPermissionContext.GetSnapshotAsync();
                if (IsDataScopeDenied(snapshot))
                {
                    return ServiceResult.Fail("Seçilen işler bulunamadı veya erişim yetkiniz yok.");
                }

                var activeSnapshot = snapshot!;
                var jobRepo = _unitOfWork.Repository<Job>();
                var jobs = await ApplyJobDataScope(jobRepo.Query(), activeSnapshot)
                    .Where(x => normalizedReferences.Contains(x.Reference))
                    .ToListAsync(cancellationToken);

                if (jobs.Count != normalizedReferences.Count)
                {
                    return ServiceResult.Fail("Seçilen işler bulunamadı veya erişim yetkiniz yok.");
                }

                if (jobs.Any(x => !IsEmailEligibleState(x.StateID)))
                {
                    return ServiceResult.Fail("E-posta operasyonu sadece fiyatlanmış, faturalanmış ya da tamamlanmış işler için geçerlidir.");
                }

                using var transaction = await _unitOfWork.BeginTransactionAsync();
                foreach (var job in jobs)
                {
                    job.Mailed = true;
                    job.Stamp = 1;
                    jobRepo.Update(job);
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş e-posta durumu güncellenirken hata oluştu.");
                return ServiceResult.Fail("İşlerin e-posta durumu güncellenemedi.");
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
                                CustomerCode = customer != null ? customer.Code : null,
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

                var invoiceInfo = await ResolveJobInvoiceInfoAsync(jobDto.Id, jobDto.InvoLineId);
                jobDto.HasInvoiceLink = invoiceInfo.HasInvoiceLink;
                jobDto.InvoLineId = invoiceInfo.InvoLineId;
                jobDto.InvoiceReference = invoiceInfo.InvoiceReference;
                jobDto.InvoiceName = invoiceInfo.InvoiceName;

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
                                    WorkTypeId = jw.WorkTypeID,
                                    WorkTypeName = xWork != null ? xWork.Name : null,
                                    TimeTypeId = jw.TimeTypeID,
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

            var liveInvoiceJobIds =
                from invoJob in _unitOfWork.Repository<InvoJob>().Query()
                    .Where(x => x.Deleted == 0)
                join line in _unitOfWork.Repository<InvoLine>().Query()
                        .Where(x => x.Deleted == 0)
                    on invoJob.InvoLineID equals line.Id
                join invoice in _unitOfWork.Repository<Invoice>().Query()
                        .Where(x => LiveInvoiceStateIds.Contains(x.StateID))
                    on line.InvoiceID equals invoice.Id
                select invoJob.JobID;

            var query = from j in _unitOfWork.Repository<Job>().Query()
                        join c in _unitOfWork.Repository<Customer>().Query() on j.CustomerID equals c.Id into cGroup
                        from customer in cGroup.DefaultIfEmpty()
                        where j.InvoLineID == null
                              && j.StateID == PricedStateId
                              && !liveInvoiceJobIds.Contains(j.Id)
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

        private static bool IsEmailEligibleState(decimal stateId)
        {
            return stateId == CompletedStateId
                || stateId == PricedStateId
                || stateId == InvoicedStateId;
        }

        private static string ResolveDefaultRecipientEmail(IEnumerable<JobEmailItemDto> items)
        {
            var distinctEmails = items
                .Select(x => x.CustomerEmail?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return distinctEmails.Count == 1
                ? distinctEmails[0]!
                : string.Empty;
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

                    Mailed = false, 
                    Evaluated = false, 
                    
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
                            NetAmount = prod.NetAmount,
                            Notes = prod.Notes ?? string.Empty,
                            
                            SelectFlag = true,
                            Deleted = 0,
                            Stamp = 1
                        };

                        prodSum += jp.NetAmount;
                        await jpRepo.AddAsync(jp);
                    }
                }

                // 5. JobProdCats Ekle
                if (jobDto.JobProdCats != null && jobDto.JobProdCats.Any())
                {
                    var jpcRepo = _unitOfWork.Repository<JobProdCat>();
                    var nextJpcId = (await jpcRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0);
                    decimal discountedProdSum = 0;

                    foreach (var category in jobDto.JobProdCats)
                    {
                        nextJpcId++;
                        var jpc = new JobProdCat
                        {
                            Id = nextJpcId,
                            JobID = job.Id,
                            ProdCatID = category.CategoryId,
                            GrossAmount = category.GrossAmount,
                            DiscPercentage = category.DiscPercentage,
                            DiscAmount = category.DiscAmount,
                            NetAmount = category.NetAmount,
                            Deleted = 0,
                            Stamp = 1
                        };

                        discountedProdSum += jpc.NetAmount;
                        await jpcRepo.AddAsync(jpc);
                    }

                    prodSum = discountedProdSum;
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
                jobDto.IsEmailSent = false;
                jobDto.IsEvaluated = false;
                
                return ServiceResult<JobDto>.Success(jobDto);
            }
            catch (Exception ex)
            {
                 await transaction.RollbackAsync();
                _logger.LogError(ex, "İş yaratılırken hata oluştu.");
                return ServiceResult<JobDto>.Fail("İş yaratılırken bir hata oluştu.");
            }
        }

        public async Task<ServiceResult<JobDto>> UpdateAsync(JobDto jobDto)
        {
            if (jobDto == null) throw new ArgumentNullException(nameof(jobDto));

            if (jobDto.Reference <= 0)
            {
                return ServiceResult<JobDto>.Fail("İş referansı zorunludur.");
            }

            if (jobDto.CustomerId <= 0)
            {
                return ServiceResult<JobDto>.Fail("Müşteri seçimi zorunludur.");
            }

            if (jobDto.FunctionId <= 0)
            {
                return ServiceResult<JobDto>.Fail("Fonksiyon seçimi zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(jobDto.Name))
            {
                return ServiceResult<JobDto>.Fail("İş adı zorunludur.");
            }

            try
            {
                var snapshot = await _currentPermissionContext.GetSnapshotAsync();
                if (IsDataScopeDenied(snapshot))
                {
                    return ServiceResult<JobDto>.Fail("İş kaydı kapsam dışında.");
                }

                var activeSnapshot = snapshot!;
                if (!activeSnapshot.AllowedFunctionIds.Contains(jobDto.FunctionId))
                {
                    return ServiceResult<JobDto>.Fail("Seçilen fonksiyon kapsam dışı.");
                }

                var jobRepo = _unitOfWork.Repository<Job>();
                var job = await ApplyJobDataScope(jobRepo.Query(), activeSnapshot)
                    .SingleOrDefaultAsync(x => x.Reference == jobDto.Reference);
                if (job == null)
                {
                    return ServiceResult<JobDto>.Fail("İş bulunamadı.");
                }

                var customerExists = await _unitOfWork.Repository<Customer>().Query()
                    .AnyAsync(x => x.Id == jobDto.CustomerId);
                if (!customerExists)
                {
                    return ServiceResult<JobDto>.Fail("Seçilen müşteri bulunamadı.");
                }

                var functionExists = await _unitOfWork.Repository<Function>().Query()
                    .AnyAsync(x => x.Id == jobDto.FunctionId);
                if (!functionExists)
                {
                    return ServiceResult<JobDto>.Fail("Seçilen fonksiyon bulunamadı.");
                }

                var jobWorkRepo = _unitOfWork.Repository<JobWork>();
                var jobProdRepo = _unitOfWork.Repository<JobProd>();
                var jobProdCatRepo = _unitOfWork.Repository<JobProdCat>();

                var workRows = await jobWorkRepo.Query()
                    .Where(x => x.JobID == job.Id && x.Deleted == 0)
                    .ToListAsync();
                var editableWorkRows = activeSnapshot.EmployeeScopeBypass
                    ? workRows
                    : workRows.Where(x => activeSnapshot.AllowedEmployeeIds.Contains(x.EmployeeID)).ToList();
                var productRows = await jobProdRepo.Query()
                    .Where(x => x.JobID == job.Id && x.Deleted == 0)
                    .ToListAsync();
                var categoryRows = await jobProdCatRepo.Query()
                    .Where(x => x.JobID == job.Id && x.Deleted == 0)
                    .ToListAsync();

                var workDtos = jobDto.JobWorks ?? new List<JobWorkDto>();
                var productDtos = jobDto.JobProds ?? new List<JobProdDto>();
                var categoryDtos = jobDto.JobProdCats ?? new List<JobProdCatDto>();

                var workRowsById = editableWorkRows.ToDictionary(x => x.Id);
                var productRowsById = productRows.ToDictionary(x => x.Id);
                var categoryRowsByCategoryId = categoryRows
                    .GroupBy(x => x.ProdCatID)
                    .ToDictionary(x => x.Key, x => x.First());

                var employeeIds = workDtos
                    .Where(x => x.EmployeeId > 0)
                    .Select(x => x.EmployeeId)
                    .Distinct()
                    .ToList();
                if (employeeIds.Count > 0)
                {
                    var existingEmployeeIds = await _unitOfWork.Repository<Employee>().Query()
                        .Where(x => employeeIds.Contains(x.Id))
                        .Select(x => x.Id)
                        .ToListAsync();

                    if (existingEmployeeIds.Count != employeeIds.Count)
                    {
                        return ServiceResult<JobDto>.Fail("Seçilen çalışan bulunamadı.");
                    }
                }

                var productIds = productDtos
                    .Where(x => x.ProductId > 0)
                    .Select(x => x.ProductId)
                    .Distinct()
                    .ToList();
                var productCategoryByProductId = new Dictionary<decimal, decimal>();
                if (productIds.Count > 0)
                {
                    productCategoryByProductId = await _unitOfWork.Repository<Product>().Query()
                        .Where(x => productIds.Contains(x.Id))
                        .Select(x => new { x.Id, x.ProdCatID })
                        .ToDictionaryAsync(x => x.Id, x => x.ProdCatID);

                    if (productCategoryByProductId.Count != productIds.Count)
                    {
                        return ServiceResult<JobDto>.Fail("Seçilen ürün bulunamadı.");
                    }
                }

                foreach (var product in productDtos.Where(x => x.CategoryId <= 0 && x.ProductId > 0))
                {
                    if (productCategoryByProductId.TryGetValue(product.ProductId, out var categoryId))
                    {
                        product.CategoryId = categoryId;
                    }
                }

                var categoryIds = categoryDtos
                    .Select(x => x.CategoryId)
                    .Concat(productDtos.Select(x => x.CategoryId))
                    .Where(x => x > 0)
                    .Distinct()
                    .ToList();
                if (categoryIds.Count > 0)
                {
                    var existingCategoryCount = await _unitOfWork.Repository<ProdCat>().Query()
                        .CountAsync(x => categoryIds.Contains(x.Id));

                    if (existingCategoryCount != categoryIds.Count)
                    {
                        return ServiceResult<JobDto>.Fail("Ürün kategorisi bulunamadı.");
                    }
                }

                foreach (var work in workDtos)
                {
                    if (work.Id > 0 && !workRowsById.ContainsKey(work.Id))
                    {
                        return ServiceResult<JobDto>.Fail("Mesai satırı bulunamadı.");
                    }

                    if (work.EmployeeId <= 0)
                    {
                        return ServiceResult<JobDto>.Fail("Çalışan seçimi zorunludur.");
                    }

                    if (!activeSnapshot.EmployeeScopeBypass && !activeSnapshot.AllowedEmployeeIds.Contains(work.EmployeeId))
                    {
                        return ServiceResult<JobDto>.Fail("Seçilen çalışan kapsam dışı.");
                    }

                    if (work.WorkTypeId <= 0)
                    {
                        return ServiceResult<JobDto>.Fail("Görev tipi zorunludur.");
                    }

                    if (work.TimeTypeId <= 0)
                    {
                        return ServiceResult<JobDto>.Fail("Mesai tipi zorunludur.");
                    }

                    if (work.Quantity < 0 || work.Quantity > short.MaxValue)
                    {
                        return ServiceResult<JobDto>.Fail("Mesai miktarı geçersiz.");
                    }
                }

                foreach (var product in productDtos)
                {
                    if (product.Id > 0 && !productRowsById.ContainsKey(product.Id))
                    {
                        return ServiceResult<JobDto>.Fail("Ürün satırı bulunamadı.");
                    }

                    if (product.ProductId <= 0)
                    {
                        return ServiceResult<JobDto>.Fail("Ürün seçimi zorunludur.");
                    }

                    if (product.CategoryId <= 0)
                    {
                        return ServiceResult<JobDto>.Fail("Ürün kategorisi zorunludur.");
                    }

                    if (product.Quantity < 0 || product.Quantity > short.MaxValue)
                    {
                        return ServiceResult<JobDto>.Fail("Ürün miktarı geçersiz.");
                    }
                }

                foreach (var category in categoryDtos)
                {
                    if (category.CategoryId <= 0)
                    {
                        return ServiceResult<JobDto>.Fail("Ürün kategorisi zorunludur.");
                    }
                }

                using var transaction = await _unitOfWork.BeginTransactionAsync();
                try
                {
                    job.FunctionID = jobDto.FunctionId;
                    job.CustomerID = jobDto.CustomerId;
                    job.Name = jobDto.Name.Trim();
                    job.Contact = jobDto.Contact?.Trim() ?? string.Empty;
                    job.StartDT = jobDto.StartDate;
                    job.EndDT = jobDto.EndDate;
                    job.IntNotes = jobDto.IntNotes ?? string.Empty;
                    job.ExtNotes = jobDto.ExtNotes ?? string.Empty;
                    job.Stamp = 1;

                    var nextWorkId = await jobWorkRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0;
                    foreach (var workDto in workDtos)
                    {
                        var isNewWork = false;
                        if (!workRowsById.TryGetValue(workDto.Id, out var work))
                        {
                            isNewWork = true;
                            work = new JobWork
                            {
                                Id = ++nextWorkId,
                                JobID = job.Id,
                                EmployeeID = workDto.EmployeeId,
                                Deleted = 0,
                                Stamp = 1
                            };
                            workRows.Add(work);
                            await jobWorkRepo.AddAsync(work);
                        }

                        work.WorkTypeID = workDto.WorkTypeId;
                        work.TimeTypeID = workDto.TimeTypeId;
                        work.Quantity = (short)Math.Round(workDto.Quantity);
                        work.Amount = workDto.Amount;
                        work.Notes = workDto.Notes ?? string.Empty;
                        work.SelectFlag = workDto.SelectFlag;
                        work.Stamp = 1;
                        if (!isNewWork)
                        {
                            jobWorkRepo.Update(work);
                        }
                    }

                    var nextProductRowId = await jobProdRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0;
                    foreach (var productDto in productDtos)
                    {
                        var isNewProduct = false;
                        if (!productRowsById.TryGetValue(productDto.Id, out var product))
                        {
                            isNewProduct = true;
                            product = new JobProd
                            {
                                Id = ++nextProductRowId,
                                JobID = job.Id,
                                ProductID = productDto.ProductId,
                                Deleted = 0,
                                Stamp = 1
                            };
                            productRows.Add(product);
                            await jobProdRepo.AddAsync(product);
                        }

                        product.Quantity = (short)Math.Round(productDto.Quantity);
                        product.Price = productDto.Price;
                        product.GrossAmount = productDto.GrossAmount;
                        product.NetAmount = productDto.NetAmount;
                        product.Notes = productDto.Notes ?? string.Empty;
                        product.SelectFlag = productDto.SelectFlag;
                        product.Stamp = 1;
                        if (!isNewProduct)
                        {
                            jobProdRepo.Update(product);
                        }
                    }

                    var nextProductCategoryRowId = await jobProdCatRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0;
                    foreach (var categoryDto in categoryDtos)
                    {
                        var isNewCategory = false;
                        if (!categoryRowsByCategoryId.TryGetValue(categoryDto.CategoryId, out var category))
                        {
                            isNewCategory = true;
                            category = new JobProdCat
                            {
                                Id = ++nextProductCategoryRowId,
                                JobID = job.Id,
                                ProdCatID = categoryDto.CategoryId,
                                Deleted = 0,
                                Stamp = 1
                            };
                            categoryRows.Add(category);
                            categoryRowsByCategoryId[categoryDto.CategoryId] = category;
                            await jobProdCatRepo.AddAsync(category);
                        }

                        category.GrossAmount = categoryDto.GrossAmount;
                        category.DiscPercentage = categoryDto.DiscPercentage;
                        category.DiscAmount = categoryDto.DiscAmount;
                        category.NetAmount = categoryDto.NetAmount;
                        category.Stamp = 1;
                        if (!isNewCategory)
                        {
                            jobProdCatRepo.Update(category);
                        }
                    }

                    job.WorkSum = workRows.Sum(x => x.Amount);
                    job.ProdSum = categoryRows.Any()
                        ? categoryRows.Sum(x => x.NetAmount)
                        : productRows.Sum(x => x.NetAmount);
                    jobRepo.Update(job);

                    await _unitOfWork.CommitAsync();
                    await transaction.CommitAsync();

                    jobDto.Id = job.Id;
                    return ServiceResult<JobDto>.Success(jobDto);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş güncellenirken hata oluştu. Reference={Reference}", jobDto.Reference);
                return ServiceResult<JobDto>.Fail("İş güncellenirken bir hata oluştu.");
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

        public async Task<ServiceResult> ExecuteWorkflowActionAsync(int reference, JobWorkflowAction action)
        {
            if (reference <= 0)
            {
                return ServiceResult.Fail("İş referansı zorunludur.");
            }

            if (!_currentPermissionContext.TryGetCurrentUserId(out var userId))
            {
                return ServiceResult.Fail("Kullanıcı bilgisi doğrulanamadı.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsDataScopeDenied(snapshot))
            {
                return ServiceResult.Fail("İş kaydı kapsam dışında.");
            }

            var activeSnapshot = snapshot!;
            var jobRepo = _unitOfWork.Repository<Job>();
            var job = await ApplyJobDataScope(jobRepo.Query(), activeSnapshot)
                .SingleOrDefaultAsync(x => x.Reference == reference);
            if (job == null)
            {
                return ServiceResult.Fail("İş bulunamadı.");
            }

            var hasInvoiceLink = await HasActiveInvoiceLinkAsync(job.Id, job.InvoLineID);
            if (job.Evaluated && action != JobWorkflowAction.UndoEvaluate)
            {
                return ServiceResult.Fail("Değerlendirilmiş işlerde sadece değerlendirmeyi geri alma işlemi yapılabilir.");
            }

            var nextStateId = job.StateID;
            var nextEvaluated = job.Evaluated;
            decimal logActionId;

            switch (action)
            {
                case JobWorkflowAction.Complete:
                    if (job.StateID != OpenStateId && job.StateID != LegacyActiveStateId)
                    {
                        return ServiceResult.Fail("Tamamlama işlemi için iş açık durumda olmalıdır.");
                    }
                    if (hasInvoiceLink)
                    {
                        return ServiceResult.Fail("Faturalı işte tamamlama işlemi yapılamaz.");
                    }
                    nextStateId = CompletedStateId;
                    logActionId = CompleteLogActionId;
                    break;
                case JobWorkflowAction.UndoComplete:
                    if (job.StateID != CompletedStateId)
                    {
                        return ServiceResult.Fail("Tamamlamayı geri alma işlemi için iş tamamlandı durumda olmalıdır.");
                    }
                    if (hasInvoiceLink)
                    {
                        return ServiceResult.Fail("Faturalı işte tamamlamayı geri alma işlemi yapılamaz.");
                    }
                    nextStateId = OpenStateId;
                    logActionId = UndoCompleteLogActionId;
                    break;
                case JobWorkflowAction.Price:
                    if (job.StateID != CompletedStateId)
                    {
                        return ServiceResult.Fail("Fiyatlama işlemi için iş tamamlandı durumda olmalıdır.");
                    }
                    if (hasInvoiceLink)
                    {
                        return ServiceResult.Fail("Faturalı işte fiyatlama işlemi yapılamaz.");
                    }
                    nextStateId = PricedStateId;
                    logActionId = PriceLogActionId;
                    break;
                case JobWorkflowAction.UndoPrice:
                    if (job.StateID != PricedStateId)
                    {
                        return ServiceResult.Fail("Fiyatlamayı geri alma işlemi için iş fiyatlandı durumda olmalıdır.");
                    }
                    if (hasInvoiceLink)
                    {
                        return ServiceResult.Fail("Faturalı işte fiyatlamayı geri alma işlemi yapılamaz.");
                    }
                    nextStateId = CompletedStateId;
                    logActionId = UndoPriceLogActionId;
                    break;
                case JobWorkflowAction.Close:
                    if (job.StateID != CompletedStateId)
                    {
                        return ServiceResult.Fail("Kapatma işlemi için iş tamamlandı durumda olmalıdır.");
                    }
                    nextStateId = ClosedStateId;
                    logActionId = CloseLogActionId;
                    break;
                case JobWorkflowAction.UndoClose:
                    if (job.StateID != ClosedStateId)
                    {
                        return ServiceResult.Fail("Kapatmayı geri alma işlemi için iş kapatıldı durumda olmalıdır.");
                    }
                    nextStateId = CompletedStateId;
                    logActionId = UndoCloseLogActionId;
                    break;
                case JobWorkflowAction.Discard:
                    if (!CanDiscardJobState(job.StateID))
                    {
                        return ServiceResult.Fail("İptal et işlemi bu durum için geçerli değildir.");
                    }
                    if (hasInvoiceLink)
                    {
                        return ServiceResult.Fail("Faturalı işte iptal işlemi yapılamaz.");
                    }
                    nextStateId = DiscardedStateId;
                    logActionId = DiscardLogActionId;
                    break;
                case JobWorkflowAction.UndoDiscard:
                    if (job.StateID != DiscardedStateId)
                    {
                        return ServiceResult.Fail("İptali geri alma işlemi için iş iptal edildi durumda olmalıdır.");
                    }
                    nextStateId = await ResolvePreviousJobStateForUndoDiscardAsync(job.Id);
                    logActionId = UndoDiscardLogActionId;
                    break;
                case JobWorkflowAction.Evaluate:
                    if (!CanEvaluateJobState(job.StateID) || job.Evaluated)
                    {
                        return ServiceResult.Fail("Değerlendirme işlemi bu iş için uygun değildir.");
                    }
                    nextEvaluated = true;
                    logActionId = EvaluateLogActionId;
                    break;
                case JobWorkflowAction.UndoEvaluate:
                    if (!CanEvaluateJobState(job.StateID) || !job.Evaluated)
                    {
                        return ServiceResult.Fail("Değerlendirmeyi geri alma işlemi bu iş için uygun değildir.");
                    }
                    nextEvaluated = false;
                    logActionId = UndoEvaluateLogActionId;
                    break;
                default:
                    return ServiceResult.Fail("Geçersiz işlem.");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                job.StateID = nextStateId;
                job.Evaluated = nextEvaluated;
                job.Stamp = 1;
                jobRepo.Update(job);

                var jobLogRepo = _unitOfWork.Repository<JobLog>();
                var nextJobLogId = (await jobLogRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                await jobLogRepo.AddAsync(new JobLog
                {
                    Id = nextJobLogId,
                    JobID = job.Id,
                    ActionDT = DateTime.Now,
                    LogActionID = logActionId,
                    UserID = userId,
                    Destination = string.Empty,
                    Stamp = 1
                });

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();
                return ServiceResult.Success("İş durumu güncellendi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "İş workflow işlemi sırasında hata oluştu. Reference={Reference}, Action={Action}", reference, action);
                return ServiceResult.Fail("İş durumu güncellenemedi.");
            }
        }

        private async Task<decimal> ResolvePreviousJobStateForUndoDiscardAsync(decimal jobId)
        {
            var jobLogRepo = _unitOfWork.Repository<JobLog>();
            var latestDiscard = await jobLogRepo.Query()
                .Where(x => x.JobID == jobId && x.LogActionID == DiscardLogActionId)
                .OrderByDescending(x => x.ActionDT)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (latestDiscard == null)
            {
                return OpenStateId;
            }

            var priorLogActionIds = await jobLogRepo.Query()
                .Where(x => x.JobID == jobId
                            && (x.ActionDT < latestDiscard.ActionDT
                                || (x.ActionDT == latestDiscard.ActionDT && x.Id < latestDiscard.Id)))
                .OrderByDescending(x => x.ActionDT)
                .ThenByDescending(x => x.Id)
                .Select(x => x.LogActionID)
                .ToListAsync();

            foreach (var actionId in priorLogActionIds)
            {
                var inferredState = ResolveStateFromJobLogAction(actionId);
                if (inferredState.HasValue && inferredState.Value != DiscardedStateId)
                {
                    return inferredState.Value == LegacyActiveStateId ? OpenStateId : inferredState.Value;
                }
            }

            return OpenStateId;
        }

        private static decimal? ResolveStateFromJobLogAction(decimal logActionId)
        {
            return logActionId switch
            {
                CreateLogActionId => OpenStateId,
                CompleteLogActionId => CompletedStateId,
                UndoCompleteLogActionId => OpenStateId,
                PriceLogActionId => PricedStateId,
                UndoPriceLogActionId => CompletedStateId,
                InvoiceLogActionId => InvoicedStateId,
                UndoInvoiceLogActionId => PricedStateId,
                CloseLogActionId => ClosedStateId,
                UndoCloseLogActionId => CompletedStateId,
                DiscardLogActionId => DiscardedStateId,
                _ => null
            };
        }

        private static bool IsOpenJobState(decimal stateId)
        {
            return stateId == LegacyActiveStateId || stateId == OpenStateId;
        }

        private static bool CanDiscardJobState(decimal stateId)
        {
            return IsOpenJobState(stateId);
        }

        private static bool CanEvaluateJobState(decimal stateId)
        {
            return stateId == ClosedStateId || stateId == DiscardedStateId;
        }

        private async Task<bool> HasActiveInvoiceLinkAsync(decimal jobId, decimal? invoLineId)
        {
            if (invoLineId.HasValue)
            {
                return true;
            }

            return await _unitOfWork.Repository<InvoJob>().Query()
                .AnyAsync(x => x.JobID == jobId && x.Deleted == 0);
        }

        private async Task<(decimal? InvoLineId, int? InvoiceReference, string? InvoiceName, bool HasInvoiceLink)> ResolveJobInvoiceInfoAsync(decimal jobId, decimal? currentInvoLineId)
        {
            var invoLineIds = new HashSet<decimal>();
            if (currentInvoLineId.HasValue)
            {
                invoLineIds.Add(currentInvoLineId.Value);
            }

            var linkedInvoLineIds = await _unitOfWork.Repository<InvoJob>().Query()
                .Where(x => x.JobID == jobId && x.Deleted == 0)
                .Select(x => x.InvoLineID)
                .Distinct()
                .ToListAsync();

            foreach (var lineId in linkedInvoLineIds)
            {
                invoLineIds.Add(lineId);
            }

            if (invoLineIds.Count == 0)
            {
                return (null, null, null, false);
            }

            var invoiceInfo = await (
                from line in _unitOfWork.Repository<InvoLine>().Query()
                where invoLineIds.Contains(line.Id) && line.Deleted == 0
                join invoice in _unitOfWork.Repository<Invoice>().Query()
                    on line.InvoiceID equals invoice.Id
                orderby invoice.IssueDate descending, invoice.Id descending
                select new
                {
                    InvoLineId = (decimal?)line.Id,
                    InvoiceReference = (int?)invoice.Reference,
                    InvoiceName = invoice.Name
                })
                .FirstOrDefaultAsync();

            if (invoiceInfo == null)
            {
                return (invoLineIds.First(), null, null, true);
            }

            return (invoiceInfo.InvoLineId, invoiceInfo.InvoiceReference, invoiceInfo.InvoiceName, true);
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
