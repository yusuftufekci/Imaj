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
    public class AbsenceService : IAbsenceService
    {
        private const decimal OpenStateId = 10m;

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentPermissionContext _currentPermissionContext;
        private readonly ILogger<AbsenceService> _logger;

        public AbsenceService(
            IUnitOfWork unitOfWork,
            ICurrentPermissionContext currentPermissionContext,
            ILogger<AbsenceService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentPermissionContext = currentPermissionContext;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResultDto<AbsenceListItemDto>>> GetAbsencesAsync(AbsenceFilterDto filter)
        {
            var normalizedFilter = filter ?? new AbsenceFilterDto();
            var page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            var pageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<PagedResultDto<AbsenceListItemDto>>.Success(EmptyPage<AbsenceListItemDto>(page, pageSize));
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var reserveQuery = ApplyAbsenceScope(_unitOfWork.Repository<Reserve>().Query(), snapshot!);

            if (normalizedFilter.FunctionId.HasValue && normalizedFilter.FunctionId.Value > 0)
            {
                reserveQuery = reserveQuery.Where(x => x.FunctionID == normalizedFilter.FunctionId.Value);
            }

            if (normalizedFilter.ReasonId.HasValue && normalizedFilter.ReasonId.Value > 0)
            {
                reserveQuery = reserveQuery.Where(x => x.ReasonID == normalizedFilter.ReasonId.Value);
            }

            if (!string.IsNullOrWhiteSpace(normalizedFilter.Name))
            {
                var name = normalizedFilter.Name.Trim();
                reserveQuery = reserveQuery.Where(x => x.Name.Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(normalizedFilter.Contact))
            {
                var contact = normalizedFilter.Contact.Trim();
                reserveQuery = reserveQuery.Where(x => x.Contact.Contains(contact));
            }

            if (normalizedFilter.StartDateFrom.HasValue)
            {
                reserveQuery = reserveQuery.Where(x => x.StartDT >= normalizedFilter.StartDateFrom.Value);
            }

            if (normalizedFilter.StartDateTo.HasValue)
            {
                reserveQuery = reserveQuery.Where(x => x.StartDT <= normalizedFilter.StartDateTo.Value);
            }

            if (normalizedFilter.EndDateFrom.HasValue)
            {
                reserveQuery = reserveQuery.Where(x => x.EndDT >= normalizedFilter.EndDateFrom.Value);
            }

            if (normalizedFilter.EndDateTo.HasValue)
            {
                reserveQuery = reserveQuery.Where(x => x.EndDT <= normalizedFilter.EndDateTo.Value);
            }

            if (normalizedFilter.StateId.HasValue && normalizedFilter.StateId.Value > 0)
            {
                reserveQuery = reserveQuery.Where(x => x.StateID == normalizedFilter.StateId.Value);
            }

            if (normalizedFilter.Evaluated.HasValue)
            {
                reserveQuery = reserveQuery.Where(x => x.Evaluated == normalizedFilter.Evaluated.Value);
            }

            var normalizedResourceIds = normalizedFilter.ResourceIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (normalizedResourceIds.Count > 0)
            {
                var allocateQuery = _unitOfWork.Repository<Allocate>()
                    .Query()
                    .Where(x => x.Deleted == 0 && normalizedResourceIds.Contains(x.ResourceID));

                reserveQuery = reserveQuery.Where(x => allocateQuery.Any(a => a.ReserveID == x.Id));
            }

            var query =
                from reserve in reserveQuery
                join preferredFunction in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == languageId)
                    on reserve.FunctionID equals preferredFunction.FunctionID into preferredFunctionGroup
                from preferredFunction in preferredFunctionGroup.DefaultIfEmpty()
                join fallbackFunction in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on reserve.FunctionID equals fallbackFunction.FunctionID into fallbackFunctionGroup
                from fallbackFunction in fallbackFunctionGroup.DefaultIfEmpty()
                join preferredReason in _unitOfWork.Repository<XReason>().Query().Where(x => x.LanguageID == languageId)
                    on reserve.ReasonID equals preferredReason.ReasonID into preferredReasonGroup
                from preferredReason in preferredReasonGroup.DefaultIfEmpty()
                join fallbackReason in _unitOfWork.Repository<XReason>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on reserve.ReasonID equals fallbackReason.ReasonID into fallbackReasonGroup
                from fallbackReason in fallbackReasonGroup.DefaultIfEmpty()
                join preferredState in _unitOfWork.Repository<XState>().Query().Where(x => x.LanguageID == languageId)
                    on reserve.StateID equals preferredState.StateID into preferredStateGroup
                from preferredState in preferredStateGroup.DefaultIfEmpty()
                join fallbackState in _unitOfWork.Repository<XState>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on reserve.StateID equals fallbackState.StateID into fallbackStateGroup
                from fallbackState in fallbackStateGroup.DefaultIfEmpty()
                select new AbsenceListItemDto
                {
                    Id = reserve.Id,
                    FunctionId = reserve.FunctionID,
                    FunctionName = preferredFunction != null
                        ? preferredFunction.Name
                        : (fallbackFunction != null ? fallbackFunction.Name : string.Empty),
                    StartDate = reserve.StartDT,
                    EndDate = reserve.EndDT,
                    ReasonId = reserve.ReasonID,
                    ReasonName = preferredReason != null
                        ? preferredReason.Name
                        : (fallbackReason != null ? fallbackReason.Name : string.Empty),
                    Name = reserve.Name,
                    StateId = reserve.StateID,
                    StateName = preferredState != null
                        ? preferredState.Name
                        : (fallbackState != null ? fallbackState.Name : string.Empty),
                    Evaluated = reserve.Evaluated
                };

            var first = normalizedFilter.First.HasValue && normalizedFilter.First.Value > 0 ? normalizedFilter.First.Value : (int?)null;
            IQueryable<AbsenceListItemDto> scopedQuery = query
                .OrderBy(x => x.StartDate)
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

            foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.FunctionName)))
            {
                item.FunctionName = item.FunctionId.ToString(CultureInfo.InvariantCulture);
            }

            foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.ReasonName)))
            {
                item.ReasonName = "-";
            }

            foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.StateName)))
            {
                item.StateName = item.StateId.ToString(CultureInfo.InvariantCulture);
            }

            return ServiceResult<PagedResultDto<AbsenceListItemDto>>.Success(new PagedResultDto<AbsenceListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult<AbsenceDetailDto>> GetAbsenceDetailAsync(decimal id)
        {
            if (id <= 0)
            {
                return ServiceResult<AbsenceDetailDto>.Fail("Mazeret ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<AbsenceDetailDto>.NotFound("Mazeret bulunamadi.");
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var row = await ApplyAbsenceScope(_unitOfWork.Repository<Reserve>().Query(), snapshot!)
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.CompanyID,
                    x.FunctionID,
                    x.ReasonID,
                    x.Name,
                    x.Contact,
                    x.StartDT,
                    x.EndDT,
                    x.StateID,
                    x.Evaluated,
                    x.Notes
                })
                .SingleOrDefaultAsync();

            if (row == null)
            {
                return ServiceResult<AbsenceDetailDto>.NotFound("Mazeret bulunamadi.");
            }

            var functionName = await ResolveFunctionNameAsync(row.FunctionID, languageId, fallbackLanguageId);
            var reasonName = await ResolveReasonNameAsync(row.ReasonID, languageId, fallbackLanguageId);
            var stateName = await ResolveStateNameAsync(row.StateID, languageId, fallbackLanguageId);

            var resources = await QueryAbsenceResources(row.Id, languageId, fallbackLanguageId).ToListAsync();
            foreach (var resource in resources)
            {
                if (string.IsNullOrWhiteSpace(resource.Name))
                {
                    resource.Name = resource.Code;
                }

                if (string.IsNullOrWhiteSpace(resource.FunctionName))
                {
                    resource.FunctionName = resource.FunctionId.ToString(CultureInfo.InvariantCulture);
                }

                if (string.IsNullOrWhiteSpace(resource.ResoCatName))
                {
                    resource.ResoCatName = resource.ResoCatId.ToString(CultureInfo.InvariantCulture);
                }
            }

            return ServiceResult<AbsenceDetailDto>.Success(new AbsenceDetailDto
            {
                Id = row.Id,
                CompanyId = row.CompanyID,
                FunctionId = row.FunctionID,
                FunctionName = string.IsNullOrWhiteSpace(functionName)
                    ? row.FunctionID.ToString(CultureInfo.InvariantCulture)
                    : functionName,
                ReasonId = row.ReasonID,
                ReasonName = string.IsNullOrWhiteSpace(reasonName) ? "-" : reasonName,
                Name = row.Name,
                Contact = row.Contact,
                StartDate = row.StartDT,
                EndDate = row.EndDT,
                StateId = row.StateID,
                StateName = string.IsNullOrWhiteSpace(stateName)
                    ? row.StateID.ToString(CultureInfo.InvariantCulture)
                    : stateName,
                Evaluated = row.Evaluated,
                Notes = row.Notes,
                Resources = resources
            });
        }

        public async Task<ServiceResult<List<AbsenceFunctionOptionDto>>> GetFunctionOptionsAsync()
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<List<AbsenceFunctionOptionDto>>.Success(new List<AbsenceFunctionOptionDto>());
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
                select new AbsenceFunctionOptionDto
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

            return ServiceResult<List<AbsenceFunctionOptionDto>>.Success(items);
        }

        public async Task<ServiceResult<List<AbsenceReasonOptionDto>>> GetReasonOptionsAsync()
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<List<AbsenceReasonOptionDto>>.Success(new List<AbsenceReasonOptionDto>());
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var reasonQuery = ApplyReasonScope(_unitOfWork.Repository<Reason>().Query(), snapshot!);

            var query =
                from reason in reasonQuery
                join preferredName in _unitOfWork.Repository<XReason>().Query().Where(x => x.LanguageID == languageId)
                    on reason.Id equals preferredName.ReasonID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XReason>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on reason.Id equals fallbackName.ReasonID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                select new AbsenceReasonOptionDto
                {
                    Id = reason.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : reason.Code)
                };

            var items = await query
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToListAsync();

            foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                item.Name = item.Id.ToString(CultureInfo.InvariantCulture);
            }

            return ServiceResult<List<AbsenceReasonOptionDto>>.Success(items);
        }

        public async Task<ServiceResult<List<AbsenceStateOptionDto>>> GetStateOptionsAsync()
        {
            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var stateQuery = _unitOfWork.Repository<State>()
                .Query()
                .Where(x => x.Category == "Reserve");

            var query =
                from state in stateQuery
                join preferredName in _unitOfWork.Repository<XState>().Query().Where(x => x.LanguageID == languageId)
                    on state.Id equals preferredName.StateID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XState>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on state.Id equals fallbackName.StateID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                select new AbsenceStateOptionDto
                {
                    Id = state.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty)
                };

            var items = await query
                .OrderBy(x => x.Id)
                .ToListAsync();

            foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                item.Name = item.Id.ToString(CultureInfo.InvariantCulture);
            }

            return ServiceResult<List<AbsenceStateOptionDto>>.Success(items);
        }

        public async Task<ServiceResult<PagedResultDto<AbsenceResourceItemDto>>> SearchResourcesAsync(AbsenceResourceLookupFilterDto filter)
        {
            var normalizedFilter = filter ?? new AbsenceResourceLookupFilterDto();
            var page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            var pageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 10;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<PagedResultDto<AbsenceResourceItemDto>>.Success(EmptyPage<AbsenceResourceItemDto>(page, pageSize));
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var resourceQuery = ApplyResourceScope(_unitOfWork.Repository<Resource>().Query(), snapshot!);

            if (normalizedFilter.FunctionId.HasValue && normalizedFilter.FunctionId.Value > 0)
            {
                resourceQuery = resourceQuery.Where(x => x.FunctionID == normalizedFilter.FunctionId.Value);
            }

            if (!string.IsNullOrWhiteSpace(normalizedFilter.Code))
            {
                var code = normalizedFilter.Code.Trim();
                resourceQuery = resourceQuery.Where(x => x.Code.Contains(code));
            }

            if (!string.IsNullOrWhiteSpace(normalizedFilter.Name))
            {
                var name = normalizedFilter.Name.Trim();
                var matchingIds = _unitOfWork.Repository<XResource>().Query()
                    .Where(x => x.Name.Contains(name))
                    .Select(x => x.ResourceID)
                    .Distinct();

                resourceQuery = resourceQuery.Where(x => matchingIds.Contains(x.Id));
            }

            if (normalizedFilter.IsInvalid.HasValue)
            {
                resourceQuery = resourceQuery.Where(x => x.Invisible == normalizedFilter.IsInvalid.Value);
            }
            else
            {
                resourceQuery = resourceQuery.Where(x => !x.Invisible);
            }

            var excludeIds = normalizedFilter.ExcludeIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();
            if (excludeIds.Count > 0)
            {
                resourceQuery = resourceQuery.Where(x => !excludeIds.Contains(x.Id));
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
                select new AbsenceResourceItemDto
                {
                    ResourceId = resource.Id,
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
                .OrderBy(x => x.FunctionName)
                .ThenBy(x => x.ResoCatName)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.ResourceId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    item.Name = item.Code;
                }

                if (string.IsNullOrWhiteSpace(item.FunctionName))
                {
                    item.FunctionName = item.FunctionId.ToString(CultureInfo.InvariantCulture);
                }

                if (string.IsNullOrWhiteSpace(item.ResoCatName))
                {
                    item.ResoCatName = item.ResoCatId.ToString(CultureInfo.InvariantCulture);
                }
            }

            return ServiceResult<PagedResultDto<AbsenceResourceItemDto>>.Success(new PagedResultDto<AbsenceResourceItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult> CreateAbsenceAsync(AbsenceCreateDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Mazeret bilgisi bos olamaz.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Mazeret kaydi icin yetki kapsami bulunamadi.");
            }

            var targetCompanyId = ResolveTargetCompanyId(snapshot!);
            if (!targetCompanyId.HasValue)
            {
                return ServiceResult.Fail("Company kapsam bilgisi olmadigi icin mazeret olusturulamadi.");
            }

            var normalizedName = (input.Name ?? string.Empty).Trim();
            var normalizedContact = (input.Contact ?? string.Empty).Trim();
            var normalizedNotes = (input.Notes ?? string.Empty).Trim();
            var resourceIds = input.ResourceIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (input.FunctionId <= 0)
            {
                return ServiceResult.Fail("Fonksiyon secimi zorunludur.");
            }

            if (input.ReasonId <= 0)
            {
                return ServiceResult.Fail("Gerekce secimi zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return ServiceResult.Fail("Ad alani zorunludur.");
            }

            if (normalizedName.Length > 32)
            {
                return ServiceResult.Fail("Ad en fazla 32 karakter olabilir.");
            }

            if (normalizedContact.Length > 32)
            {
                return ServiceResult.Fail("Ilgili alani en fazla 32 karakter olabilir.");
            }

            if (input.StartDate >= input.EndDate)
            {
                return ServiceResult.Fail("Bitis tarihi baslangic tarihinden buyuk olmalidir.");
            }

            var functionValidation = await ValidateFunctionAsync(input.FunctionId, snapshot!);
            if (!functionValidation.IsSuccess)
            {
                return functionValidation;
            }

            var reasonValidation = await ValidateReasonAsync(input.ReasonId, snapshot!);
            if (!reasonValidation.IsSuccess)
            {
                return reasonValidation;
            }

            var stateValidation = await ValidateOpenStateAsync();
            if (!stateValidation.IsSuccess)
            {
                return stateValidation;
            }

            if (resourceIds.Count > 0)
            {
                var resourceValidation = await ValidateResourcesAsync(resourceIds, snapshot!);
                if (!resourceValidation.IsSuccess)
                {
                    return resourceValidation;
                }
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var reserveRepo = _unitOfWork.Repository<Reserve>();
                var allocateRepo = _unitOfWork.Repository<Allocate>();

                var nextReserveId = (await reserveRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                await reserveRepo.AddAsync(new Reserve
                {
                    Id = nextReserveId,
                    CompanyID = targetCompanyId.Value,
                    FunctionID = input.FunctionId,
                    StateID = OpenStateId,
                    Name = normalizedName,
                    Contact = normalizedContact,
                    Notes = normalizedNotes,
                    StartDT = input.StartDate,
                    EndDT = input.EndDate,
                    Absence = true,
                    Evaluated = input.Evaluated,
                    SelectFlag = false,
                    Stamp = 1,
                    CustomerID = null,
                    ReasonID = input.ReasonId
                });

                if (resourceIds.Count > 0)
                {
                    var nextAllocateId = (await allocateRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                    foreach (var resourceId in resourceIds)
                    {
                        await allocateRepo.AddAsync(new Allocate
                        {
                            Id = nextAllocateId++,
                            ReserveID = nextReserveId,
                            ResourceID = resourceId,
                            Deleted = 0,
                            SelectFlag = false,
                            Stamp = 1
                        });
                    }
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Mazeret kaydedildi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Mazeret kaydedilirken hata olustu.");
                return ServiceResult.Fail("Mazeret kaydedilemedi.");
            }
        }

        private async Task<ServiceResult> ValidateFunctionAsync(decimal functionId, PermissionSnapshotDto snapshot)
        {
            var exists = await ApplyFunctionScope(_unitOfWork.Repository<Function>().Query(), snapshot)
                .AnyAsync(x => x.Id == functionId);

            if (!exists)
            {
                return ServiceResult.Fail("Secilen fonksiyon gecersiz veya kapsam disi.");
            }

            return ServiceResult.Success();
        }

        private async Task<ServiceResult> ValidateReasonAsync(decimal reasonId, PermissionSnapshotDto snapshot)
        {
            var exists = await ApplyReasonScope(_unitOfWork.Repository<Reason>().Query(), snapshot)
                .AnyAsync(x => x.Id == reasonId);

            if (!exists)
            {
                return ServiceResult.Fail("Secilen gerekce gecersiz veya kapsam disi.");
            }

            return ServiceResult.Success();
        }

        private async Task<ServiceResult> ValidateOpenStateAsync()
        {
            var exists = await _unitOfWork.Repository<State>()
                .Query()
                .AnyAsync(x => x.Id == OpenStateId && x.Category == "Reserve");

            if (!exists)
            {
                return ServiceResult.Fail("Acilis durumu tanimli degil.");
            }

            return ServiceResult.Success();
        }

        private async Task<ServiceResult> ValidateResourcesAsync(List<decimal> resourceIds, PermissionSnapshotDto snapshot)
        {
            var scopedCount = await ApplyResourceScope(_unitOfWork.Repository<Resource>().Query(), snapshot)
                .CountAsync(x => resourceIds.Contains(x.Id));

            if (scopedCount != resourceIds.Count)
            {
                return ServiceResult.Fail("Secilen kaynaklardan en az biri gecersiz veya kapsam disi.");
            }

            return ServiceResult.Success();
        }

        private async Task<string> ResolveFunctionNameAsync(decimal functionId, decimal languageId, decimal fallbackLanguageId)
        {
            return await _unitOfWork.Repository<XFunction>()
                .Query()
                .Where(x => x.FunctionID == functionId
                            && (x.LanguageID == languageId || x.LanguageID == fallbackLanguageId))
                .OrderBy(x => x.LanguageID == languageId ? 0 : 1)
                .Select(x => x.Name)
                .FirstOrDefaultAsync() ?? string.Empty;
        }

        private async Task<string> ResolveReasonNameAsync(decimal? reasonId, decimal languageId, decimal fallbackLanguageId)
        {
            if (!reasonId.HasValue)
            {
                return string.Empty;
            }

            return await _unitOfWork.Repository<XReason>()
                .Query()
                .Where(x => x.ReasonID == reasonId.Value
                            && (x.LanguageID == languageId || x.LanguageID == fallbackLanguageId))
                .OrderBy(x => x.LanguageID == languageId ? 0 : 1)
                .Select(x => x.Name)
                .FirstOrDefaultAsync() ?? string.Empty;
        }

        private async Task<string> ResolveStateNameAsync(decimal stateId, decimal languageId, decimal fallbackLanguageId)
        {
            return await _unitOfWork.Repository<XState>()
                .Query()
                .Where(x => x.StateID == stateId
                            && (x.LanguageID == languageId || x.LanguageID == fallbackLanguageId))
                .OrderBy(x => x.LanguageID == languageId ? 0 : 1)
                .Select(x => x.Name)
                .FirstOrDefaultAsync() ?? string.Empty;
        }

        private IQueryable<AbsenceResourceItemDto> QueryAbsenceResources(decimal reserveId, decimal languageId, decimal fallbackLanguageId)
        {
            return
                from allocate in _unitOfWork.Repository<Allocate>().Query()
                join resource in _unitOfWork.Repository<Resource>().Query() on allocate.ResourceID equals resource.Id
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
                where allocate.ReserveID == reserveId && allocate.Deleted == 0
                orderby resource.FunctionID, resource.ResoCatID, resource.Code
                select new AbsenceResourceItemDto
                {
                    ResourceId = resource.Id,
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
        }

        private static decimal? ResolveTargetCompanyId(PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                return snapshot.CompanyId;
            }

            return snapshot.CompanyId;
        }

        private static IQueryable<Reserve> ApplyAbsenceScope(IQueryable<Reserve> query, PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                if (!snapshot.CompanyId.HasValue)
                {
                    return query.Where(_ => false);
                }

                query = query.Where(x => x.CompanyID == snapshot.CompanyId.Value);
            }

            return query
                .Where(x => x.Absence)
                .Where(x => snapshot.AllowedFunctionIds.Contains(x.FunctionID));
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

        private static IQueryable<Reason> ApplyReasonScope(IQueryable<Reason> query, PermissionSnapshotDto snapshot)
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
