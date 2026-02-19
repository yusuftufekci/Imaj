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
    public class ProdGrpService : IProdGrpService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentPermissionContext _currentPermissionContext;
        private readonly ILogger<ProdGrpService> _logger;

        public ProdGrpService(
            IUnitOfWork unitOfWork,
            ICurrentPermissionContext currentPermissionContext,
            ILogger<ProdGrpService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentPermissionContext = currentPermissionContext;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResultDto<ProdGrpListItemDto>>> GetProdGrpsAsync(ProdGrpFilterDto filter)
        {
            var normalizedFilter = filter ?? new ProdGrpFilterDto();
            var page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            var pageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<PagedResultDto<ProdGrpListItemDto>>.Success(EmptyPage<ProdGrpListItemDto>(page, pageSize));
            }

            var preferredLanguageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var query =
                from prodGrp in ApplyProdGrpScope(_unitOfWork.Repository<ProdGrp>().Query(), snapshot!)
                join preferredName in _unitOfWork.Repository<XProdGrp>().Query().Where(x => x.LanguageID == preferredLanguageId)
                    on prodGrp.Id equals preferredName.ProdGrpID into preferredGroup
                from preferredName in preferredGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XProdGrp>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on prodGrp.Id equals fallbackName.ProdGrpID into fallbackGroup
                from fallbackName in fallbackGroup.DefaultIfEmpty()
                select new ProdGrpListItemDto
                {
                    Id = prodGrp.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    Invisible = prodGrp.Invisible
                };

            if (normalizedFilter.IsInvalid.HasValue)
            {
                query = query.Where(x => x.Invisible == normalizedFilter.IsInvalid.Value);
            }

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

            return ServiceResult<PagedResultDto<ProdGrpListItemDto>>.Success(new PagedResultDto<ProdGrpListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult<ProdGrpDetailDto>> GetProdGrpDetailAsync(decimal id)
        {
            if (id <= 0)
            {
                return ServiceResult<ProdGrpDetailDto>.Fail("Urun grubu ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<ProdGrpDetailDto>.NotFound("Urun grubu bulunamadi.");
            }

            var row = await ApplyProdGrpScope(_unitOfWork.Repository<ProdGrp>().Query(), snapshot!)
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
                return ServiceResult<ProdGrpDetailDto>.NotFound("Urun grubu bulunamadi.");
            }

            var names = await (
                from localizedName in _unitOfWork.Repository<XProdGrp>().Query()
                join language in _unitOfWork.Repository<Language>().Query()
                    on localizedName.LanguageID equals language.Id into languageGroup
                from language in languageGroup.DefaultIfEmpty()
                where localizedName.ProdGrpID == row.Id
                orderby localizedName.LanguageID
                select new ProdGrpLocalizedNameDto
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

            return ServiceResult<ProdGrpDetailDto>.Success(new ProdGrpDetailDto
            {
                Id = row.Id,
                CompanyId = row.CompanyID,
                Invisible = row.Invisible,
                Names = names
            });
        }

        public async Task<ServiceResult<List<ProdGrpLanguageDto>>> GetLanguagesAsync()
        {
            var items = await _unitOfWork.Repository<Language>()
                .Query()
                .OrderBy(x => x.Sequence)
                .ThenBy(x => x.Id)
                .Select(x => new ProdGrpLanguageDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            return ServiceResult<List<ProdGrpLanguageDto>>.Success(items);
        }

        public async Task<ServiceResult> CreateProdGrpAsync(ProdGrpUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Urun grubu bilgisi bos olamaz.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Urun grubu kaydi icin yetki kapsami bulunamadi.");
            }

            var targetCompanyId = ResolveTargetCompanyId(snapshot!);
            if (!targetCompanyId.HasValue)
            {
                return ServiceResult.Fail("Company kapsam bilgisi olmadigi icin urun grubu olusturulamadi.");
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
                var prodGrpRepo = _unitOfWork.Repository<ProdGrp>();
                var xProdGrpRepo = _unitOfWork.Repository<XProdGrp>();

                var nextProdGrpId = (await prodGrpRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                await prodGrpRepo.AddAsync(new ProdGrp
                {
                    Id = nextProdGrpId,
                    CompanyID = targetCompanyId.Value,
                    Invisible = input.Invisible,
                    SelectFlag = false,
                    Stamp = 1
                });

                var nextXProdGrpId = (await xProdGrpRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xProdGrpRepo.AddAsync(new XProdGrp
                    {
                        Id = nextXProdGrpId++,
                        ProdGrpID = nextProdGrpId,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        Stamp = 1
                    });
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Urun grubu kaydedildi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Urun grubu kaydedilirken hata olustu.");
                return ServiceResult.Fail("Urun grubu kaydedilemedi.");
            }
        }

        public async Task<ServiceResult> UpdateProdGrpAsync(ProdGrpUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Urun grubu bilgisi bos olamaz.");
            }

            if (!input.Id.HasValue || input.Id.Value <= 0)
            {
                return ServiceResult.Fail("Urun grubu ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Urun grubu guncelleme icin yetki kapsami bulunamadi.");
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
                var prodGrpRepo = _unitOfWork.Repository<ProdGrp>();
                var xProdGrpRepo = _unitOfWork.Repository<XProdGrp>();

                var prodGrp = await ApplyProdGrpScope(prodGrpRepo.Query(), snapshot!)
                    .SingleOrDefaultAsync(x => x.Id == input.Id.Value);
                if (prodGrp == null)
                {
                    return ServiceResult.NotFound("Urun grubu bulunamadi.");
                }

                prodGrp.Invisible = input.Invisible;
                prodGrp.Stamp = 1;
                prodGrpRepo.Update(prodGrp);

                var existingNames = await xProdGrpRepo.Query()
                    .Where(x => x.ProdGrpID == prodGrp.Id)
                    .ToListAsync();

                foreach (var existingName in existingNames)
                {
                    xProdGrpRepo.Remove(existingName);
                }

                var nextXProdGrpId = (await xProdGrpRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xProdGrpRepo.AddAsync(new XProdGrp
                    {
                        Id = nextXProdGrpId++,
                        ProdGrpID = prodGrp.Id,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        Stamp = 1
                    });
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Urun grubu guncellendi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Urun grubu guncellenirken hata olustu. ProdGrpID={ProdGrpId}", input.Id.Value);
                return ServiceResult.Fail("Urun grubu guncellenemedi.");
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

        private static List<ProdGrpLocalizedNameInputDto> NormalizeLocalizedNames(IEnumerable<ProdGrpLocalizedNameInputDto>? input)
        {
            return input?
                .Select(x => new ProdGrpLocalizedNameInputDto
                {
                    LanguageId = x.LanguageId,
                    Name = (x.Name ?? string.Empty).Trim()
                })
                .Where(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.LanguageId)
                .Select(x => x.First())
                .OrderBy(x => x.LanguageId)
                .ToList()
                ?? new List<ProdGrpLocalizedNameInputDto>();
        }

        private static decimal? ResolveTargetCompanyId(PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                return snapshot.CompanyId;
            }

            return snapshot.CompanyId;
        }

        private static IQueryable<ProdGrp> ApplyProdGrpScope(IQueryable<ProdGrp> query, PermissionSnapshotDto snapshot)
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
