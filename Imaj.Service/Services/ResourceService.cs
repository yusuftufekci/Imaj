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
    public class ResourceService : IResourceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentPermissionContext _currentPermissionContext;
        private readonly ILogger<ResourceService> _logger;

        public ResourceService(
            IUnitOfWork unitOfWork,
            ICurrentPermissionContext currentPermissionContext,
            ILogger<ResourceService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentPermissionContext = currentPermissionContext;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResultDto<ResourceListItemDto>>> GetResourcesAsync(ResourceFilterDto filter)
        {
            var normalizedFilter = filter ?? new ResourceFilterDto();
            var page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            var pageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<PagedResultDto<ResourceListItemDto>>.Success(EmptyPage<ResourceListItemDto>(page, pageSize));
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var resourceQuery = ApplyResourceScope(_unitOfWork.Repository<Resource>().Query(), snapshot!);

            if (!string.IsNullOrWhiteSpace(normalizedFilter.Code))
            {
                var code = normalizedFilter.Code.Trim();
                resourceQuery = resourceQuery.Where(x => x.Code.Contains(code));
            }

            if (normalizedFilter.SequenceFrom.HasValue)
            {
                resourceQuery = resourceQuery.Where(x => x.Sequence >= normalizedFilter.SequenceFrom.Value);
            }

            if (normalizedFilter.SequenceTo.HasValue)
            {
                resourceQuery = resourceQuery.Where(x => x.Sequence <= normalizedFilter.SequenceTo.Value);
            }

            if (normalizedFilter.FunctionId.HasValue && normalizedFilter.FunctionId.Value > 0)
            {
                resourceQuery = resourceQuery.Where(x => x.FunctionID == normalizedFilter.FunctionId.Value);
            }

            if (normalizedFilter.ResoCatId.HasValue && normalizedFilter.ResoCatId.Value > 0)
            {
                resourceQuery = resourceQuery.Where(x => x.ResoCatID == normalizedFilter.ResoCatId.Value);
            }

            if (normalizedFilter.IsInvalid.HasValue)
            {
                resourceQuery = resourceQuery.Where(x => x.Invisible == normalizedFilter.IsInvalid.Value);
            }

            var query =
                from resource in resourceQuery
                join preferredName in _unitOfWork.Repository<XResource>().Query().Where(x => x.LanguageID == languageId)
                    on resource.Id equals preferredName.ResourceID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XResource>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on resource.Id equals fallbackName.ResourceID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                join preferredFunction in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == languageId)
                    on resource.FunctionID equals preferredFunction.FunctionID into preferredFunctionGroup
                from preferredFunction in preferredFunctionGroup.DefaultIfEmpty()
                join fallbackFunction in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on resource.FunctionID equals fallbackFunction.FunctionID into fallbackFunctionGroup
                from fallbackFunction in fallbackFunctionGroup.DefaultIfEmpty()
                join preferredResoCat in _unitOfWork.Repository<XResoCat>().Query().Where(x => x.LanguageID == languageId)
                    on resource.ResoCatID equals preferredResoCat.ResoCatID into preferredResoCatGroup
                from preferredResoCat in preferredResoCatGroup.DefaultIfEmpty()
                join fallbackResoCat in _unitOfWork.Repository<XResoCat>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on resource.ResoCatID equals fallbackResoCat.ResoCatID into fallbackResoCatGroup
                from fallbackResoCat in fallbackResoCatGroup.DefaultIfEmpty()
                select new ResourceListItemDto
                {
                    Id = resource.Id,
                    Sequence = resource.Sequence,
                    Code = resource.Code,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    FunctionId = resource.FunctionID,
                    FunctionName = preferredFunction != null
                        ? preferredFunction.Name
                        : (fallbackFunction != null ? fallbackFunction.Name : string.Empty),
                    ResoCatId = resource.ResoCatID,
                    ResoCatName = preferredResoCat != null
                        ? preferredResoCat.Name
                        : (fallbackResoCat != null ? fallbackResoCat.Name : string.Empty),
                    Invisible = resource.Invisible
                };

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(x => x.Sequence)
                .ThenBy(x => x.Code)
                .ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                item.Name = item.Code;
            }

            return ServiceResult<PagedResultDto<ResourceListItemDto>>.Success(new PagedResultDto<ResourceListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult<ResourceDetailDto>> GetResourceDetailAsync(decimal id)
        {
            if (id <= 0)
            {
                return ServiceResult<ResourceDetailDto>.Fail("Kaynak ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<ResourceDetailDto>.NotFound("Kaynak bulunamadi.");
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var row = await ApplyResourceScope(_unitOfWork.Repository<Resource>().Query(), snapshot!)
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.CompanyID,
                    x.FunctionID,
                    x.ResoCatID,
                    x.Sequence,
                    x.Code,
                    x.Invisible
                })
                .SingleOrDefaultAsync();

            if (row == null)
            {
                return ServiceResult<ResourceDetailDto>.NotFound("Kaynak bulunamadi.");
            }

            var functionName = await _unitOfWork.Repository<XFunction>()
                .Query()
                .Where(x => x.FunctionID == row.FunctionID
                            && (x.LanguageID == languageId || x.LanguageID == fallbackLanguageId))
                .OrderBy(x => x.LanguageID == languageId ? 0 : 1)
                .Select(x => x.Name)
                .FirstOrDefaultAsync() ?? string.Empty;

            var resoCatName = await _unitOfWork.Repository<XResoCat>()
                .Query()
                .Where(x => x.ResoCatID == row.ResoCatID
                            && (x.LanguageID == languageId || x.LanguageID == fallbackLanguageId))
                .OrderBy(x => x.LanguageID == languageId ? 0 : 1)
                .Select(x => x.Name)
                .FirstOrDefaultAsync() ?? string.Empty;

            var names = await (
                from localizedName in _unitOfWork.Repository<XResource>().Query()
                join language in _unitOfWork.Repository<Language>().Query()
                    on localizedName.LanguageID equals language.Id into languageGroup
                from language in languageGroup.DefaultIfEmpty()
                where localizedName.ResourceID == row.Id
                orderby localizedName.LanguageID
                select new ResourceLocalizedNameDto
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

            return ServiceResult<ResourceDetailDto>.Success(new ResourceDetailDto
            {
                Id = row.Id,
                CompanyId = row.CompanyID,
                FunctionId = row.FunctionID,
                FunctionName = functionName,
                ResoCatId = row.ResoCatID,
                ResoCatName = resoCatName,
                Sequence = row.Sequence,
                Code = row.Code,
                Invisible = row.Invisible,
                Names = names
            });
        }

        public async Task<ServiceResult<List<ResourceLanguageDto>>> GetLanguagesAsync()
        {
            var items = await _unitOfWork.Repository<Language>()
                .Query()
                .OrderBy(x => x.Sequence)
                .ThenBy(x => x.Id)
                .Select(x => new ResourceLanguageDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            return ServiceResult<List<ResourceLanguageDto>>.Success(items);
        }

        public async Task<ServiceResult<List<ResourceFunctionOptionDto>>> GetFunctionOptionsAsync()
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<List<ResourceFunctionOptionDto>>.Success(new List<ResourceFunctionOptionDto>());
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var functionQuery = ApplyFunctionScope(_unitOfWork.Repository<Function>().Query(), snapshot!)
                .Where(x => !x.Invisible);

            var query =
                from function in functionQuery
                join preferredName in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == languageId)
                    on function.Id equals preferredName.FunctionID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on function.Id equals fallbackName.FunctionID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                select new ResourceFunctionOptionDto
                {
                    Id = function.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty)
                };

            var items = await query
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToListAsync();

            foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                item.Name = item.Id.ToString(CultureInfo.InvariantCulture);
            }

            return ServiceResult<List<ResourceFunctionOptionDto>>.Success(items);
        }

        public async Task<ServiceResult<List<ResourceResoCatOptionDto>>> GetResoCatOptionsAsync()
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<List<ResourceResoCatOptionDto>>.Success(new List<ResourceResoCatOptionDto>());
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var resoCatQuery = ApplyResoCatScope(_unitOfWork.Repository<ResoCat>().Query(), snapshot!)
                .Where(x => !x.Invisible);

            var query =
                from resoCat in resoCatQuery
                join preferredName in _unitOfWork.Repository<XResoCat>().Query().Where(x => x.LanguageID == languageId)
                    on resoCat.Id equals preferredName.ResoCatID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XResoCat>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on resoCat.Id equals fallbackName.ResoCatID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                select new ResourceResoCatOptionDto
                {
                    Id = resoCat.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty)
                };

            var items = await query
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToListAsync();

            foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                item.Name = item.Id.ToString(CultureInfo.InvariantCulture);
            }

            return ServiceResult<List<ResourceResoCatOptionDto>>.Success(items);
        }

        public async Task<ServiceResult> CreateResourceAsync(ResourceUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Kaynak bilgisi bos olamaz.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Kaynak kaydi icin yetki kapsami bulunamadi.");
            }

            var targetCompanyId = ResolveTargetCompanyId(snapshot!);
            if (!targetCompanyId.HasValue)
            {
                return ServiceResult.Fail("Company kapsam bilgisi olmadigi icin kaynak olusturulamadi.");
            }

            var normalizedCode = NormalizeCode(input.Code);
            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                return ServiceResult.Fail("Kaynak kodu zorunludur.");
            }

            var normalizedNames = NormalizeLocalizedNames(input.Names);
            if (normalizedNames.Count == 0)
            {
                return ServiceResult.Fail("En az bir dilde ad girilmelidir.");
            }

            if (input.FunctionId <= 0)
            {
                return ServiceResult.Fail("Fonksiyon secimi zorunludur.");
            }

            if (input.ResoCatId <= 0)
            {
                return ServiceResult.Fail("Kaynak kategorisi secimi zorunludur.");
            }

            if (normalizedCode.Length > 8)
            {
                return ServiceResult.Fail("Kod en fazla 8 karakter olabilir.");
            }

            var languageValidation = await ValidateLanguagesAsync(normalizedNames.Select(x => x.LanguageId));
            if (!languageValidation.IsSuccess)
            {
                return languageValidation;
            }

            var functionValidation = await ValidateFunctionAsync(input.FunctionId, snapshot!);
            if (!functionValidation.IsSuccess)
            {
                return functionValidation;
            }

            var resoCatValidation = await ValidateResoCatAsync(input.ResoCatId, snapshot!);
            if (!resoCatValidation.IsSuccess)
            {
                return resoCatValidation;
            }

            var duplicateExists = await ApplyResourceScope(_unitOfWork.Repository<Resource>().Query(), snapshot!)
                .AnyAsync(x => x.Code == normalizedCode);
            if (duplicateExists)
            {
                return ServiceResult.Fail("Ayni kod ile baska bir kaynak kaydi mevcut.");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var resourceRepo = _unitOfWork.Repository<Resource>();
                var xResourceRepo = _unitOfWork.Repository<XResource>();

                var nextResourceId = (await resourceRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                await resourceRepo.AddAsync(new Resource
                {
                    Id = nextResourceId,
                    CompanyID = targetCompanyId.Value,
                    FunctionID = input.FunctionId,
                    ResoCatID = input.ResoCatId,
                    Sequence = input.Sequence,
                    Code = normalizedCode,
                    Invisible = input.Invisible,
                    SelectFlag = false,
                    Stamp = 1
                });

                var nextXResourceId = (await xResourceRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xResourceRepo.AddAsync(new XResource
                    {
                        Id = nextXResourceId++,
                        ResourceID = nextResourceId,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        Stamp = 1
                    });
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Kaynak kaydedildi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Kaynak kaydedilirken hata olustu.");
                return ServiceResult.Fail("Kaynak kaydedilemedi.");
            }
        }

        public async Task<ServiceResult> UpdateResourceAsync(ResourceUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Kaynak bilgisi bos olamaz.");
            }

            if (!input.Id.HasValue || input.Id.Value <= 0)
            {
                return ServiceResult.Fail("Kaynak ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Kaynak guncelleme icin yetki kapsami bulunamadi.");
            }

            var normalizedCode = NormalizeCode(input.Code);
            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                return ServiceResult.Fail("Kaynak kodu zorunludur.");
            }

            var normalizedNames = NormalizeLocalizedNames(input.Names);
            if (normalizedNames.Count == 0)
            {
                return ServiceResult.Fail("En az bir dilde ad girilmelidir.");
            }

            if (input.FunctionId <= 0)
            {
                return ServiceResult.Fail("Fonksiyon secimi zorunludur.");
            }

            if (input.ResoCatId <= 0)
            {
                return ServiceResult.Fail("Kaynak kategorisi secimi zorunludur.");
            }

            if (normalizedCode.Length > 8)
            {
                return ServiceResult.Fail("Kod en fazla 8 karakter olabilir.");
            }

            var languageValidation = await ValidateLanguagesAsync(normalizedNames.Select(x => x.LanguageId));
            if (!languageValidation.IsSuccess)
            {
                return languageValidation;
            }

            var functionValidation = await ValidateFunctionAsync(input.FunctionId, snapshot!);
            if (!functionValidation.IsSuccess)
            {
                return functionValidation;
            }

            var resoCatValidation = await ValidateResoCatAsync(input.ResoCatId, snapshot!);
            if (!resoCatValidation.IsSuccess)
            {
                return resoCatValidation;
            }

            var duplicateExists = await ApplyResourceScope(_unitOfWork.Repository<Resource>().Query(), snapshot!)
                .AnyAsync(x => x.Id != input.Id.Value && x.Code == normalizedCode);
            if (duplicateExists)
            {
                return ServiceResult.Fail("Ayni kod ile baska bir kaynak kaydi mevcut.");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var resourceRepo = _unitOfWork.Repository<Resource>();
                var xResourceRepo = _unitOfWork.Repository<XResource>();

                var resource = await ApplyResourceScope(resourceRepo.Query(), snapshot!)
                    .SingleOrDefaultAsync(x => x.Id == input.Id.Value);
                if (resource == null)
                {
                    return ServiceResult.NotFound("Kaynak bulunamadi.");
                }

                resource.FunctionID = input.FunctionId;
                resource.ResoCatID = input.ResoCatId;
                resource.Sequence = input.Sequence;
                resource.Code = normalizedCode;
                resource.Invisible = input.Invisible;
                resource.Stamp = 1;
                resourceRepo.Update(resource);

                var existingLocalizedNames = await xResourceRepo.Query()
                    .Where(x => x.ResourceID == resource.Id)
                    .ToListAsync();
                foreach (var existingLocalizedName in existingLocalizedNames)
                {
                    xResourceRepo.Remove(existingLocalizedName);
                }

                var nextXResourceId = (await xResourceRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xResourceRepo.AddAsync(new XResource
                    {
                        Id = nextXResourceId++,
                        ResourceID = resource.Id,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        Stamp = 1
                    });
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Kaynak guncellendi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Kaynak guncellenirken hata olustu. ResourceID={ResourceId}", input.Id.Value);
                return ServiceResult.Fail("Kaynak guncellenemedi.");
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

        private async Task<ServiceResult> ValidateFunctionAsync(decimal functionId, PermissionSnapshotDto snapshot)
        {
            var functionQuery = ApplyFunctionScope(_unitOfWork.Repository<Function>().Query(), snapshot)
                .Where(x => x.Id == functionId);

            var exists = await functionQuery.AnyAsync();
            if (!exists)
            {
                return ServiceResult.Fail("Secilen fonksiyon gecersiz veya kapsam disi.");
            }

            return ServiceResult.Success();
        }

        private async Task<ServiceResult> ValidateResoCatAsync(decimal resoCatId, PermissionSnapshotDto snapshot)
        {
            var resoCatQuery = ApplyResoCatScope(_unitOfWork.Repository<ResoCat>().Query(), snapshot)
                .Where(x => x.Id == resoCatId);

            var exists = await resoCatQuery.AnyAsync();
            if (!exists)
            {
                return ServiceResult.Fail("Secilen kaynak kategorisi gecersiz veya kapsam disi.");
            }

            return ServiceResult.Success();
        }

        private static string NormalizeCode(string? code)
        {
            return (code ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static List<ResourceLocalizedNameInputDto> NormalizeLocalizedNames(IEnumerable<ResourceLocalizedNameInputDto>? input)
        {
            return input?
                .Select(x => new ResourceLocalizedNameInputDto
                {
                    LanguageId = x.LanguageId,
                    Name = (x.Name ?? string.Empty).Trim()
                })
                .Where(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.LanguageId)
                .Select(x => x.First())
                .OrderBy(x => x.LanguageId)
                .ToList()
                ?? new List<ResourceLocalizedNameInputDto>();
        }

        private static decimal? ResolveTargetCompanyId(PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                return snapshot.CompanyId;
            }

            return snapshot.CompanyId;
        }

        private static IQueryable<Resource> ApplyResourceScope(IQueryable<Resource> query, PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                if (!snapshot.CompanyId.HasValue)
                {
                    return query.Where(_ => false);
                }

                query = query.Where(x => x.CompanyID == snapshot.CompanyId.Value);
            }

            return query.Where(x => snapshot.AllowedFunctionIds.Contains(x.FunctionID));
        }

        private static IQueryable<Function> ApplyFunctionScope(IQueryable<Function> query, PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                if (!snapshot.CompanyId.HasValue)
                {
                    return query.Where(_ => false);
                }

                query = query.Where(x => x.CompanyID == snapshot.CompanyId.Value);
            }

            return query.Where(x => snapshot.AllowedFunctionIds.Contains(x.Id));
        }

        private static IQueryable<ResoCat> ApplyResoCatScope(IQueryable<ResoCat> query, PermissionSnapshotDto snapshot)
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
                || snapshot.CompanyScopeMode == CompanyScopeMode.Deny
                || snapshot.AllowedFunctionIds.Count == 0;
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
