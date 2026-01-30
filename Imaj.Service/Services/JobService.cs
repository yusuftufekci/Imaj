using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Service.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imaj.Service.Services
{
    /// <summary>
    /// İş (Job) işlemleri için business service.
    /// Dropdown verilerini veritabanından sağlar.
    /// </summary>
    public class JobService : IJobService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<JobService> _logger;

        public JobService(IUnitOfWork unitOfWork, ILogger<JobService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// State (Durum) listesini veritabanından getirir.
        /// State tablosunda Category='Job' olan kayıtların XState'teki Türkçe isimlerini çeker.
        /// </summary>
        public async Task<ServiceResult<List<StateDto>>> GetStatesAsync()
        {
            try
            {
                // State tablosundan Category='Job' olan ID'leri al
                var jobStateIds = await _unitOfWork.Repository<State>()
                    .Query()
                    .Where(s => s.Category == "Job")
                    .Select(s => s.Id)
                    .ToListAsync();

                // Bu ID'lere karşılık gelen XState kayıtlarından Türkçe isimleri al
                var states = await _unitOfWork.Repository<XState>()
                    .Query()
                    .Where(x => x.LanguageID == 1 && jobStateIds.Contains(x.StateID))
                    .OrderBy(x => x.Name)
                    .Select(x => new StateDto
                    {
                        Id = x.StateID,
                        Name = x.Name
                    })
                    .ToListAsync();

                return ServiceResult<List<StateDto>>.Success(states);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "State listesi alınırken hata oluştu");
                return ServiceResult<List<StateDto>>.Fail("Durum listesi yüklenirken hata oluştu.");
            }
        }

        /// <summary>
        /// Fonksiyon listesini veritabanından getirir.
        /// XFunction tablosundan LanguageID=1 olanları çeker.
        /// </summary>
        public async Task<ServiceResult<List<FunctionDto>>> GetFunctionsAsync()
        {
            try
            {
                var functions = await _unitOfWork.Repository<XFunction>()
                    .Query()
                    .Where(x => x.LanguageID == 1)
                    .OrderBy(x => x.Name)
                    .Select(x => new FunctionDto
                    {
                        Id = x.FunctionID,
                        Name = x.Name
                    })
                    .ToListAsync();

                return ServiceResult<List<FunctionDto>>.Success(functions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fonksiyon listesi alınırken hata oluştu");
                return ServiceResult<List<FunctionDto>>.Fail("Fonksiyon listesi yüklenirken hata oluştu.");
            }
        }

        /// <summary>
        /// Görev Tipi (WorkType) listesini veritabanından getirir.
        /// XWorkType tablosundan LanguageID=1 olanları çeker.
        /// </summary>
        public async Task<ServiceResult<List<WorkTypeDto>>> GetWorkTypesAsync()
        {
            try
            {
                var workTypes = await _unitOfWork.Repository<XWorkType>()
                    .Query()
                    .Where(x => x.LanguageID == 1)
                    .OrderBy(x => x.Name)
                    .Select(x => new WorkTypeDto
                    {
                        Id = x.WorkTypeID,
                        Name = x.Name
                    })
                    .ToListAsync();

                return ServiceResult<List<WorkTypeDto>>.Success(workTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WorkType listesi alınırken hata oluştu");
                return ServiceResult<List<WorkTypeDto>>.Fail("Görev tipi listesi yüklenirken hata oluştu.");
            }
        }

        /// <summary>
        /// Mesai Tipi (TimeType) listesini veritabanından getirir.
        /// XTimeType tablosundan LanguageID=1 olanları çeker.
        /// </summary>
        public async Task<ServiceResult<List<TimeTypeDto>>> GetTimeTypesAsync()
        {
            try
            {
                var timeTypes = await _unitOfWork.Repository<XTimeType>()
                    .Query()
                    .Where(x => x.LanguageID == 1)
                    .OrderBy(x => x.Name)
                    .Select(x => new TimeTypeDto
                    {
                        Id = x.TimeTypeID,
                        Name = x.Name
                    })
                    .ToListAsync();

                return ServiceResult<List<TimeTypeDto>>.Success(timeTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TimeType listesi alınırken hata oluştu");
                return ServiceResult<List<TimeTypeDto>>.Fail("Mesai tipi listesi yüklenirken hata oluştu.");
            }
        }

        /// <summary>
        /// İş kayıtlarını filtreleyerek getirir.
        /// Customer, Function, State tabloları ile join yaparak isim bilgilerini alır.
        /// </summary>
        public async Task<ServiceResult<PagedResult<JobDto>>> GetByFilterAsync(JobFilterDto filter)
        {
            try
            {
                // Ana sorgu - Job'lardan başla
                var query = from j in _unitOfWork.Repository<Job>().Query()
                            join c in _unitOfWork.Repository<Customer>().Query() on j.CustomerID equals c.Id into cGroup
                            from customer in cGroup.DefaultIfEmpty()
                            join xf in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == 1) 
                                on j.FunctionID equals xf.FunctionID into fGroup
                            from xFunc in fGroup.DefaultIfEmpty()
                            join xs in _unitOfWork.Repository<XState>().Query().Where(x => x.LanguageID == 1) 
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

                // Toplam kayıt sayısı
                var totalCount = await query.CountAsync();

                // Sayfalama ve sıralama
                var pageSize = filter.PageSize > 0 ? filter.PageSize : 20;
                var skip = (filter.Page - 1) * pageSize;

                var items = await query
                    .OrderByDescending(x => x.Job.StartDT)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(x => new JobDto
                    {
                        Id = x.Job.Id,
                        Reference = x.Job.Reference,
                        FunctionId = x.Job.FunctionID,
                        FunctionName = x.FunctionName,
                        CustomerId = x.Job.CustomerID,
                        CustomerName = x.CustomerName,
                        Name = x.Job.Name,
                        Contact = x.Job.Contact,
                        StartDate = x.Job.StartDT,
                        EndDate = x.Job.EndDT,
                        StateId = x.Job.StateID,
                        StatusName = x.StateName,
                        IsEmailSent = x.Job.Mailed,
                        IsEvaluated = x.Job.Evaluated,
                        InvoLineId = x.Job.InvoLineID
                    })
                    .ToListAsync();

                var result = new PagedResult<JobDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = filter.Page,
                    PageSize = pageSize
                };

                return ServiceResult<PagedResult<JobDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş listesi alınırken hata oluştu");
                // Hata detayını dönüyoruz ki görebilelim (Development ortamı için uygun)
                return ServiceResult<PagedResult<JobDto>>.Fail($"Hata: {ex.Message} Inner: {ex.InnerException?.Message}");
            }
        }
    }
}
