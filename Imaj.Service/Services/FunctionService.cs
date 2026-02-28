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
    public class FunctionService : IFunctionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentPermissionContext _currentPermissionContext;
        private readonly ILogger<FunctionService> _logger;

        public FunctionService(
            IUnitOfWork unitOfWork,
            ICurrentPermissionContext currentPermissionContext,
            ILogger<FunctionService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentPermissionContext = currentPermissionContext;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResultDto<FunctionListItemDto>>> GetFunctionsAsync(FunctionFilterDto filter)
        {
            var normalizedFilter = filter ?? new FunctionFilterDto();
            var page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            var pageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<PagedResultDto<FunctionListItemDto>>.Success(EmptyPage<FunctionListItemDto>(page, pageSize));
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var functionQuery = ApplyFunctionScope(_unitOfWork.Repository<Function>().Query(), snapshot!);

            if (normalizedFilter.Reservable.HasValue)
            {
                functionQuery = functionQuery.Where(x => x.Reservable == normalizedFilter.Reservable.Value);
            }

            if (normalizedFilter.IntervalId.HasValue && normalizedFilter.IntervalId.Value > 0)
            {
                functionQuery = functionQuery.Where(x => x.IntervalID == normalizedFilter.IntervalId.Value);
            }

            if (normalizedFilter.IsInvalid.HasValue)
            {
                functionQuery = functionQuery.Where(x => x.Invisible == normalizedFilter.IsInvalid.Value);
            }

            var query =
                from function in functionQuery
                join preferredName in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == languageId)
                    on function.Id equals preferredName.FunctionID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on function.Id equals fallbackName.FunctionID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                join preferredInterval in _unitOfWork.Repository<XInterval>().Query().Where(x => x.LanguageID == languageId)
                    on function.IntervalID equals (decimal?)preferredInterval.IntervalID into preferredIntervalGroup
                from preferredInterval in preferredIntervalGroup.DefaultIfEmpty()
                join fallbackInterval in _unitOfWork.Repository<XInterval>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on function.IntervalID equals (decimal?)fallbackInterval.IntervalID into fallbackIntervalGroup
                from fallbackInterval in fallbackIntervalGroup.DefaultIfEmpty()
                select new FunctionListItemDto
                {
                    Id = function.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    Reservable = function.Reservable,
                    IntervalId = function.IntervalID,
                    IntervalName = preferredInterval != null
                        ? preferredInterval.Name
                        : (fallbackInterval != null ? fallbackInterval.Name : string.Empty),
                    Invisible = function.Invisible
                };

            var first = normalizedFilter.First.HasValue && normalizedFilter.First.Value > 0 ? normalizedFilter.First.Value : (int?)null;
            IQueryable<FunctionListItemDto> scopedQuery = query
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

            return ServiceResult<PagedResultDto<FunctionListItemDto>>.Success(new PagedResultDto<FunctionListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult<FunctionDetailDto>> GetFunctionDetailAsync(decimal id)
        {
            if (id <= 0)
            {
                return ServiceResult<FunctionDetailDto>.Fail("Fonksiyon ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<FunctionDetailDto>.NotFound("Fonksiyon bulunamadi.");
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var functionRow = await ApplyFunctionScope(_unitOfWork.Repository<Function>().Query(), snapshot!)
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.CompanyID,
                    x.Reservable,
                    x.WorkMandatory,
                    x.ProdMandatory,
                    x.Invisible,
                    x.IntervalID
                })
                .SingleOrDefaultAsync();

            if (functionRow == null)
            {
                return ServiceResult<FunctionDetailDto>.NotFound("Fonksiyon bulunamadi.");
            }

            var intervalName = string.Empty;
            if (functionRow.IntervalID.HasValue)
            {
                intervalName = await _unitOfWork.Repository<XInterval>()
                    .Query()
                    .Where(x => x.IntervalID == functionRow.IntervalID.Value
                                && (x.LanguageID == languageId || x.LanguageID == fallbackLanguageId))
                    .OrderBy(x => x.LanguageID == languageId ? 0 : 1)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync() ?? string.Empty;
            }

            var names = await (
                from localizedName in _unitOfWork.Repository<XFunction>().Query()
                join language in _unitOfWork.Repository<Language>().Query()
                    on localizedName.LanguageID equals language.Id into languageGroup
                from language in languageGroup.DefaultIfEmpty()
                where localizedName.FunctionID == id
                orderby localizedName.LanguageID
                select new FunctionLocalizedNameDto
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

            var scopedProducts = ApplyProductScope(_unitOfWork.Repository<Product>().Query(), snapshot!);
            var products = await (
                from mapping in _unitOfWork.Repository<FuncProd>().Query()
                join product in scopedProducts on mapping.ProductID equals product.Id
                join preferredName in _unitOfWork.Repository<XProduct>().Query().Where(x => x.LanguageID == languageId)
                    on product.Id equals preferredName.ProductID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XProduct>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on product.Id equals fallbackName.ProductID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                where mapping.FunctionID == id && mapping.Deleted == 0
                orderby product.Code
                select new FunctionProductAssignmentDto
                {
                    ProductId = product.Id,
                    Code = product.Code,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    Invisible = product.Invisible
                })
                .ToListAsync();

            foreach (var product in products.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                product.Name = product.ProductId.ToString(CultureInfo.InvariantCulture);
            }

            var ruleRows = await _unitOfWork.Repository<FuncRule>()
                .Query()
                .Where(x => x.FunctionID == id && x.Deleted == 0)
                .OrderBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.MinValue,
                    x.MaxValue
                })
                .ToListAsync();

            var ruleIds = ruleRows.Select(x => x.Id).ToList();
            var resoMappings = new List<(decimal RuleId, FunctionRuleResoCatDto ResoCat)>();

            if (ruleIds.Count > 0)
            {
                var scopedResoCats = ApplyResoCatScope(_unitOfWork.Repository<ResoCat>().Query(), snapshot!);

                var rows = await (
                    from mapping in _unitOfWork.Repository<FuncReso>().Query()
                    join resoCat in scopedResoCats on mapping.ResoCatID equals resoCat.Id
                    join preferredName in _unitOfWork.Repository<XResoCat>().Query().Where(x => x.LanguageID == languageId)
                        on resoCat.Id equals preferredName.ResoCatID into preferredNameGroup
                    from preferredName in preferredNameGroup.DefaultIfEmpty()
                    join fallbackName in _unitOfWork.Repository<XResoCat>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                        on resoCat.Id equals fallbackName.ResoCatID into fallbackNameGroup
                    from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                    where ruleIds.Contains(mapping.FuncRuleID) && mapping.Deleted == 0
                    orderby mapping.FuncRuleID, resoCat.Id
                    select new
                    {
                        mapping.FuncRuleID,
                        resoCat.Id,
                        Name = preferredName != null
                            ? preferredName.Name
                            : (fallbackName != null ? fallbackName.Name : string.Empty),
                        resoCat.Invisible
                    })
                    .ToListAsync();

                resoMappings = rows
                    .Select(x => (
                        x.FuncRuleID,
                        new FunctionRuleResoCatDto
                        {
                            ResoCatId = x.Id,
                            Name = string.IsNullOrWhiteSpace(x.Name)
                                ? x.Id.ToString(CultureInfo.InvariantCulture)
                                : x.Name,
                            Invisible = x.Invisible
                        }))
                    .ToList();
            }

            var rules = ruleRows
                .Select(rule => new FunctionRuleDto
                {
                    RuleId = rule.Id,
                    Name = rule.Name,
                    MinValue = rule.MinValue,
                    MaxValue = rule.MaxValue,
                    ResoCats = resoMappings
                        .Where(x => x.RuleId == rule.Id)
                        .Select(x => x.ResoCat)
                        .OrderBy(x => x.Name)
                        .ToList()
                })
                .ToList();

            return ServiceResult<FunctionDetailDto>.Success(new FunctionDetailDto
            {
                Id = functionRow.Id,
                CompanyId = functionRow.CompanyID,
                Reservable = functionRow.Reservable,
                WorkMandatory = functionRow.WorkMandatory,
                ProdMandatory = functionRow.ProdMandatory,
                Invisible = functionRow.Invisible,
                IntervalId = functionRow.IntervalID,
                IntervalName = intervalName,
                Names = names,
                Products = products,
                Rules = rules
            });
        }

        public async Task<ServiceResult<List<FunctionLanguageDto>>> GetLanguagesAsync()
        {
            var items = await _unitOfWork.Repository<Language>()
                .Query()
                .OrderBy(x => x.Sequence)
                .ThenBy(x => x.Id)
                .Select(x => new FunctionLanguageDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            return ServiceResult<List<FunctionLanguageDto>>.Success(items);
        }

        public async Task<ServiceResult<List<FunctionIntervalOptionDto>>> GetIntervalsAsync()
        {
            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var query =
                from interval in _unitOfWork.Repository<Interval>().Query()
                join preferredName in _unitOfWork.Repository<XInterval>().Query().Where(x => x.LanguageID == languageId)
                    on interval.Id equals preferredName.IntervalID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XInterval>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on interval.Id equals fallbackName.IntervalID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                select new FunctionIntervalOptionDto
                {
                    Id = interval.Id,
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

            return ServiceResult<List<FunctionIntervalOptionDto>>.Success(items);
        }

        public async Task<ServiceResult<PagedResultDto<FunctionProductLookupItemDto>>> SearchProductsAsync(FunctionProductLookupFilterDto filter)
        {
            var normalizedFilter = filter ?? new FunctionProductLookupFilterDto();
            var page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            var pageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 10;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<PagedResultDto<FunctionProductLookupItemDto>>.Success(EmptyPage<FunctionProductLookupItemDto>(page, pageSize));
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;
            var productQuery = ApplyProductScope(_unitOfWork.Repository<Product>().Query(), snapshot!);

            if (normalizedFilter.IsInvalid.HasValue)
            {
                productQuery = productQuery.Where(x => x.Invisible == normalizedFilter.IsInvalid.Value);
            }

            if (normalizedFilter.ExcludeIds.Count > 0)
            {
                productQuery = productQuery.Where(x => !normalizedFilter.ExcludeIds.Contains(x.Id));
            }

            var query =
                from product in productQuery
                join preferredName in _unitOfWork.Repository<XProduct>().Query().Where(x => x.LanguageID == languageId)
                    on product.Id equals preferredName.ProductID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XProduct>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on product.Id equals fallbackName.ProductID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                select new FunctionProductLookupItemDto
                {
                    Id = product.Id,
                    Code = product.Code,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    Invisible = product.Invisible
                };

            if (!string.IsNullOrWhiteSpace(normalizedFilter.Code))
            {
                var code = normalizedFilter.Code.Trim();
                query = query.Where(x => x.Code.Contains(code));
            }

            if (!string.IsNullOrWhiteSpace(normalizedFilter.Name))
            {
                var name = normalizedFilter.Name.Trim();
                query = query.Where(x => x.Name.Contains(name));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(x => x.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                item.Name = item.Id.ToString(CultureInfo.InvariantCulture);
            }

            return ServiceResult<PagedResultDto<FunctionProductLookupItemDto>>.Success(new PagedResultDto<FunctionProductLookupItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult<PagedResultDto<FunctionResoCatLookupItemDto>>> SearchResoCategoriesAsync(FunctionResoCatLookupFilterDto filter)
        {
            var normalizedFilter = filter ?? new FunctionResoCatLookupFilterDto();
            var page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            var pageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 10;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<PagedResultDto<FunctionResoCatLookupItemDto>>.Success(EmptyPage<FunctionResoCatLookupItemDto>(page, pageSize));
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;
            var resoCatQuery = ApplyResoCatScope(_unitOfWork.Repository<ResoCat>().Query(), snapshot!);

            if (normalizedFilter.IsInvalid.HasValue)
            {
                resoCatQuery = resoCatQuery.Where(x => x.Invisible == normalizedFilter.IsInvalid.Value);
            }

            if (normalizedFilter.ExcludeIds.Count > 0)
            {
                resoCatQuery = resoCatQuery.Where(x => !normalizedFilter.ExcludeIds.Contains(x.Id));
            }

            var query =
                from resoCat in resoCatQuery
                join preferredName in _unitOfWork.Repository<XResoCat>().Query().Where(x => x.LanguageID == languageId)
                    on resoCat.Id equals preferredName.ResoCatID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XResoCat>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on resoCat.Id equals fallbackName.ResoCatID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                select new FunctionResoCatLookupItemDto
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

            return ServiceResult<PagedResultDto<FunctionResoCatLookupItemDto>>.Success(new PagedResultDto<FunctionResoCatLookupItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult> CreateFunctionAsync(FunctionUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Fonksiyon bilgisi bos olamaz.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Fonksiyon kaydi icin yetki kapsami bulunamadi.");
            }

            var targetCompanyId = ResolveTargetCompanyId(snapshot!);
            if (!targetCompanyId.HasValue)
            {
                return ServiceResult.Fail("Company kapsam bilgisi olmadigi icin fonksiyon olusturulamadi.");
            }

            var normalizedIntervalId = NormalizeIntervalId(input.IntervalId);
            var reservationValidation = ValidateReservationPair(input.Reservable, normalizedIntervalId);
            if (!reservationValidation.IsSuccess)
            {
                return reservationValidation;
            }

            var normalizedNames = NormalizeLocalizedNames(input.Names);
            if (normalizedNames.Count == 0)
            {
                return ServiceResult.Fail("En az bir dilde ad girilmelidir.");
            }

            var normalizedProductIds = NormalizePositiveIds(input.ProductIds);
            var normalizedRules = NormalizeRules(input.Rules);

            var ruleValidation = ValidateRules(normalizedRules, input.Reservable);
            if (!ruleValidation.IsSuccess)
            {
                return ruleValidation;
            }

            var languageValidation = await ValidateLanguagesAsync(normalizedNames.Select(x => x.LanguageId));
            if (!languageValidation.IsSuccess)
            {
                return languageValidation;
            }

            var intervalValidation = await ValidateIntervalAsync(normalizedIntervalId);
            if (!intervalValidation.IsSuccess)
            {
                return intervalValidation;
            }

            var productValidation = await ValidateProductsAsync(normalizedProductIds, snapshot!);
            if (!productValidation.IsSuccess)
            {
                return productValidation;
            }

            var allResoCatIds = normalizedRules
                .SelectMany(x => x.ResoCatIds)
                .Distinct()
                .ToList();

            var resoCatValidation = await ValidateResoCategoriesAsync(allResoCatIds, snapshot!);
            if (!resoCatValidation.IsSuccess)
            {
                return resoCatValidation;
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var functionRepo = _unitOfWork.Repository<Function>();
                var xFunctionRepo = _unitOfWork.Repository<XFunction>();

                var nextFunctionId = (await functionRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                await functionRepo.AddAsync(new Function
                {
                    Id = nextFunctionId,
                    CompanyID = targetCompanyId.Value,
                    Reservable = input.Reservable,
                    WorkMandatory = input.WorkMandatory,
                    ProdMandatory = input.ProdMandatory,
                    Invisible = input.Invisible,
                    SelectFlag = false,
                    Stamp = 1,
                    IntervalID = normalizedIntervalId
                });

                var nextXFunctionId = (await xFunctionRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xFunctionRepo.AddAsync(new XFunction
                    {
                        Id = nextXFunctionId++,
                        FunctionID = nextFunctionId,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        Stamp = 1
                    });
                }

                await AddFunctionProductMappingsAsync(nextFunctionId, normalizedProductIds);
                if (input.Reservable && normalizedIntervalId.HasValue)
                {
                    await AddFunctionRulesAsync(nextFunctionId, normalizedRules);
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Fonksiyon kaydedildi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Fonksiyon kaydedilirken hata olustu.");
                return ServiceResult.Fail("Fonksiyon kaydedilemedi.");
            }
        }

        public async Task<ServiceResult> UpdateFunctionAsync(FunctionUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Fonksiyon bilgisi bos olamaz.");
            }

            if (!input.Id.HasValue || input.Id.Value <= 0)
            {
                return ServiceResult.Fail("Fonksiyon ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Fonksiyon guncelleme icin yetki kapsami bulunamadi.");
            }

            var normalizedIntervalId = NormalizeIntervalId(input.IntervalId);
            var reservationValidation = ValidateReservationPair(input.Reservable, normalizedIntervalId);
            if (!reservationValidation.IsSuccess)
            {
                return reservationValidation;
            }

            var normalizedNames = NormalizeLocalizedNames(input.Names);
            if (normalizedNames.Count == 0)
            {
                return ServiceResult.Fail("En az bir dilde ad girilmelidir.");
            }

            var normalizedProductIds = NormalizePositiveIds(input.ProductIds);
            var normalizedRules = NormalizeRules(input.Rules);

            var ruleValidation = ValidateRules(normalizedRules, input.Reservable);
            if (!ruleValidation.IsSuccess)
            {
                return ruleValidation;
            }

            var languageValidation = await ValidateLanguagesAsync(normalizedNames.Select(x => x.LanguageId));
            if (!languageValidation.IsSuccess)
            {
                return languageValidation;
            }

            var intervalValidation = await ValidateIntervalAsync(normalizedIntervalId);
            if (!intervalValidation.IsSuccess)
            {
                return intervalValidation;
            }

            var productValidation = await ValidateProductsAsync(normalizedProductIds, snapshot!);
            if (!productValidation.IsSuccess)
            {
                return productValidation;
            }

            var allResoCatIds = normalizedRules
                .SelectMany(x => x.ResoCatIds)
                .Distinct()
                .ToList();

            var resoCatValidation = await ValidateResoCategoriesAsync(allResoCatIds, snapshot!);
            if (!resoCatValidation.IsSuccess)
            {
                return resoCatValidation;
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var functionRepo = _unitOfWork.Repository<Function>();
                var xFunctionRepo = _unitOfWork.Repository<XFunction>();

                var function = await ApplyFunctionScope(functionRepo.Query(), snapshot!)
                    .SingleOrDefaultAsync(x => x.Id == input.Id.Value);
                if (function == null)
                {
                    return ServiceResult.NotFound("Fonksiyon bulunamadi.");
                }

                function.Reservable = input.Reservable;
                function.WorkMandatory = input.WorkMandatory;
                function.ProdMandatory = input.ProdMandatory;
                function.Invisible = input.Invisible;
                function.IntervalID = normalizedIntervalId;
                function.Stamp = 1;
                functionRepo.Update(function);

                var existingLocalizedNames = await xFunctionRepo.Query()
                    .Where(x => x.FunctionID == function.Id)
                    .ToListAsync();
                foreach (var existingLocalizedName in existingLocalizedNames)
                {
                    xFunctionRepo.Remove(existingLocalizedName);
                }

                var nextXFunctionId = (await xFunctionRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xFunctionRepo.AddAsync(new XFunction
                    {
                        Id = nextXFunctionId++,
                        FunctionID = function.Id,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        Stamp = 1
                    });
                }

                await ReplaceFunctionProductMappingsAsync(function.Id, normalizedProductIds);
                await ReplaceFunctionRulesAsync(function.Id, normalizedRules, input.Reservable, normalizedIntervalId);

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Fonksiyon guncellendi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Fonksiyon guncellenirken hata olustu. FunctionID={FunctionId}", input.Id.Value);
                return ServiceResult.Fail("Fonksiyon guncellenemedi.");
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

        private async Task<ServiceResult> ValidateIntervalAsync(decimal? intervalId)
        {
            if (!intervalId.HasValue)
            {
                return ServiceResult.Success();
            }

            var exists = await _unitOfWork.Repository<Interval>()
                .Query()
                .AnyAsync(x => x.Id == intervalId.Value);

            if (!exists)
            {
                return ServiceResult.Fail("Secilen rezervasyon araligi bulunamadi.");
            }

            return ServiceResult.Success();
        }

        private async Task<ServiceResult> ValidateProductsAsync(IReadOnlyCollection<decimal> productIds, PermissionSnapshotDto snapshot)
        {
            if (productIds.Count == 0)
            {
                return ServiceResult.Success();
            }

            var validCount = await ApplyProductScope(_unitOfWork.Repository<Product>().Query(), snapshot)
                .Where(x => productIds.Contains(x.Id))
                .Select(x => x.Id)
                .Distinct()
                .CountAsync();

            if (validCount != productIds.Count)
            {
                return ServiceResult.Fail("Secilen urunlerden en az biri gecersiz veya kapsam disi.");
            }

            return ServiceResult.Success();
        }

        private async Task<ServiceResult> ValidateResoCategoriesAsync(IReadOnlyCollection<decimal> resoCatIds, PermissionSnapshotDto snapshot)
        {
            if (resoCatIds.Count == 0)
            {
                return ServiceResult.Success();
            }

            var validCount = await ApplyResoCatScope(_unitOfWork.Repository<ResoCat>().Query(), snapshot)
                .Where(x => resoCatIds.Contains(x.Id))
                .Select(x => x.Id)
                .Distinct()
                .CountAsync();

            if (validCount != resoCatIds.Count)
            {
                return ServiceResult.Fail("Secilen kaynak kategorilerinden en az biri gecersiz veya kapsam disi.");
            }

            return ServiceResult.Success();
        }

        private async Task AddFunctionProductMappingsAsync(decimal functionId, IReadOnlyCollection<decimal> productIds)
        {
            if (productIds.Count == 0)
            {
                return;
            }

            var funcProdRepo = _unitOfWork.Repository<FuncProd>();
            var nextFuncProdId = (await funcProdRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;

            foreach (var productId in productIds)
            {
                await funcProdRepo.AddAsync(new FuncProd
                {
                    Id = nextFuncProdId++,
                    FunctionID = functionId,
                    ProductID = productId,
                    Deleted = 0,
                    SelectFlag = false,
                    Stamp = 1
                });
            }
        }

        private async Task ReplaceFunctionProductMappingsAsync(decimal functionId, IReadOnlyCollection<decimal> productIds)
        {
            var funcProdRepo = _unitOfWork.Repository<FuncProd>();
            var existingRows = await funcProdRepo.Query()
                .Where(x => x.FunctionID == functionId && x.Deleted == 0)
                .ToListAsync();

            foreach (var row in existingRows)
            {
                row.Deleted = row.Id;
                row.SelectFlag = false;
                row.Stamp = 1;
                funcProdRepo.Update(row);
            }

            await AddFunctionProductMappingsAsync(functionId, productIds);
        }

        private async Task AddFunctionRulesAsync(decimal functionId, IReadOnlyCollection<FunctionRuleInputDto> rules)
        {
            if (rules.Count == 0)
            {
                return;
            }

            var funcRuleRepo = _unitOfWork.Repository<FuncRule>();
            var funcResoRepo = _unitOfWork.Repository<FuncReso>();

            var nextFuncRuleId = (await funcRuleRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
            var nextFuncResoId = (await funcResoRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;

            foreach (var rule in rules)
            {
                var ruleId = nextFuncRuleId++;

                await funcRuleRepo.AddAsync(new FuncRule
                {
                    Id = ruleId,
                    FunctionID = functionId,
                    Name = rule.Name,
                    MinValue = rule.MinValue,
                    MaxValue = rule.MaxValue,
                    Deleted = 0,
                    SelectFlag = false,
                    Stamp = 1
                });

                foreach (var resoCatId in rule.ResoCatIds)
                {
                    await funcResoRepo.AddAsync(new FuncReso
                    {
                        Id = nextFuncResoId++,
                        FuncRuleID = ruleId,
                        ResoCatID = resoCatId,
                        Deleted = 0,
                        SelectFlag = false,
                        Stamp = 1
                    });
                }
            }
        }

        private async Task ReplaceFunctionRulesAsync(
            decimal functionId,
            IReadOnlyCollection<FunctionRuleInputDto> rules,
            bool reservable,
            decimal? intervalId)
        {
            var funcRuleRepo = _unitOfWork.Repository<FuncRule>();
            var funcResoRepo = _unitOfWork.Repository<FuncReso>();

            var existingRules = await funcRuleRepo.Query()
                .Where(x => x.FunctionID == functionId && x.Deleted == 0)
                .ToListAsync();

            var existingRuleIds = existingRules.Select(x => x.Id).ToList();
            foreach (var existingRule in existingRules)
            {
                existingRule.Deleted = existingRule.Id;
                existingRule.SelectFlag = false;
                existingRule.Stamp = 1;
                funcRuleRepo.Update(existingRule);
            }

            if (existingRuleIds.Count > 0)
            {
                var existingResoRows = await funcResoRepo.Query()
                    .Where(x => existingRuleIds.Contains(x.FuncRuleID) && x.Deleted == 0)
                    .ToListAsync();

                foreach (var existingResoRow in existingResoRows)
                {
                    existingResoRow.Deleted = existingResoRow.Id;
                    existingResoRow.SelectFlag = false;
                    existingResoRow.Stamp = 1;
                    funcResoRepo.Update(existingResoRow);
                }
            }

            if (!reservable || !intervalId.HasValue)
            {
                return;
            }

            await AddFunctionRulesAsync(functionId, rules);
        }

        private static ServiceResult ValidateReservationPair(bool reservable, decimal? intervalId)
        {
            var hasInterval = intervalId.HasValue;
            if (reservable != hasInterval)
            {
                return ServiceResult.Fail("Rezervasyon Uyumlu ve Rezervasyon Araligi birlikte secilmelidir.");
            }

            return ServiceResult.Success();
        }

        private static ServiceResult ValidateRules(IReadOnlyCollection<FunctionRuleInputDto> rules, bool reservable)
        {
            if (!reservable && rules.Count > 0)
            {
                return ServiceResult.Fail("Rezervasyon bilgileri secilmeden kural eklenemez.");
            }

            foreach (var rule in rules)
            {
                if (string.IsNullOrWhiteSpace(rule.Name))
                {
                    return ServiceResult.Fail("Kural adlari bos olamaz.");
                }

                if (rule.Name.Length > 32)
                {
                    return ServiceResult.Fail("Kural adi en fazla 32 karakter olabilir.");
                }

                if (rule.MinValue > rule.MaxValue)
                {
                    return ServiceResult.Fail("Kural minimum degeri maksimum degerden buyuk olamaz.");
                }

                if (rule.ResoCatIds.Count == 0)
                {
                    return ServiceResult.Fail("Her kural icin en az bir kaynak kategorisi secilmelidir.");
                }
            }

            return ServiceResult.Success();
        }

        private static List<FunctionLocalizedNameInputDto> NormalizeLocalizedNames(IEnumerable<FunctionLocalizedNameInputDto>? input)
        {
            return input?
                .Select(x => new FunctionLocalizedNameInputDto
                {
                    LanguageId = x.LanguageId,
                    Name = (x.Name ?? string.Empty).Trim()
                })
                .Where(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.LanguageId)
                .Select(x => x.First())
                .OrderBy(x => x.LanguageId)
                .ToList()
                ?? new List<FunctionLocalizedNameInputDto>();
        }

        private static List<decimal> NormalizePositiveIds(IEnumerable<decimal>? ids)
        {
            return ids?
                .Where(x => x > 0)
                .Distinct()
                .OrderBy(x => x)
                .ToList()
                ?? new List<decimal>();
        }

        private static List<FunctionRuleInputDto> NormalizeRules(IEnumerable<FunctionRuleInputDto>? rules)
        {
            return rules?
                .Select(rule => new FunctionRuleInputDto
                {
                    Name = (rule.Name ?? string.Empty).Trim(),
                    MinValue = rule.MinValue,
                    MaxValue = rule.MaxValue,
                    ResoCatIds = NormalizePositiveIds(rule.ResoCatIds)
                })
                .Where(rule => !string.IsNullOrWhiteSpace(rule.Name)
                               || rule.ResoCatIds.Count > 0
                               || rule.MinValue != 0
                               || rule.MaxValue != 0)
                .ToList()
                ?? new List<FunctionRuleInputDto>();
        }

        private static decimal? NormalizeIntervalId(decimal? intervalId)
        {
            return intervalId.HasValue && intervalId.Value > 0
                ? intervalId.Value
                : null;
        }

        private static decimal? ResolveTargetCompanyId(PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                return snapshot.CompanyId;
            }

            return snapshot.CompanyId;
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

            return query;
        }

        private static IQueryable<Product> ApplyProductScope(IQueryable<Product> query, PermissionSnapshotDto snapshot)
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
