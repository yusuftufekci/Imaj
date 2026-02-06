using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Service.DTOs;
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
        public JobService(IUnitOfWork unitOfWork, ILogger<JobService> logger, IConfiguration configuration)
            : base(unitOfWork, logger, configuration)
        {
        }

        /// <summary>
        /// İş kayıtlarını filtreleyerek getirir.
        /// Customer, Function, State tabloları ile join yaparak isim bilgilerini alır.
        /// </summary>
        public async Task<ServiceResult<PagedResult<JobDto>>> GetByFilterAsync(JobFilterDto filter)
        {
            // ... (existing code) ...
            try
            {
                // ... (implementation of GetByFilterAsync) ...
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

                // Ürün filtresi - belirli bir ürünü içeren Job'ları getir
                if (filter.ProductId.HasValue)
                {
                    _logger.LogInformation("Ürün filtresi uygulanıyor. ProductId: {ProductId}", filter.ProductId.Value);
                    
                    var jobProdRepo = _unitOfWork.Repository<JobProd>();
                    var jobIdsWithProduct = jobProdRepo.Query()
                        .Where(jp => jp.ProductID == filter.ProductId.Value && jp.Deleted == 0)
                        .Select(jp => jp.JobID);
                    
                    var matchingJobIds = await jobIdsWithProduct.ToListAsync();
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
                    
                    // Subquery: Bu çalışanın mesai kaydı olan Job ID'leri
                    var jobIdsWithEmployee = from jw in jobWorkRepo.Query()
                                              join e in employeeRepo.Query() on jw.EmployeeID equals e.Id
                                              where e.Code == filter.EmployeeCode && jw.Deleted == 0
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
                return ServiceResult<PagedResult<JobDto>>.Fail($"Hata: {ex.Message} Inner: {ex.InnerException?.Message}");
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
                // 1. Ana İş Bilgisi
                var jobQuery = from j in _unitOfWork.Repository<Job>().Query()
                            where j.Reference == reference // Referansa göre filtrele
                            join c in _unitOfWork.Repository<Customer>().Query() on j.CustomerID equals c.Id into cGroup
                            from customer in cGroup.DefaultIfEmpty()
                            join xf in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == 1) 
                                on j.FunctionID equals xf.FunctionID into fGroup
                            from xFunc in fGroup.DefaultIfEmpty()
                            join xs in _unitOfWork.Repository<XState>().Query().Where(x => x.LanguageID == 1) 
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
                                join e in _unitOfWork.Repository<Employee>().Query() on jw.EmployeeID equals e.Id
                                join xw in _unitOfWork.Repository<XWorkType>().Query().Where(x => x.LanguageID == 1) 
                                    on jw.WorkTypeID equals xw.WorkTypeID into wGroup
                                from xWork in wGroup.DefaultIfEmpty()
                                join xt in _unitOfWork.Repository<XTimeType>().Query().Where(x => x.LanguageID == 1) 
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
                                where jp.JobID == jobDto.Id
                                join p in _unitOfWork.Repository<Product>().Query() on jp.ProductID equals p.Id
                                join xp in _unitOfWork.Repository<XProduct>().Query().Where(x => x.LanguageID == 1) 
                                    on jp.ProductID equals xp.ProductID into xpGroup
                                from xProduct in xpGroup.DefaultIfEmpty()
                                join xpc in _unitOfWork.Repository<XProdCat>().Query().Where(x => x.LanguageID == 1) 
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

                return ServiceResult<JobDto>.Success(jobDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş detayı getirilirken hata oluştu: {Reference}", reference);
                return ServiceResult<JobDto>.Fail("İş detayı yüklenirken bir hata oluştu.");
            }
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
                return ServiceResult<JobDto>.Fail("İş yaratılırken bir hata oluştu: " + ex.Message);
            }
        }
        public async Task<ServiceResult<List<JobLogDto>>> GetJobHistoryAsync(decimal jobId)
        {
            try
            {
                // JobLog tablosundan iş geçmişini getir
                // ActionDT: İşlem tarihi, Destination: E-posta adresi vb. hedef bilgisi
                var query = from log in _unitOfWork.Repository<JobLog>().Query()
                            where log.JobID == jobId
                            join u in _unitOfWork.Repository<User>().Query() on log.UserID equals u.Id
                            join xl in _unitOfWork.Repository<XLogAction>().Query().Where(x => x.LanguageID == 1) // Türkçe
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
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName", connection))
                {
                    command.Parameters.AddWithValue("@tableName", tableName);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            columns.Add(reader.GetString(0));
                        }
                    }
                }
            }
            return columns;
        }
    }
}
