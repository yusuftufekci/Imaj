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
    public class ReasonService : IReasonService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentPermissionContext _currentPermissionContext;
        private readonly ILogger<ReasonService> _logger;

        public ReasonService(
            IUnitOfWork unitOfWork,
            ICurrentPermissionContext currentPermissionContext,
            ILogger<ReasonService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentPermissionContext = currentPermissionContext;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResultDto<ReasonListItemDto>>> GetReasonsAsync(ReasonFilterDto filter)
        {
            var normalizedFilter = filter ?? new ReasonFilterDto();
            var page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            var pageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<PagedResultDto<ReasonListItemDto>>.Success(EmptyPage<ReasonListItemDto>(page, pageSize));
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var reasonQuery = ApplyReasonScope(_unitOfWork.Repository<Reason>().Query(), snapshot!);

            if (!string.IsNullOrWhiteSpace(normalizedFilter.Code))
            {
                var code = normalizedFilter.Code.Trim();
                reasonQuery = reasonQuery.Where(x => x.Code.Contains(code));
            }

            if (normalizedFilter.ReasonCatId.HasValue && normalizedFilter.ReasonCatId.Value > 0)
            {
                reasonQuery = reasonQuery.Where(x => x.ReasonCatID == normalizedFilter.ReasonCatId.Value);
            }

            if (normalizedFilter.IsInvalid.HasValue)
            {
                reasonQuery = reasonQuery.Where(x => x.Invisible == normalizedFilter.IsInvalid.Value);
            }

            var query =
                from reason in reasonQuery
                join preferredName in _unitOfWork.Repository<XReason>().Query().Where(x => x.LanguageID == languageId)
                    on reason.Id equals preferredName.ReasonID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XReason>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on reason.Id equals fallbackName.ReasonID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                join preferredReasonCat in _unitOfWork.Repository<XReasonCat>().Query().Where(x => x.LanguageID == languageId)
                    on reason.ReasonCatID equals preferredReasonCat.ReasonCatID into preferredReasonCatGroup
                from preferredReasonCat in preferredReasonCatGroup.DefaultIfEmpty()
                join fallbackReasonCat in _unitOfWork.Repository<XReasonCat>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on reason.ReasonCatID equals fallbackReasonCat.ReasonCatID into fallbackReasonCatGroup
                from fallbackReasonCat in fallbackReasonCatGroup.DefaultIfEmpty()
                select new ReasonListItemDto
                {
                    Id = reason.Id,
                    Code = reason.Code,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    ReasonCatId = reason.ReasonCatID,
                    ReasonCatName = preferredReasonCat != null
                        ? preferredReasonCat.Name
                        : (fallbackReasonCat != null ? fallbackReasonCat.Name : string.Empty),
                    Invisible = reason.Invisible
                };

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(x => x.Code)
                .ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                item.Name = item.Code;
            }

            foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.ReasonCatName)))
            {
                item.ReasonCatName = item.ReasonCatId.ToString(CultureInfo.InvariantCulture);
            }

            return ServiceResult<PagedResultDto<ReasonListItemDto>>.Success(new PagedResultDto<ReasonListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult<ReasonDetailDto>> GetReasonDetailAsync(decimal id)
        {
            if (id <= 0)
            {
                return ServiceResult<ReasonDetailDto>.Fail("Gerekce ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<ReasonDetailDto>.NotFound("Gerekce bulunamadi.");
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var row = await ApplyReasonScope(_unitOfWork.Repository<Reason>().Query(), snapshot!)
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.CompanyID,
                    x.ReasonCatID,
                    x.Code,
                    x.Invisible
                })
                .SingleOrDefaultAsync();

            if (row == null)
            {
                return ServiceResult<ReasonDetailDto>.NotFound("Gerekce bulunamadi.");
            }

            var reasonCatName = await _unitOfWork.Repository<XReasonCat>()
                .Query()
                .Where(x => x.ReasonCatID == row.ReasonCatID
                            && (x.LanguageID == languageId || x.LanguageID == fallbackLanguageId))
                .OrderBy(x => x.LanguageID == languageId ? 0 : 1)
                .Select(x => x.Name)
                .FirstOrDefaultAsync() ?? string.Empty;

            var names = await (
                from localizedName in _unitOfWork.Repository<XReason>().Query()
                join language in _unitOfWork.Repository<Language>().Query()
                    on localizedName.LanguageID equals language.Id into languageGroup
                from language in languageGroup.DefaultIfEmpty()
                where localizedName.ReasonID == row.Id
                orderby localizedName.LanguageID
                select new ReasonLocalizedNameDto
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

            if (string.IsNullOrWhiteSpace(reasonCatName))
            {
                reasonCatName = row.ReasonCatID.ToString(CultureInfo.InvariantCulture);
            }

            return ServiceResult<ReasonDetailDto>.Success(new ReasonDetailDto
            {
                Id = row.Id,
                CompanyId = row.CompanyID,
                ReasonCatId = row.ReasonCatID,
                ReasonCatName = reasonCatName,
                Code = row.Code,
                Invisible = row.Invisible,
                Names = names
            });
        }

        public async Task<ServiceResult<List<ReasonLanguageDto>>> GetLanguagesAsync()
        {
            var items = await _unitOfWork.Repository<Language>()
                .Query()
                .OrderBy(x => x.Sequence)
                .ThenBy(x => x.Id)
                .Select(x => new ReasonLanguageDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            return ServiceResult<List<ReasonLanguageDto>>.Success(items);
        }

        public async Task<ServiceResult<List<ReasonCatOptionDto>>> GetReasonCatOptionsAsync()
        {
            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var query =
                from reasonCat in _unitOfWork.Repository<ReasonCat>().Query()
                join preferredName in _unitOfWork.Repository<XReasonCat>().Query().Where(x => x.LanguageID == languageId)
                    on reasonCat.Id equals preferredName.ReasonCatID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XReasonCat>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on reasonCat.Id equals fallbackName.ReasonCatID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                select new ReasonCatOptionDto
                {
                    Id = reasonCat.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : reasonCat.Descr)
                };

            var items = await query
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToListAsync();

            foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                item.Name = item.Id.ToString(CultureInfo.InvariantCulture);
            }

            return ServiceResult<List<ReasonCatOptionDto>>.Success(items);
        }

        public async Task<ServiceResult> CreateReasonAsync(ReasonUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Gerekce bilgisi bos olamaz.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Gerekce kaydi icin yetki kapsami bulunamadi.");
            }

            var targetCompanyId = ResolveTargetCompanyId(snapshot!);
            if (!targetCompanyId.HasValue)
            {
                return ServiceResult.Fail("Company kapsam bilgisi olmadigi icin gerekce olusturulamadi.");
            }

            var normalizedCode = NormalizeCode(input.Code);
            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                return ServiceResult.Fail("Gerekce kodu zorunludur.");
            }

            if (normalizedCode.Length > 8)
            {
                return ServiceResult.Fail("Kod en fazla 8 karakter olabilir.");
            }

            if (input.ReasonCatId <= 0)
            {
                return ServiceResult.Fail("Gerekce kategorisi secimi zorunludur.");
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

            var reasonCatValidation = await ValidateReasonCatAsync(input.ReasonCatId);
            if (!reasonCatValidation.IsSuccess)
            {
                return reasonCatValidation;
            }

            var duplicateExists = await ApplyReasonScope(_unitOfWork.Repository<Reason>().Query(), snapshot!)
                .AnyAsync(x => x.Code == normalizedCode);
            if (duplicateExists)
            {
                return ServiceResult.Fail("Ayni kod ile baska bir gerekce kaydi mevcut.");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var reasonRepo = _unitOfWork.Repository<Reason>();
                var xReasonRepo = _unitOfWork.Repository<XReason>();

                var nextReasonId = (await reasonRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                await reasonRepo.AddAsync(new Reason
                {
                    Id = nextReasonId,
                    CompanyID = targetCompanyId.Value,
                    ReasonCatID = input.ReasonCatId,
                    Code = normalizedCode,
                    Invisible = input.Invisible,
                    SelectFlag = false,
                    Stamp = 1
                });

                var nextXReasonId = (await xReasonRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xReasonRepo.AddAsync(new XReason
                    {
                        Id = nextXReasonId++,
                        ReasonID = nextReasonId,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        Stamp = 1
                    });
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Gerekce kaydedildi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Gerekce kaydedilirken hata olustu.");
                return ServiceResult.Fail("Gerekce kaydedilemedi.");
            }
        }

        public async Task<ServiceResult> UpdateReasonAsync(ReasonUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Gerekce bilgisi bos olamaz.");
            }

            if (!input.Id.HasValue || input.Id.Value <= 0)
            {
                return ServiceResult.Fail("Gerekce ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Gerekce guncelleme icin yetki kapsami bulunamadi.");
            }

            var normalizedCode = NormalizeCode(input.Code);
            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                return ServiceResult.Fail("Gerekce kodu zorunludur.");
            }

            if (normalizedCode.Length > 8)
            {
                return ServiceResult.Fail("Kod en fazla 8 karakter olabilir.");
            }

            if (input.ReasonCatId <= 0)
            {
                return ServiceResult.Fail("Gerekce kategorisi secimi zorunludur.");
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

            var reasonCatValidation = await ValidateReasonCatAsync(input.ReasonCatId);
            if (!reasonCatValidation.IsSuccess)
            {
                return reasonCatValidation;
            }

            var duplicateExists = await ApplyReasonScope(_unitOfWork.Repository<Reason>().Query(), snapshot!)
                .AnyAsync(x => x.Id != input.Id.Value && x.Code == normalizedCode);
            if (duplicateExists)
            {
                return ServiceResult.Fail("Ayni kod ile baska bir gerekce kaydi mevcut.");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var reasonRepo = _unitOfWork.Repository<Reason>();
                var xReasonRepo = _unitOfWork.Repository<XReason>();

                var reason = await ApplyReasonScope(reasonRepo.Query(), snapshot!)
                    .SingleOrDefaultAsync(x => x.Id == input.Id.Value);
                if (reason == null)
                {
                    return ServiceResult.NotFound("Gerekce bulunamadi.");
                }

                reason.ReasonCatID = input.ReasonCatId;
                reason.Code = normalizedCode;
                reason.Invisible = input.Invisible;
                reason.Stamp = 1;
                reasonRepo.Update(reason);

                var existingNames = await xReasonRepo.Query()
                    .Where(x => x.ReasonID == reason.Id)
                    .ToListAsync();

                foreach (var existingName in existingNames)
                {
                    xReasonRepo.Remove(existingName);
                }

                var nextXReasonId = (await xReasonRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xReasonRepo.AddAsync(new XReason
                    {
                        Id = nextXReasonId++,
                        ReasonID = reason.Id,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        Stamp = 1
                    });
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Gerekce guncellendi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Gerekce guncellenirken hata olustu. ReasonID={ReasonId}", input.Id.Value);
                return ServiceResult.Fail("Gerekce guncellenemedi.");
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

        private async Task<ServiceResult> ValidateReasonCatAsync(decimal reasonCatId)
        {
            var exists = await _unitOfWork.Repository<ReasonCat>()
                .Query()
                .AnyAsync(x => x.Id == reasonCatId);

            if (!exists)
            {
                return ServiceResult.Fail("Secilen gerekce kategorisi gecersiz.");
            }

            return ServiceResult.Success();
        }

        private static string NormalizeCode(string? code)
        {
            return (code ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static List<ReasonLocalizedNameInputDto> NormalizeLocalizedNames(IEnumerable<ReasonLocalizedNameInputDto>? input)
        {
            return input?
                .Select(x => new ReasonLocalizedNameInputDto
                {
                    LanguageId = x.LanguageId,
                    Name = (x.Name ?? string.Empty).Trim()
                })
                .Where(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.LanguageId)
                .Select(x => x.First())
                .OrderBy(x => x.LanguageId)
                .ToList()
                ?? new List<ReasonLocalizedNameInputDto>();
        }

        private static decimal? ResolveTargetCompanyId(PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                return snapshot.CompanyId;
            }

            return snapshot.CompanyId;
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
