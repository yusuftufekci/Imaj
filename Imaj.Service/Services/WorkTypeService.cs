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
    public class WorkTypeService : IWorkTypeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentPermissionContext _currentPermissionContext;
        private readonly ILogger<WorkTypeService> _logger;

        public WorkTypeService(
            IUnitOfWork unitOfWork,
            ICurrentPermissionContext currentPermissionContext,
            ILogger<WorkTypeService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentPermissionContext = currentPermissionContext;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResultDto<WorkTypeListItemDto>>> GetWorkTypesAsync(WorkTypeFilterDto filter)
        {
            var normalizedFilter = filter ?? new WorkTypeFilterDto();
            var page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            var pageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<PagedResultDto<WorkTypeListItemDto>>.Success(EmptyPage<WorkTypeListItemDto>(page, pageSize));
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var workTypeQuery = ApplyWorkTypeScope(_unitOfWork.Repository<WorkType>().Query(), snapshot!);

            if (normalizedFilter.IsInvalid.HasValue)
            {
                workTypeQuery = workTypeQuery.Where(x => x.Invisible == normalizedFilter.IsInvalid.Value);
            }

            var query =
                from workType in workTypeQuery
                join preferredName in _unitOfWork.Repository<XWorkType>().Query().Where(x => x.LanguageID == languageId)
                    on workType.Id equals preferredName.WorkTypeID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XWorkType>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on workType.Id equals fallbackName.WorkTypeID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                select new WorkTypeListItemDto
                {
                    Id = workType.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    Invisible = workType.Invisible
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

            return ServiceResult<PagedResultDto<WorkTypeListItemDto>>.Success(new PagedResultDto<WorkTypeListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult<WorkTypeDetailDto>> GetWorkTypeDetailAsync(decimal id)
        {
            if (id <= 0)
            {
                return ServiceResult<WorkTypeDetailDto>.Fail("Gorev tipi ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<WorkTypeDetailDto>.NotFound("Gorev tipi bulunamadi.");
            }

            var row = await ApplyWorkTypeScope(_unitOfWork.Repository<WorkType>().Query(), snapshot!)
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
                return ServiceResult<WorkTypeDetailDto>.NotFound("Gorev tipi bulunamadi.");
            }

            var names = await (
                from localizedName in _unitOfWork.Repository<XWorkType>().Query()
                join language in _unitOfWork.Repository<Language>().Query()
                    on localizedName.LanguageID equals language.Id into languageGroup
                from language in languageGroup.DefaultIfEmpty()
                where localizedName.WorkTypeID == row.Id
                orderby localizedName.LanguageID
                select new WorkTypeLocalizedNameDto
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

            return ServiceResult<WorkTypeDetailDto>.Success(new WorkTypeDetailDto
            {
                Id = row.Id,
                CompanyId = row.CompanyID,
                Invisible = row.Invisible,
                Names = names
            });
        }

        public async Task<ServiceResult<List<WorkTypeLanguageDto>>> GetLanguagesAsync()
        {
            var items = await _unitOfWork.Repository<Language>()
                .Query()
                .OrderBy(x => x.Sequence)
                .ThenBy(x => x.Id)
                .Select(x => new WorkTypeLanguageDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            return ServiceResult<List<WorkTypeLanguageDto>>.Success(items);
        }

        public async Task<ServiceResult> CreateWorkTypeAsync(WorkTypeUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Gorev tipi bilgisi bos olamaz.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Gorev tipi kaydi icin yetki kapsami bulunamadi.");
            }

            var targetCompanyId = ResolveTargetCompanyId(snapshot!);
            if (!targetCompanyId.HasValue)
            {
                return ServiceResult.Fail("Company kapsam bilgisi olmadigi icin gorev tipi olusturulamadi.");
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
                var workTypeRepo = _unitOfWork.Repository<WorkType>();
                var xWorkTypeRepo = _unitOfWork.Repository<XWorkType>();

                var nextWorkTypeId = (await workTypeRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                await workTypeRepo.AddAsync(new WorkType
                {
                    Id = nextWorkTypeId,
                    CompanyID = targetCompanyId.Value,
                    Invisible = input.Invisible,
                    SelectFlag = false,
                    Stamp = 1
                });

                var nextXWorkTypeId = (await xWorkTypeRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xWorkTypeRepo.AddAsync(new XWorkType
                    {
                        Id = nextXWorkTypeId++,
                        WorkTypeID = nextWorkTypeId,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        Stamp = 1
                    });
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Gorev tipi kaydedildi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Gorev tipi kaydedilirken hata olustu.");
                return ServiceResult.Fail("Gorev tipi kaydedilemedi.");
            }
        }

        public async Task<ServiceResult> UpdateWorkTypeAsync(WorkTypeUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Gorev tipi bilgisi bos olamaz.");
            }

            if (!input.Id.HasValue || input.Id.Value <= 0)
            {
                return ServiceResult.Fail("Gorev tipi ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Gorev tipi guncelleme icin yetki kapsami bulunamadi.");
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
                var workTypeRepo = _unitOfWork.Repository<WorkType>();
                var xWorkTypeRepo = _unitOfWork.Repository<XWorkType>();

                var workType = await ApplyWorkTypeScope(workTypeRepo.Query(), snapshot!)
                    .SingleOrDefaultAsync(x => x.Id == input.Id.Value);
                if (workType == null)
                {
                    return ServiceResult.NotFound("Gorev tipi bulunamadi.");
                }

                workType.Invisible = input.Invisible;
                workType.Stamp = 1;
                workTypeRepo.Update(workType);

                var existingNames = await xWorkTypeRepo.Query()
                    .Where(x => x.WorkTypeID == workType.Id)
                    .ToListAsync();

                foreach (var existingName in existingNames)
                {
                    xWorkTypeRepo.Remove(existingName);
                }

                var nextXWorkTypeId = (await xWorkTypeRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xWorkTypeRepo.AddAsync(new XWorkType
                    {
                        Id = nextXWorkTypeId++,
                        WorkTypeID = workType.Id,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        Stamp = 1
                    });
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Gorev tipi guncellendi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Gorev tipi guncellenirken hata olustu. WorkTypeID={WorkTypeId}", input.Id.Value);
                return ServiceResult.Fail("Gorev tipi guncellenemedi.");
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

        private static List<WorkTypeLocalizedNameInputDto> NormalizeLocalizedNames(IEnumerable<WorkTypeLocalizedNameInputDto>? input)
        {
            return input?
                .Select(x => new WorkTypeLocalizedNameInputDto
                {
                    LanguageId = x.LanguageId,
                    Name = (x.Name ?? string.Empty).Trim()
                })
                .Where(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.LanguageId)
                .Select(x => x.First())
                .OrderBy(x => x.LanguageId)
                .ToList()
                ?? new List<WorkTypeLocalizedNameInputDto>();
        }

        private static decimal? ResolveTargetCompanyId(PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                return snapshot.CompanyId;
            }

            return snapshot.CompanyId;
        }

        private static IQueryable<WorkType> ApplyWorkTypeScope(IQueryable<WorkType> query, PermissionSnapshotDto snapshot)
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
