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
    public class TimeTypeService : ITimeTypeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentPermissionContext _currentPermissionContext;
        private readonly ILogger<TimeTypeService> _logger;

        public TimeTypeService(
            IUnitOfWork unitOfWork,
            ICurrentPermissionContext currentPermissionContext,
            ILogger<TimeTypeService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentPermissionContext = currentPermissionContext;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResultDto<TimeTypeListItemDto>>> GetTimeTypesAsync(TimeTypeFilterDto filter)
        {
            var normalizedFilter = filter ?? new TimeTypeFilterDto();
            var page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            var pageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<PagedResultDto<TimeTypeListItemDto>>.Success(EmptyPage<TimeTypeListItemDto>(page, pageSize));
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var timeTypeQuery = ApplyTimeTypeScope(_unitOfWork.Repository<TimeType>().Query(), snapshot!);

            if (normalizedFilter.IsInvalid.HasValue)
            {
                timeTypeQuery = timeTypeQuery.Where(x => x.Invisible == normalizedFilter.IsInvalid.Value);
            }

            var query =
                from timeType in timeTypeQuery
                join preferredName in _unitOfWork.Repository<XTimeType>().Query().Where(x => x.LanguageID == languageId)
                    on timeType.Id equals preferredName.TimeTypeID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XTimeType>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on timeType.Id equals fallbackName.TimeTypeID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                select new TimeTypeListItemDto
                {
                    Id = timeType.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    Invisible = timeType.Invisible
                };

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                item.Name = item.Id.ToString(CultureInfo.InvariantCulture);
            }

            return ServiceResult<PagedResultDto<TimeTypeListItemDto>>.Success(new PagedResultDto<TimeTypeListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult<TimeTypeDetailDto>> GetTimeTypeDetailAsync(decimal id)
        {
            if (id <= 0)
            {
                return ServiceResult<TimeTypeDetailDto>.Fail("Mesai tipi ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<TimeTypeDetailDto>.NotFound("Mesai tipi bulunamadi.");
            }

            var row = await ApplyTimeTypeScope(_unitOfWork.Repository<TimeType>().Query(), snapshot!)
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.CompanyID,
                    x.Invisible
                })
                .SingleOrDefaultAsync();

            if (row == null)
            {
                return ServiceResult<TimeTypeDetailDto>.NotFound("Mesai tipi bulunamadi.");
            }

            var names = await (
                from localizedName in _unitOfWork.Repository<XTimeType>().Query()
                join language in _unitOfWork.Repository<Language>().Query()
                    on localizedName.LanguageID equals language.Id into languageGroup
                from language in languageGroup.DefaultIfEmpty()
                where localizedName.TimeTypeID == row.Id
                orderby localizedName.LanguageID
                select new TimeTypeLocalizedNameDto
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

            return ServiceResult<TimeTypeDetailDto>.Success(new TimeTypeDetailDto
            {
                Id = row.Id,
                CompanyId = row.CompanyID,
                Invisible = row.Invisible,
                Names = names
            });
        }

        public async Task<ServiceResult<List<TimeTypeLanguageDto>>> GetLanguagesAsync()
        {
            var items = await _unitOfWork.Repository<Language>()
                .Query()
                .OrderBy(x => x.Sequence)
                .ThenBy(x => x.Id)
                .Select(x => new TimeTypeLanguageDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            return ServiceResult<List<TimeTypeLanguageDto>>.Success(items);
        }

        public async Task<ServiceResult> CreateTimeTypeAsync(TimeTypeUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Mesai tipi bilgisi bos olamaz.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Mesai tipi kaydi icin yetki kapsami bulunamadi.");
            }

            var targetCompanyId = ResolveTargetCompanyId(snapshot!);
            if (!targetCompanyId.HasValue)
            {
                return ServiceResult.Fail("Company kapsam bilgisi olmadigi icin mesai tipi olusturulamadi.");
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
                var timeTypeRepo = _unitOfWork.Repository<TimeType>();
                var xTimeTypeRepo = _unitOfWork.Repository<XTimeType>();

                var nextTimeTypeId = (await timeTypeRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                await timeTypeRepo.AddAsync(new TimeType
                {
                    Id = nextTimeTypeId,
                    CompanyID = targetCompanyId.Value,
                    Invisible = input.Invisible,
                    SelectFlag = false,
                    Stamp = 1
                });

                var nextXTimeTypeId = (await xTimeTypeRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xTimeTypeRepo.AddAsync(new XTimeType
                    {
                        Id = nextXTimeTypeId++,
                        TimeTypeID = nextTimeTypeId,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        Stamp = 1
                    });
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Mesai tipi kaydedildi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Mesai tipi kaydedilirken hata olustu.");
                return ServiceResult.Fail("Mesai tipi kaydedilemedi.");
            }
        }

        public async Task<ServiceResult> UpdateTimeTypeAsync(TimeTypeUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Mesai tipi bilgisi bos olamaz.");
            }

            if (!input.Id.HasValue || input.Id.Value <= 0)
            {
                return ServiceResult.Fail("Mesai tipi ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Mesai tipi guncelleme icin yetki kapsami bulunamadi.");
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
                var timeTypeRepo = _unitOfWork.Repository<TimeType>();
                var xTimeTypeRepo = _unitOfWork.Repository<XTimeType>();

                var timeType = await ApplyTimeTypeScope(timeTypeRepo.Query(), snapshot!)
                    .SingleOrDefaultAsync(x => x.Id == input.Id.Value);
                if (timeType == null)
                {
                    return ServiceResult.NotFound("Mesai tipi bulunamadi.");
                }

                timeType.Invisible = input.Invisible;
                timeType.Stamp = 1;
                timeTypeRepo.Update(timeType);

                var existingNames = await xTimeTypeRepo.Query()
                    .Where(x => x.TimeTypeID == timeType.Id)
                    .ToListAsync();

                foreach (var existingName in existingNames)
                {
                    xTimeTypeRepo.Remove(existingName);
                }

                var nextXTimeTypeId = (await xTimeTypeRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xTimeTypeRepo.AddAsync(new XTimeType
                    {
                        Id = nextXTimeTypeId++,
                        TimeTypeID = timeType.Id,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        Stamp = 1
                    });
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Mesai tipi guncellendi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Mesai tipi guncellenirken hata olustu. TimeTypeID={TimeTypeId}", input.Id.Value);
                return ServiceResult.Fail("Mesai tipi guncellenemedi.");
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

        private static List<TimeTypeLocalizedNameInputDto> NormalizeLocalizedNames(IEnumerable<TimeTypeLocalizedNameInputDto>? input)
        {
            return input?
                .Select(x => new TimeTypeLocalizedNameInputDto
                {
                    LanguageId = x.LanguageId,
                    Name = (x.Name ?? string.Empty).Trim()
                })
                .Where(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.LanguageId)
                .Select(x => x.First())
                .OrderBy(x => x.LanguageId)
                .ToList()
                ?? new List<TimeTypeLocalizedNameInputDto>();
        }

        private static decimal? ResolveTargetCompanyId(PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                return snapshot.CompanyId;
            }

            return snapshot.CompanyId;
        }

        private static IQueryable<TimeType> ApplyTimeTypeScope(IQueryable<TimeType> query, PermissionSnapshotDto snapshot)
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
