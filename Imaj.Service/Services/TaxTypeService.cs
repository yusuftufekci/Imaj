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
    public class TaxTypeService : ITaxTypeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentPermissionContext _currentPermissionContext;
        private readonly ILogger<TaxTypeService> _logger;

        public TaxTypeService(
            IUnitOfWork unitOfWork,
            ICurrentPermissionContext currentPermissionContext,
            ILogger<TaxTypeService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentPermissionContext = currentPermissionContext;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResultDto<TaxTypeListItemDto>>> GetTaxTypesAsync(TaxTypeFilterDto filter)
        {
            var normalizedFilter = filter ?? new TaxTypeFilterDto();
            var page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            var pageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<PagedResultDto<TaxTypeListItemDto>>.Success(EmptyPage<TaxTypeListItemDto>(page, pageSize));
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var taxTypeQuery = ApplyTaxTypeScope(_unitOfWork.Repository<TaxType>().Query(), snapshot!);

            if (!string.IsNullOrWhiteSpace(normalizedFilter.Code))
            {
                var code = normalizedFilter.Code.Trim().ToUpperInvariant();
                taxTypeQuery = taxTypeQuery.Where(x => x.Code.Contains(code));
            }

            if (normalizedFilter.IsInvalid.HasValue)
            {
                taxTypeQuery = taxTypeQuery.Where(x => x.Invisible == normalizedFilter.IsInvalid.Value);
            }

            var query =
                from taxType in taxTypeQuery
                join preferredName in _unitOfWork.Repository<XTaxType>().Query().Where(x => x.LanguageID == languageId)
                    on taxType.Id equals preferredName.TaxTypeID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XTaxType>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on taxType.Id equals fallbackName.TaxTypeID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                select new TaxTypeListItemDto
                {
                    Id = taxType.Id,
                    Code = taxType.Code,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    TaxPercentage = taxType.TaxPercentage,
                    Invisible = taxType.Invisible
                };

            var first = normalizedFilter.First.HasValue && normalizedFilter.First.Value > 0 ? normalizedFilter.First.Value : (int?)null;
            IQueryable<TaxTypeListItemDto> scopedQuery = query
                .OrderBy(x => x.Code)
                .ThenBy(x => x.Id);

            if (first.HasValue)
            {
                scopedQuery = scopedQuery.Take(first.Value);
            }

            var totalCount = await scopedQuery.CountAsync();
            var items = await scopedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                item.Name = item.Code;
            }

            return ServiceResult<PagedResultDto<TaxTypeListItemDto>>.Success(new PagedResultDto<TaxTypeListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult<TaxTypeDetailDto>> GetTaxTypeDetailAsync(decimal id)
        {
            if (id <= 0)
            {
                return ServiceResult<TaxTypeDetailDto>.Fail("Vergi tipi ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<TaxTypeDetailDto>.NotFound("Vergi tipi bulunamadi.");
            }

            var row = await ApplyTaxTypeScope(_unitOfWork.Repository<TaxType>().Query(), snapshot!)
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.CompanyID,
                    x.Code,
                    x.TaxPercentage,
                    x.Invisible
                })
                .SingleOrDefaultAsync();

            if (row == null)
            {
                return ServiceResult<TaxTypeDetailDto>.NotFound("Vergi tipi bulunamadi.");
            }

            var names = await (
                from localizedName in _unitOfWork.Repository<XTaxType>().Query()
                join language in _unitOfWork.Repository<Language>().Query()
                    on localizedName.LanguageID equals language.Id into languageGroup
                from language in languageGroup.DefaultIfEmpty()
                where localizedName.TaxTypeID == row.Id
                orderby localizedName.LanguageID
                select new TaxTypeLocalizedNameDto
                {
                    LanguageId = localizedName.LanguageID,
                    LanguageName = language != null ? language.Name : string.Empty,
                    Name = localizedName.Name,
                    InvoLinePostfix = localizedName.InvoLinePostfix
                })
                .ToListAsync();

            foreach (var localizedName in names.Where(x => string.IsNullOrWhiteSpace(x.LanguageName)))
            {
                localizedName.LanguageName = localizedName.LanguageId.ToString(CultureInfo.InvariantCulture);
            }

            return ServiceResult<TaxTypeDetailDto>.Success(new TaxTypeDetailDto
            {
                Id = row.Id,
                CompanyId = row.CompanyID,
                Code = row.Code,
                TaxPercentage = row.TaxPercentage,
                Invisible = row.Invisible,
                Names = names
            });
        }

        public async Task<ServiceResult<List<TaxTypeLanguageDto>>> GetLanguagesAsync()
        {
            var items = await _unitOfWork.Repository<Language>()
                .Query()
                .OrderBy(x => x.Sequence)
                .ThenBy(x => x.Id)
                .Select(x => new TaxTypeLanguageDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            return ServiceResult<List<TaxTypeLanguageDto>>.Success(items);
        }

        public async Task<ServiceResult> CreateTaxTypeAsync(TaxTypeUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Vergi tipi bilgisi bos olamaz.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Vergi tipi kaydi icin yetki kapsami bulunamadi.");
            }

            var targetCompanyId = ResolveTargetCompanyId(snapshot!);
            if (!targetCompanyId.HasValue)
            {
                return ServiceResult.Fail("Company kapsam bilgisi olmadigi icin vergi tipi olusturulamadi.");
            }

            var normalizedCode = NormalizeCode(input.Code);
            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                return ServiceResult.Fail("Kod zorunludur.");
            }

            if (normalizedCode.Length > 8)
            {
                return ServiceResult.Fail("Kod en fazla 8 karakter olabilir.");
            }

            if (input.TaxPercentage < 0 || input.TaxPercentage > 100)
            {
                return ServiceResult.Fail("Vergi orani 0 ile 100 arasinda olmalidir.");
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

            var duplicateExists = await ApplyTaxTypeScope(_unitOfWork.Repository<TaxType>().Query(), snapshot!)
                .AnyAsync(x => x.Code == normalizedCode);
            if (duplicateExists)
            {
                return ServiceResult.Fail("Ayni kod ile baska bir vergi tipi kaydi mevcut.");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var taxTypeRepo = _unitOfWork.Repository<TaxType>();
                var xTaxTypeRepo = _unitOfWork.Repository<XTaxType>();

                var nextTaxTypeId = (await taxTypeRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                await taxTypeRepo.AddAsync(new TaxType
                {
                    Id = nextTaxTypeId,
                    CompanyID = targetCompanyId.Value,
                    Code = normalizedCode,
                    TaxPercentage = input.TaxPercentage,
                    Invisible = input.Invisible,
                    SelectFlag = false,
                    SelectQty = 0,
                    Stamp = 1
                });

                var nextXTaxTypeId = (await xTaxTypeRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xTaxTypeRepo.AddAsync(new XTaxType
                    {
                        Id = nextXTaxTypeId++,
                        TaxTypeID = nextTaxTypeId,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        InvoLinePostfix = localizedName.InvoLinePostfix,
                        Stamp = 1
                    });
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Vergi tipi kaydedildi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Vergi tipi kaydedilirken hata olustu.");
                return ServiceResult.Fail("Vergi tipi kaydedilemedi.");
            }
        }

        public async Task<ServiceResult> UpdateTaxTypeAsync(TaxTypeUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Vergi tipi bilgisi bos olamaz.");
            }

            if (!input.Id.HasValue || input.Id.Value <= 0)
            {
                return ServiceResult.Fail("Vergi tipi ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Vergi tipi guncelleme icin yetki kapsami bulunamadi.");
            }

            var normalizedCode = NormalizeCode(input.Code);
            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                return ServiceResult.Fail("Kod zorunludur.");
            }

            if (normalizedCode.Length > 8)
            {
                return ServiceResult.Fail("Kod en fazla 8 karakter olabilir.");
            }

            if (input.TaxPercentage < 0 || input.TaxPercentage > 100)
            {
                return ServiceResult.Fail("Vergi orani 0 ile 100 arasinda olmalidir.");
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

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var taxTypeRepo = _unitOfWork.Repository<TaxType>();
                var xTaxTypeRepo = _unitOfWork.Repository<XTaxType>();

                var taxType = await ApplyTaxTypeScope(taxTypeRepo.Query(), snapshot!)
                    .SingleOrDefaultAsync(x => x.Id == input.Id.Value);
                if (taxType == null)
                {
                    return ServiceResult.NotFound("Vergi tipi bulunamadi.");
                }

                var duplicateExists = await ApplyTaxTypeScope(taxTypeRepo.Query(), snapshot!)
                    .AnyAsync(x => x.Id != taxType.Id && x.Code == normalizedCode);
                if (duplicateExists)
                {
                    return ServiceResult.Fail("Ayni kod ile baska bir vergi tipi kaydi mevcut.");
                }

                taxType.Code = normalizedCode;
                taxType.TaxPercentage = input.TaxPercentage;
                taxType.Invisible = input.Invisible;
                taxType.Stamp = 1;
                taxTypeRepo.Update(taxType);

                var existingNames = await xTaxTypeRepo.Query()
                    .Where(x => x.TaxTypeID == taxType.Id)
                    .ToListAsync();

                foreach (var existingName in existingNames)
                {
                    xTaxTypeRepo.Remove(existingName);
                }

                var nextXTaxTypeId = (await xTaxTypeRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xTaxTypeRepo.AddAsync(new XTaxType
                    {
                        Id = nextXTaxTypeId++,
                        TaxTypeID = taxType.Id,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        InvoLinePostfix = localizedName.InvoLinePostfix,
                        Stamp = 1
                    });
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Vergi tipi guncellendi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Vergi tipi guncellenirken hata olustu. TaxTypeID={TaxTypeId}", input.Id.Value);
                return ServiceResult.Fail("Vergi tipi guncellenemedi.");
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

        private static List<TaxTypeLocalizedNameInputDto> NormalizeLocalizedNames(IEnumerable<TaxTypeLocalizedNameInputDto>? input)
        {
            return input?
                .Select(x => new TaxTypeLocalizedNameInputDto
                {
                    LanguageId = x.LanguageId,
                    Name = (x.Name ?? string.Empty).Trim(),
                    InvoLinePostfix = (x.InvoLinePostfix ?? string.Empty).Trim()
                })
                .Where(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.LanguageId)
                .Select(x => x.First())
                .OrderBy(x => x.LanguageId)
                .ToList()
                ?? new List<TaxTypeLocalizedNameInputDto>();
        }

        private static string NormalizeCode(string? code)
        {
            return (code ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static decimal? ResolveTargetCompanyId(PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                return snapshot.CompanyId;
            }

            return snapshot.CompanyId;
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
