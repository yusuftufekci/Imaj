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
    public class ResoCatService : IResoCatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentPermissionContext _currentPermissionContext;
        private readonly ILogger<ResoCatService> _logger;

        public ResoCatService(
            IUnitOfWork unitOfWork,
            ICurrentPermissionContext currentPermissionContext,
            ILogger<ResoCatService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentPermissionContext = currentPermissionContext;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResultDto<ResoCatListItemDto>>> GetResoCatsAsync(ResoCatFilterDto filter)
        {
            var normalizedFilter = filter ?? new ResoCatFilterDto();
            var page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            var pageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<PagedResultDto<ResoCatListItemDto>>.Success(EmptyPage<ResoCatListItemDto>(page, pageSize));
            }

            var scopedResoCats = ApplyResoCatScope(_unitOfWork.Repository<ResoCat>().Query(), snapshot!);
            var preferredLanguageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var query =
                from resoCat in scopedResoCats
                join preferredName in _unitOfWork.Repository<XResoCat>().Query().Where(x => x.LanguageID == preferredLanguageId)
                    on resoCat.Id equals preferredName.ResoCatID into preferredGroup
                from preferredName in preferredGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XResoCat>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on resoCat.Id equals fallbackName.ResoCatID into fallbackGroup
                from fallbackName in fallbackGroup.DefaultIfEmpty()
                select new ResoCatListItemDto
                {
                    Id = resoCat.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    Invisible = resoCat.Invisible
                };

            if (!string.IsNullOrWhiteSpace(normalizedFilter.Name))
            {
                var name = normalizedFilter.Name.Trim();
                query = query.Where(x => x.Name.Contains(name));
            }

            if (normalizedFilter.IsInvalid.HasValue)
            {
                query = query.Where(x => x.Invisible == normalizedFilter.IsInvalid.Value);
            }

            var first = normalizedFilter.First.HasValue && normalizedFilter.First.Value > 0 ? normalizedFilter.First.Value : (int?)null;
            IQueryable<ResoCatListItemDto> scopedQuery = query
                .OrderBy(x => x.Name)
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
                item.Name = item.Id.ToString(CultureInfo.InvariantCulture);
            }

            return ServiceResult<PagedResultDto<ResoCatListItemDto>>.Success(new PagedResultDto<ResoCatListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult<ResoCatDetailDto>> GetResoCatDetailAsync(decimal id)
        {
            if (id <= 0)
            {
                return ServiceResult<ResoCatDetailDto>.Fail("Kaynak kategorisi ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<ResoCatDetailDto>.NotFound("Kaynak kategorisi bulunamadi.");
            }

            var scopedResoCats = ApplyResoCatScope(_unitOfWork.Repository<ResoCat>().Query(), snapshot!);

            var row = await (
                from resoCat in scopedResoCats
                join company in _unitOfWork.Repository<Company>().Query() on resoCat.CompanyID equals company.Id into companyGroup
                from company in companyGroup.DefaultIfEmpty()
                where resoCat.Id == id
                select new
                {
                    resoCat.Id,
                    resoCat.CompanyID,
                    CompanyName = company != null ? company.Name : string.Empty,
                    resoCat.Invisible
                })
                .SingleOrDefaultAsync();

            if (row == null)
            {
                return ServiceResult<ResoCatDetailDto>.NotFound("Kaynak kategorisi bulunamadi.");
            }

            var names = await (
                from localizedName in _unitOfWork.Repository<XResoCat>().Query()
                join language in _unitOfWork.Repository<Language>().Query()
                    on localizedName.LanguageID equals language.Id into languageGroup
                from language in languageGroup.DefaultIfEmpty()
                where localizedName.ResoCatID == row.Id
                orderby localizedName.LanguageID
                select new ResoCatLocalizedNameDto
                {
                    LanguageId = localizedName.LanguageID,
                    LanguageName = language != null ? language.Name : string.Empty,
                    Name = localizedName.Name
                })
                .ToListAsync();

            foreach (var name in names.Where(x => string.IsNullOrWhiteSpace(x.LanguageName)))
            {
                name.LanguageName = name.LanguageId.ToString(CultureInfo.InvariantCulture);
            }

            return ServiceResult<ResoCatDetailDto>.Success(new ResoCatDetailDto
            {
                Id = row.Id,
                CompanyId = row.CompanyID,
                CompanyName = row.CompanyName,
                Invisible = row.Invisible,
                Names = names
            });
        }

        public async Task<ServiceResult<List<ResoCatLanguageDto>>> GetLanguagesAsync()
        {
            var items = await _unitOfWork.Repository<Language>()
                .Query()
                .OrderBy(x => x.Sequence)
                .ThenBy(x => x.Id)
                .Select(x => new ResoCatLanguageDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            return ServiceResult<List<ResoCatLanguageDto>>.Success(items);
        }

        public async Task<ServiceResult> CreateResoCatAsync(ResoCatUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Kaynak kategorisi bilgisi bos olamaz.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Kaynak kategorisi kaydi icin yetki kapsami bulunamadi.");
            }

            var targetCompanyId = ResolveTargetCompanyIdForCreate(snapshot!);
            if (!targetCompanyId.HasValue)
            {
                return ServiceResult.Fail("Company kapsam bilgisi olmadigi icin kaynak kategorisi olusturulamadi.");
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
                var resoCatRepository = _unitOfWork.Repository<ResoCat>();
                var xResoCatRepository = _unitOfWork.Repository<XResoCat>();

                var nextResoCatId = (await resoCatRepository.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                await resoCatRepository.AddAsync(new ResoCat
                {
                    Id = nextResoCatId,
                    CompanyID = targetCompanyId.Value,
                    Invisible = input.Invisible,
                    SelectFlag = false,
                    Stamp = 1
                });

                var nextXResoCatId = (await xResoCatRepository.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xResoCatRepository.AddAsync(new XResoCat
                    {
                        Id = nextXResoCatId++,
                        ResoCatID = nextResoCatId,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        Stamp = 1
                    });
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Kaynak kategorisi kaydedildi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Kaynak kategorisi eklenirken hata olustu.");
                return ServiceResult.Fail("Kaynak kategorisi kaydedilemedi.");
            }
        }

        public async Task<ServiceResult> UpdateResoCatAsync(ResoCatUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Kaynak kategorisi bilgisi bos olamaz.");
            }

            if (!input.Id.HasValue || input.Id.Value <= 0)
            {
                return ServiceResult.Fail("Kaynak kategorisi ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Kaynak kategorisi guncelleme icin yetki kapsami bulunamadi.");
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
                var resoCatRepository = _unitOfWork.Repository<ResoCat>();
                var xResoCatRepository = _unitOfWork.Repository<XResoCat>();

                var scopedResoCats = ApplyResoCatScope(resoCatRepository.Query(), snapshot!);
                var resoCat = await scopedResoCats.SingleOrDefaultAsync(x => x.Id == input.Id.Value);
                if (resoCat == null)
                {
                    return ServiceResult.NotFound("Kaynak kategorisi bulunamadi.");
                }

                resoCat.Invisible = input.Invisible;
                resoCat.Stamp = 1;
                resoCatRepository.Update(resoCat);

                var nextXResoCatId = (await xResoCatRepository.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                var existingLocalizedNames = await xResoCatRepository.Query()
                    .Where(x => x.ResoCatID == resoCat.Id)
                    .ToListAsync();

                foreach (var existingLocalizedName in existingLocalizedNames)
                {
                    xResoCatRepository.Remove(existingLocalizedName);
                }

                foreach (var localizedName in normalizedNames)
                {
                    await xResoCatRepository.AddAsync(new XResoCat
                    {
                        Id = nextXResoCatId++,
                        ResoCatID = resoCat.Id,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        Stamp = 1
                    });
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Kaynak kategorisi guncellendi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Kaynak kategorisi guncellenirken hata olustu. ResoCatID={ResoCatId}", input.Id.Value);
                return ServiceResult.Fail("Kaynak kategorisi guncellenemedi.");
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

        private static List<ResoCatLocalizedNameInputDto> NormalizeLocalizedNames(IEnumerable<ResoCatLocalizedNameInputDto>? input)
        {
            return input?
                .Select(x => new ResoCatLocalizedNameInputDto
                {
                    LanguageId = x.LanguageId,
                    Name = (x.Name ?? string.Empty).Trim()
                })
                .Where(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.LanguageId)
                .Select(x => x.First())
                .OrderBy(x => x.LanguageId)
                .ToList()
                ?? new List<ResoCatLocalizedNameInputDto>();
        }

        private static decimal? ResolveTargetCompanyIdForCreate(PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                return snapshot.CompanyId;
            }

            return snapshot.CompanyId;
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
