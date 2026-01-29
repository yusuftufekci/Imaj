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
    }
}
