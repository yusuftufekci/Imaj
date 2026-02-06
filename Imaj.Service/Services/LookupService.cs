using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Service.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Imaj.Service.Services
{
    /// <summary>
    /// Ortak dropdown ve lookup verileri için service implementasyonu.
    /// Tüm referans tablo verilerini tek noktadan yönetir.
    /// </summary>
    public class LookupService : BaseService, ILookupService
    {
        public LookupService(
            IUnitOfWork unitOfWork, 
            ILogger<LookupService> logger, 
            IConfiguration configuration)
            : base(unitOfWork, logger, configuration)
        {
        }

        /// <summary>
        /// Belirtilen kategoriye göre State listesini getirir.
        /// State tablosunda Category filtresi ile XState'teki çeviri isimlerini çeker.
        /// </summary>
        public async Task<ServiceResult<List<StateDto>>> GetStatesAsync(string category)
        {
            try
            {
                // Önce State tablosundan belirtilen kategorideki ID'leri al
                var stateIds = await _unitOfWork.Repository<State>()
                    .Query()
                    .Where(s => s.Category == category)
                    .Select(s => s.Id)
                    .ToListAsync();

                // Sonra bu ID'lere karşılık gelen XState kayıtlarından çeviri isimlerini al
                var states = await _unitOfWork.Repository<XState>()
                    .Query()
                    .Where(x => x.LanguageID == CurrentLanguageId && stateIds.Contains(x.StateID))
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
                _logger.LogError(ex, "State listesi alınırken hata oluştu. Kategori: {Category}", category);
                return ServiceResult<List<StateDto>>.Fail("Durum listesi yüklenirken hata oluştu.");
            }
        }

        /// <summary>
        /// Fonksiyon listesini getirir.
        /// XFunction ve Function tablolarını birleştirerek çeviri isimlerini çeker.
        /// </summary>
        public async Task<ServiceResult<List<FunctionDto>>> GetFunctionsAsync()
        {
            return await GetTranslatedListAsync<Function, XFunction, FunctionDto>(
                f => f.Id,
                xf => xf.FunctionID,
                (f, xf) => new FunctionDto { Id = f.Id, Name = xf.Name }
            );
        }

        /// <summary>
        /// Görev Tipi (WorkType) listesini getirir.
        /// XWorkType ve WorkType tablolarını birleştirerek çeviri isimlerini çeker.
        /// </summary>
        public async Task<ServiceResult<List<WorkTypeDto>>> GetWorkTypesAsync()
        {
            return await GetTranslatedListAsync<WorkType, XWorkType, WorkTypeDto>(
                wt => wt.Id,
                xwt => xwt.WorkTypeID,
                (wt, xwt) => new WorkTypeDto { Id = wt.Id, Name = xwt.Name }
            );
        }

        /// <summary>
        /// Mesai Tipi (TimeType) listesini getirir.
        /// XTimeType ve TimeType tablolarını birleştirerek çeviri isimlerini çeker.
        /// </summary>
        public async Task<ServiceResult<List<TimeTypeDto>>> GetTimeTypesAsync()
        {
            return await GetTranslatedListAsync<TimeType, XTimeType, TimeTypeDto>(
                tt => tt.Id,
                xtt => xtt.TimeTypeID,
                (tt, xtt) => new TimeTypeDto { Id = tt.Id, Name = xtt.Name }
            );
        }

        /// <summary>
        /// Ürün Kategorilerini getirir.
        /// ProdCat -> XProdCat (Ad) -> TaxType (Kod) -> XTaxType (Vergi Adı) join yapar.
        /// </summary>
        public async Task<ServiceResult<List<ProductCategoryDto>>> GetProductCategoriesAsync()
        {
            try
            {
                // LINQ Query Syntax ile join işlemi
                var query = from pc in _unitOfWork.Repository<ProdCat>().Query()
                            join xpc in _unitOfWork.Repository<XProdCat>().Query() 
                                on pc.Id equals xpc.ProdCatID
                            join tt in _unitOfWork.Repository<TaxType>().Query() 
                                on pc.TaxTypeID equals tt.Id
                            join xtt in _unitOfWork.Repository<XTaxType>().Query() 
                                on tt.Id equals xtt.TaxTypeID
                            where pc.TaxTypeID == 6 
                                  && xpc.LanguageID == CurrentLanguageId
                                  && xtt.LanguageID == CurrentLanguageId
                                  && pc.Invisible == false
                            orderby pc.Sequence
                            select new ProductCategoryDto
                            {
                                Id = pc.Id,
                                Name = xpc.Name,
                                TaxTypeId = pc.TaxTypeID,
                                TaxCode = tt.Code,
                                TaxName = xtt.Name,
                                Sequence = pc.Sequence
                            };

                var categories = await query.ToListAsync();

                return ServiceResult<List<ProductCategoryDto>>.Success(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün kategorileri yüklenirken hata oluştu");
                return ServiceResult<List<ProductCategoryDto>>.Fail("Ürün kategorileri yüklenirken hata oluştu.");
            }
        }
    }
}
