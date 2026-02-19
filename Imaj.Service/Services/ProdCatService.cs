using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Service.DTOs;
using Imaj.Service.DTOs.Security;
using Imaj.Service.Interfaces;
using Imaj.Service.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imaj.Service.Services
{
    public class ProdCatService : IProdCatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentPermissionContext _currentPermissionContext;
        private readonly ILogger<ProdCatService> _logger;

        public ProdCatService(
            IUnitOfWork unitOfWork,
            ICurrentPermissionContext currentPermissionContext,
            ILogger<ProdCatService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentPermissionContext = currentPermissionContext;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResultDto<ProdCatListItemDto>>> GetProdCatsAsync(ProdCatFilterDto filter)
        {
            var normalizedFilter = filter ?? new ProdCatFilterDto();
            var page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            var pageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<PagedResultDto<ProdCatListItemDto>>.Success(EmptyPage<ProdCatListItemDto>(page, pageSize));
            }

            var preferredLanguageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var prodCatQuery = ApplyProdCatScope(_unitOfWork.Repository<ProdCat>().Query(), snapshot!);
            var taxTypeQuery = ApplyTaxTypeScope(_unitOfWork.Repository<TaxType>().Query(), snapshot!);

            var query =
                from prodCat in prodCatQuery
                join preferredCatName in _unitOfWork.Repository<XProdCat>().Query().Where(x => x.LanguageID == preferredLanguageId)
                    on prodCat.Id equals preferredCatName.ProdCatID into preferredCatGroup
                from preferredCatName in preferredCatGroup.DefaultIfEmpty()
                join fallbackCatName in _unitOfWork.Repository<XProdCat>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on prodCat.Id equals fallbackCatName.ProdCatID into fallbackCatGroup
                from fallbackCatName in fallbackCatGroup.DefaultIfEmpty()
                join taxType in taxTypeQuery on prodCat.TaxTypeID equals taxType.Id into taxTypeGroup
                from taxType in taxTypeGroup.DefaultIfEmpty()
                join preferredTaxName in _unitOfWork.Repository<XTaxType>().Query().Where(x => x.LanguageID == preferredLanguageId)
                    on prodCat.TaxTypeID equals preferredTaxName.TaxTypeID into preferredTaxGroup
                from preferredTaxName in preferredTaxGroup.DefaultIfEmpty()
                join fallbackTaxName in _unitOfWork.Repository<XTaxType>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on prodCat.TaxTypeID equals fallbackTaxName.TaxTypeID into fallbackTaxGroup
                from fallbackTaxName in fallbackTaxGroup.DefaultIfEmpty()
                select new ProdCatListItemDto
                {
                    Id = prodCat.Id,
                    Name = preferredCatName != null
                        ? preferredCatName.Name
                        : (fallbackCatName != null ? fallbackCatName.Name : string.Empty),
                    TaxCode = taxType != null ? taxType.Code : string.Empty,
                    TaxName = preferredTaxName != null
                        ? preferredTaxName.Name
                        : (fallbackTaxName != null ? fallbackTaxName.Name : string.Empty),
                    Sequence = prodCat.Sequence,
                    Invisible = prodCat.Invisible
                };

            if (normalizedFilter.IsInvalid.HasValue)
            {
                query = query.Where(x => x.Invisible == normalizedFilter.IsInvalid.Value);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(x => x.Sequence)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    item.Name = item.Id.ToString(CultureInfo.InvariantCulture);
                }

                if (string.IsNullOrWhiteSpace(item.TaxName))
                {
                    item.TaxName = item.TaxCode;
                }
            }

            return ServiceResult<PagedResultDto<ProdCatListItemDto>>.Success(new PagedResultDto<ProdCatListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult<ProdCatDetailDto>> GetProdCatDetailAsync(decimal id)
        {
            if (id <= 0)
            {
                return ServiceResult<ProdCatDetailDto>.Fail("Urun kategorisi ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<ProdCatDetailDto>.NotFound("Urun kategorisi bulunamadi.");
            }

            var preferredLanguageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;
            var taxTypeQuery = ApplyTaxTypeScope(_unitOfWork.Repository<TaxType>().Query(), snapshot!);

            var row = await (
                from prodCat in ApplyProdCatScope(_unitOfWork.Repository<ProdCat>().Query(), snapshot!)
                join taxType in taxTypeQuery on prodCat.TaxTypeID equals taxType.Id into taxTypeGroup
                from taxType in taxTypeGroup.DefaultIfEmpty()
                join preferredTaxName in _unitOfWork.Repository<XTaxType>().Query().Where(x => x.LanguageID == preferredLanguageId)
                    on prodCat.TaxTypeID equals preferredTaxName.TaxTypeID into preferredTaxGroup
                from preferredTaxName in preferredTaxGroup.DefaultIfEmpty()
                join fallbackTaxName in _unitOfWork.Repository<XTaxType>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on prodCat.TaxTypeID equals fallbackTaxName.TaxTypeID into fallbackTaxGroup
                from fallbackTaxName in fallbackTaxGroup.DefaultIfEmpty()
                where prodCat.Id == id
                select new
                {
                    prodCat.Id,
                    prodCat.CompanyID,
                    prodCat.TaxTypeID,
                    TaxCode = taxType != null ? taxType.Code : string.Empty,
                    TaxName = preferredTaxName != null
                        ? preferredTaxName.Name
                        : (fallbackTaxName != null ? fallbackTaxName.Name : string.Empty),
                    prodCat.Sequence,
                    prodCat.Invisible
                })
                .SingleOrDefaultAsync();

            if (row == null)
            {
                return ServiceResult<ProdCatDetailDto>.NotFound("Urun kategorisi bulunamadi.");
            }

            var names = await (
                from localizedName in _unitOfWork.Repository<XProdCat>().Query()
                join language in _unitOfWork.Repository<Language>().Query()
                    on localizedName.LanguageID equals language.Id into languageGroup
                from language in languageGroup.DefaultIfEmpty()
                where localizedName.ProdCatID == row.Id
                orderby localizedName.LanguageID
                select new ProdCatLocalizedNameDto
                {
                    LanguageId = localizedName.LanguageID,
                    LanguageName = language != null ? language.Name : string.Empty,
                    Name = localizedName.Name
                })
                .ToListAsync();

            foreach (var localizedName in names.Where(x => string.IsNullOrWhiteSpace(x.LanguageName)))
            {
                localizedName.LanguageName = localizedName.LanguageId.ToString(CultureInfo.InvariantCulture);
            }

            return ServiceResult<ProdCatDetailDto>.Success(new ProdCatDetailDto
            {
                Id = row.Id,
                CompanyId = row.CompanyID,
                TaxTypeId = row.TaxTypeID,
                TaxCode = row.TaxCode,
                TaxName = string.IsNullOrWhiteSpace(row.TaxName) ? row.TaxCode : row.TaxName,
                Sequence = row.Sequence,
                Invisible = row.Invisible,
                Names = names
            });
        }

        public async Task<ServiceResult<List<ProdCatLanguageDto>>> GetLanguagesAsync()
        {
            var items = await _unitOfWork.Repository<Language>()
                .Query()
                .OrderBy(x => x.Sequence)
                .ThenBy(x => x.Id)
                .Select(x => new ProdCatLanguageDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            return ServiceResult<List<ProdCatLanguageDto>>.Success(items);
        }

        public async Task<ServiceResult<List<ProdCatTaxTypeOptionDto>>> GetTaxTypeOptionsAsync()
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<List<ProdCatTaxTypeOptionDto>>.Success(new List<ProdCatTaxTypeOptionDto>());
            }

            var preferredLanguageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var options = await (
                from taxType in ApplyTaxTypeScope(_unitOfWork.Repository<TaxType>().Query(), snapshot!)
                join preferredTaxName in _unitOfWork.Repository<XTaxType>().Query().Where(x => x.LanguageID == preferredLanguageId)
                    on taxType.Id equals preferredTaxName.TaxTypeID into preferredTaxGroup
                from preferredTaxName in preferredTaxGroup.DefaultIfEmpty()
                join fallbackTaxName in _unitOfWork.Repository<XTaxType>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on taxType.Id equals fallbackTaxName.TaxTypeID into fallbackTaxGroup
                from fallbackTaxName in fallbackTaxGroup.DefaultIfEmpty()
                orderby taxType.Code, taxType.Id
                select new ProdCatTaxTypeOptionDto
                {
                    Id = taxType.Id,
                    Code = taxType.Code,
                    Name = preferredTaxName != null
                        ? preferredTaxName.Name
                        : (fallbackTaxName != null ? fallbackTaxName.Name : string.Empty)
                })
                .ToListAsync();

            foreach (var option in options.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                option.Name = option.Code;
            }

            return ServiceResult<List<ProdCatTaxTypeOptionDto>>.Success(options);
        }

        public async Task<ServiceResult> CreateProdCatAsync(ProdCatUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Urun kategorisi bilgisi bos olamaz.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Urun kategorisi kaydi icin yetki kapsami bulunamadi.");
            }

            var targetCompanyId = ResolveTargetCompanyId(snapshot!);
            if (!targetCompanyId.HasValue)
            {
                return ServiceResult.Fail("Company kapsam bilgisi olmadigi icin urun kategorisi olusturulamadi.");
            }

            if (input.TaxTypeId <= 0)
            {
                return ServiceResult.Fail("Vergi tipi secimi zorunludur.");
            }

            if (input.Sequence < 0)
            {
                return ServiceResult.Fail("Sira no sifirdan kucuk olamaz.");
            }

            var normalizedNames = NormalizeLocalizedNames(input.Names);
            if (normalizedNames.Count == 0)
            {
                return ServiceResult.Fail("En az bir dilde ad girilmelidir.");
            }

            var languageValidation = await ValidateLanguagesAsync(normalizedNames.Select(x => x.LanguageId));
            if (!languageValidation.IsSuccess)
            {
                return languageValidation;
            }

            var taxTypeExists = await ApplyTaxTypeScope(_unitOfWork.Repository<TaxType>().Query(), snapshot!)
                .AnyAsync(x => x.Id == input.TaxTypeId);
            if (!taxTypeExists)
            {
                return ServiceResult.Fail("Secilen vergi tipi gecersiz veya kapsam disi.");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var prodCatRepo = _unitOfWork.Repository<ProdCat>();
                var xProdCatRepo = _unitOfWork.Repository<XProdCat>();

                var nextProdCatId = (await prodCatRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                await prodCatRepo.AddAsync(new ProdCat
                {
                    Id = nextProdCatId,
                    CompanyID = targetCompanyId.Value,
                    TaxTypeID = input.TaxTypeId,
                    Sequence = input.Sequence,
                    Invisible = input.Invisible,
                    SelectFlag = false,
                    Stamp = 1
                });

                var nextXProdCatId = (await xProdCatRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xProdCatRepo.AddAsync(new XProdCat
                    {
                        Id = nextXProdCatId++,
                        ProdCatID = nextProdCatId,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        Stamp = 1
                    });
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Urun kategorisi kaydedildi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Urun kategorisi kaydedilirken hata olustu.");
                return ServiceResult.Fail("Urun kategorisi kaydedilemedi.");
            }
        }

        public async Task<ServiceResult> UpdateProdCatAsync(ProdCatUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Urun kategorisi bilgisi bos olamaz.");
            }

            if (!input.Id.HasValue || input.Id.Value <= 0)
            {
                return ServiceResult.Fail("Urun kategorisi ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Urun kategorisi guncelleme icin yetki kapsami bulunamadi.");
            }

            if (input.TaxTypeId <= 0)
            {
                return ServiceResult.Fail("Vergi tipi secimi zorunludur.");
            }

            if (input.Sequence < 0)
            {
                return ServiceResult.Fail("Sira no sifirdan kucuk olamaz.");
            }

            var normalizedNames = NormalizeLocalizedNames(input.Names);
            if (normalizedNames.Count == 0)
            {
                return ServiceResult.Fail("En az bir dilde ad girilmelidir.");
            }

            var languageValidation = await ValidateLanguagesAsync(normalizedNames.Select(x => x.LanguageId));
            if (!languageValidation.IsSuccess)
            {
                return languageValidation;
            }

            var taxTypeExists = await ApplyTaxTypeScope(_unitOfWork.Repository<TaxType>().Query(), snapshot!)
                .AnyAsync(x => x.Id == input.TaxTypeId);
            if (!taxTypeExists)
            {
                return ServiceResult.Fail("Secilen vergi tipi gecersiz veya kapsam disi.");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var prodCatRepo = _unitOfWork.Repository<ProdCat>();
                var xProdCatRepo = _unitOfWork.Repository<XProdCat>();

                var prodCat = await ApplyProdCatScope(prodCatRepo.Query(), snapshot!)
                    .SingleOrDefaultAsync(x => x.Id == input.Id.Value);
                if (prodCat == null)
                {
                    return ServiceResult.NotFound("Urun kategorisi bulunamadi.");
                }

                prodCat.TaxTypeID = input.TaxTypeId;
                prodCat.Sequence = input.Sequence;
                prodCat.Invisible = input.Invisible;
                prodCat.Stamp = 1;
                prodCatRepo.Update(prodCat);

                var existingNames = await xProdCatRepo.Query()
                    .Where(x => x.ProdCatID == prodCat.Id)
                    .ToListAsync();

                foreach (var existingName in existingNames)
                {
                    xProdCatRepo.Remove(existingName);
                }

                var nextXProdCatId = (await xProdCatRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xProdCatRepo.AddAsync(new XProdCat
                    {
                        Id = nextXProdCatId++,
                        ProdCatID = prodCat.Id,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        Stamp = 1
                    });
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Urun kategorisi guncellendi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Urun kategorisi guncellenirken hata olustu. ProdCatID={ProdCatId}", input.Id.Value);
                return ServiceResult.Fail("Urun kategorisi guncellenemedi.");
            }
        }

        private async Task<ServiceResult> ValidateLanguagesAsync(IEnumerable<decimal> languageIds)
        {
            var normalizedLanguageIds = languageIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (normalizedLanguageIds.Count == 0)
            {
                return ServiceResult.Fail("En az bir gecerli dil secilmelidir.");
            }

            var existingLanguageIds = await _unitOfWork.Repository<Language>()
                .Query()
                .Where(x => normalizedLanguageIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            if (existingLanguageIds.Count != normalizedLanguageIds.Count)
            {
                return ServiceResult.Fail("Secilen dillerden en az biri gecersiz.");
            }

            return ServiceResult.Success();
        }

        private static List<ProdCatLocalizedNameInputDto> NormalizeLocalizedNames(IEnumerable<ProdCatLocalizedNameInputDto>? input)
        {
            return input?
                .Select(x => new ProdCatLocalizedNameInputDto
                {
                    LanguageId = x.LanguageId,
                    Name = (x.Name ?? string.Empty).Trim()
                })
                .Where(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.LanguageId)
                .Select(x => x.First())
                .OrderBy(x => x.LanguageId)
                .ToList()
                ?? new List<ProdCatLocalizedNameInputDto>();
        }

        private static decimal? ResolveTargetCompanyId(PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                return snapshot.CompanyId;
            }

            return snapshot.CompanyId;
        }

        private static IQueryable<ProdCat> ApplyProdCatScope(IQueryable<ProdCat> query, PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                if (!snapshot.CompanyId.HasValue)
                {
                    return query.Where(_ => false);
                }

                query = query.Where(x => x.CompanyID == snapshot.CompanyId.Value);
            }

            return query;
        }

        private static IQueryable<TaxType> ApplyTaxTypeScope(IQueryable<TaxType> query, PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                if (!snapshot.CompanyId.HasValue)
                {
                    return query.Where(_ => false);
                }

                query = query.Where(x => x.CompanyID == snapshot.CompanyId.Value);
            }

            return query;
        }

        private static bool IsScopeDenied(PermissionSnapshotDto? snapshot)
        {
            return snapshot == null
                   || snapshot.IsDenied
                   || snapshot.CompanyScopeMode == CompanyScopeMode.Deny;
        }

        private static PagedResultDto<T> EmptyPage<T>(int page, int pageSize)
        {
            return new PagedResultDto<T>
            {
                Items = new List<T>(),
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            };
        }

        private static decimal ResolveUiLanguageId()
        {
            var culture = CultureInfo.CurrentUICulture.Name;
            return culture.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? 2m : 1m;
        }
    }
}
