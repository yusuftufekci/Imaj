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
    public class ProductPageService : IProductPageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentPermissionContext _currentPermissionContext;
        private readonly ILogger<ProductPageService> _logger;

        public ProductPageService(
            IUnitOfWork unitOfWork,
            ICurrentPermissionContext currentPermissionContext,
            ILogger<ProductPageService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentPermissionContext = currentPermissionContext;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResultDto<ProductPageListItemDto>>> GetProductsAsync(ProductPageFilterDto filter)
        {
            var normalizedFilter = filter ?? new ProductPageFilterDto();
            var page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            var pageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<PagedResultDto<ProductPageListItemDto>>.Success(EmptyPage<ProductPageListItemDto>(page, pageSize));
            }

            if (normalizedFilter.FunctionId.HasValue
                && normalizedFilter.FunctionId.Value > 0
                && !snapshot!.AllowedFunctionIds.Contains(normalizedFilter.FunctionId.Value))
            {
                return ServiceResult<PagedResultDto<ProductPageListItemDto>>.Success(EmptyPage<ProductPageListItemDto>(page, pageSize));
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var productQuery = ApplyProductScope(_unitOfWork.Repository<Product>().Query(), snapshot!);

            if (!string.IsNullOrWhiteSpace(normalizedFilter.Code))
            {
                var code = normalizedFilter.Code.Trim();
                productQuery = productQuery.Where(x => x.Code.Contains(code));
            }

            if (normalizedFilter.ProductCategoryId.HasValue && normalizedFilter.ProductCategoryId.Value > 0)
            {
                productQuery = productQuery.Where(x => x.ProdCatID == normalizedFilter.ProductCategoryId.Value);
            }

            if (normalizedFilter.ProductGroupId.HasValue && normalizedFilter.ProductGroupId.Value > 0)
            {
                productQuery = productQuery.Where(x => x.ProdGrpID == normalizedFilter.ProductGroupId.Value);
            }

            if (normalizedFilter.IsInvalid.HasValue)
            {
                productQuery = productQuery.Where(x => x.Invisible == normalizedFilter.IsInvalid.Value);
            }

            if (normalizedFilter.FunctionId.HasValue && normalizedFilter.FunctionId.Value > 0)
            {
                var functionId = normalizedFilter.FunctionId.Value;
                productQuery = productQuery.Where(product => _unitOfWork.Repository<ProdFunc>()
                    .Query()
                    .Any(mapping => mapping.ProductID == product.Id
                                    && mapping.Deleted == 0
                                    && mapping.FunctionID == functionId));
            }

            var query =
                from product in productQuery
                join preferredName in _unitOfWork.Repository<XProduct>().Query().Where(x => x.LanguageID == languageId)
                    on product.Id equals preferredName.ProductID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XProduct>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on product.Id equals fallbackName.ProductID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                join preferredCategoryName in _unitOfWork.Repository<XProdCat>().Query().Where(x => x.LanguageID == languageId)
                    on product.ProdCatID equals preferredCategoryName.ProdCatID into preferredCategoryGroup
                from preferredCategoryName in preferredCategoryGroup.DefaultIfEmpty()
                join fallbackCategoryName in _unitOfWork.Repository<XProdCat>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on product.ProdCatID equals fallbackCategoryName.ProdCatID into fallbackCategoryGroup
                from fallbackCategoryName in fallbackCategoryGroup.DefaultIfEmpty()
                join preferredGroupName in _unitOfWork.Repository<XProdGrp>().Query().Where(x => x.LanguageID == languageId)
                    on product.ProdGrpID equals preferredGroupName.ProdGrpID into preferredGroupNameGroup
                from preferredGroupName in preferredGroupNameGroup.DefaultIfEmpty()
                join fallbackGroupName in _unitOfWork.Repository<XProdGrp>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on product.ProdGrpID equals fallbackGroupName.ProdGrpID into fallbackGroupNameGroup
                from fallbackGroupName in fallbackGroupNameGroup.DefaultIfEmpty()
                select new ProductPageListItemDto
                {
                    Id = product.Id,
                    Code = product.Code,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    ProductCategoryId = product.ProdCatID,
                    ProductCategoryName = preferredCategoryName != null
                        ? preferredCategoryName.Name
                        : (fallbackCategoryName != null ? fallbackCategoryName.Name : string.Empty),
                    ProductGroupId = product.ProdGrpID,
                    ProductGroupName = preferredGroupName != null
                        ? preferredGroupName.Name
                        : (fallbackGroupName != null ? fallbackGroupName.Name : string.Empty),
                    Price = product.Price,
                    Invisible = product.Invisible
                };

            var first = normalizedFilter.First.HasValue && normalizedFilter.First.Value > 0 ? normalizedFilter.First.Value : (int?)null;
            IQueryable<ProductPageListItemDto> scopedQuery = query
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

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    item.Name = item.Code;
                }

                if (string.IsNullOrWhiteSpace(item.ProductCategoryName))
                {
                    item.ProductCategoryName = item.ProductCategoryId.ToString(CultureInfo.InvariantCulture);
                }

                if (string.IsNullOrWhiteSpace(item.ProductGroupName))
                {
                    item.ProductGroupName = item.ProductGroupId.ToString(CultureInfo.InvariantCulture);
                }
            }

            return ServiceResult<PagedResultDto<ProductPageListItemDto>>.Success(new PagedResultDto<ProductPageListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult<ProductPageDetailDto>> GetProductDetailAsync(decimal id)
        {
            if (id <= 0)
            {
                return ServiceResult<ProductPageDetailDto>.Fail("Urun ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<ProductPageDetailDto>.NotFound("Urun bulunamadi.");
            }
            var scopedSnapshot = snapshot!;

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var row = await ApplyProductScope(_unitOfWork.Repository<Product>().Query(), scopedSnapshot)
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.CompanyID,
                    x.Code,
                    x.ProdCatID,
                    x.ProdGrpID,
                    x.Price,
                    x.Invisible
                })
                .SingleOrDefaultAsync();

            if (row == null)
            {
                return ServiceResult<ProductPageDetailDto>.NotFound("Urun bulunamadi.");
            }

            var names = await (
                from localizedName in _unitOfWork.Repository<XProduct>().Query()
                join language in _unitOfWork.Repository<Language>().Query()
                    on localizedName.LanguageID equals language.Id into languageGroup
                from language in languageGroup.DefaultIfEmpty()
                where localizedName.ProductID == row.Id
                orderby localizedName.LanguageID
                select new ProductPageLocalizedNameDto
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

            var productCategoryName = await _unitOfWork.Repository<XProdCat>()
                .Query()
                .Where(x => x.ProdCatID == row.ProdCatID
                            && (x.LanguageID == languageId || x.LanguageID == fallbackLanguageId))
                .OrderBy(x => x.LanguageID == languageId ? 0 : 1)
                .Select(x => x.Name)
                .FirstOrDefaultAsync() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(productCategoryName))
            {
                productCategoryName = row.ProdCatID.ToString(CultureInfo.InvariantCulture);
            }

            var productGroupName = await _unitOfWork.Repository<XProdGrp>()
                .Query()
                .Where(x => x.ProdGrpID == row.ProdGrpID
                            && (x.LanguageID == languageId || x.LanguageID == fallbackLanguageId))
                .OrderBy(x => x.LanguageID == languageId ? 0 : 1)
                .Select(x => x.Name)
                .FirstOrDefaultAsync() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(productGroupName))
            {
                productGroupName = row.ProdGrpID.ToString(CultureInfo.InvariantCulture);
            }

            var functionIds = await _unitOfWork.Repository<ProdFunc>()
                .Query()
                .Where(x => x.ProductID == row.Id
                            && x.Deleted == 0
                            && scopedSnapshot.AllowedFunctionIds.Contains(x.FunctionID))
                .Select(x => x.FunctionID)
                .Distinct()
                .ToListAsync();

            var functions = await BuildFunctionOptionQuery(scopedSnapshot, languageId, fallbackLanguageId)
                .Where(x => functionIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToListAsync();

            foreach (var function in functions.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                function.Name = function.Id.ToString(CultureInfo.InvariantCulture);
            }

            return ServiceResult<ProductPageDetailDto>.Success(new ProductPageDetailDto
            {
                Id = row.Id,
                CompanyId = row.CompanyID,
                Code = row.Code,
                ProductCategoryId = row.ProdCatID,
                ProductCategoryName = productCategoryName,
                ProductGroupId = row.ProdGrpID,
                ProductGroupName = productGroupName,
                Price = row.Price,
                Invisible = row.Invisible,
                Names = names,
                Functions = functions
            });
        }

        public async Task<ServiceResult<List<ProductPageLanguageDto>>> GetLanguagesAsync()
        {
            var items = await _unitOfWork.Repository<Language>()
                .Query()
                .OrderBy(x => x.Sequence)
                .ThenBy(x => x.Id)
                .Select(x => new ProductPageLanguageDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            return ServiceResult<List<ProductPageLanguageDto>>.Success(items);
        }

        public async Task<ServiceResult<List<ProductPageCategoryOptionDto>>> GetProductCategoryOptionsAsync()
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<List<ProductPageCategoryOptionDto>>.Success(new List<ProductPageCategoryOptionDto>());
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var query = BuildProductCategoryOptionQuery(snapshot!, languageId, fallbackLanguageId);
            var items = await query
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToListAsync();

            foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                item.Name = item.Id.ToString(CultureInfo.InvariantCulture);
            }

            return ServiceResult<List<ProductPageCategoryOptionDto>>.Success(items);
        }

        public async Task<ServiceResult<List<ProductPageGroupOptionDto>>> GetProductGroupOptionsAsync()
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<List<ProductPageGroupOptionDto>>.Success(new List<ProductPageGroupOptionDto>());
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var query = BuildProductGroupOptionQuery(snapshot!, languageId, fallbackLanguageId);
            var items = await query
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToListAsync();

            foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                item.Name = item.Id.ToString(CultureInfo.InvariantCulture);
            }

            return ServiceResult<List<ProductPageGroupOptionDto>>.Success(items);
        }

        public async Task<ServiceResult<PagedResultDto<ProductPageFunctionOptionDto>>> SearchFunctionsAsync(ProductPageFunctionLookupFilterDto filter)
        {
            var normalizedFilter = filter ?? new ProductPageFunctionLookupFilterDto();
            var page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            var pageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 10;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<PagedResultDto<ProductPageFunctionOptionDto>>.Success(EmptyPage<ProductPageFunctionOptionDto>(page, pageSize));
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var query = BuildFunctionOptionQuery(snapshot!, languageId, fallbackLanguageId);

            if (!string.IsNullOrWhiteSpace(normalizedFilter.Name))
            {
                var name = normalizedFilter.Name.Trim();
                query = query.Where(x => x.Name.Contains(name));
            }

            if (normalizedFilter.IsInvalid.HasValue)
            {
                query = query.Where(x => x.Invisible == normalizedFilter.IsInvalid.Value);
            }

            var excludedIds = NormalizePositiveIds(normalizedFilter.ExcludeIds);
            if (excludedIds.Count > 0)
            {
                query = query.Where(x => !excludedIds.Contains(x.Id));
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

            return ServiceResult<PagedResultDto<ProductPageFunctionOptionDto>>.Success(new PagedResultDto<ProductPageFunctionOptionDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult> CreateProductAsync(ProductPageUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Urun bilgisi bos olamaz.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Urun kaydi icin yetki kapsami bulunamadi.");
            }

            var targetCompanyId = ResolveTargetCompanyId(snapshot!);
            if (!targetCompanyId.HasValue)
            {
                return ServiceResult.Fail("Company kapsam bilgisi olmadigi icin urun olusturulamadi.");
            }

            var normalizedCode = NormalizeCode(input.Code);
            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                return ServiceResult.Fail("Urun kodu zorunludur.");
            }

            if (normalizedCode.Length > 8)
            {
                return ServiceResult.Fail("Kod en fazla 8 karakter olabilir.");
            }

            if (input.ProductCategoryId <= 0)
            {
                return ServiceResult.Fail("Urun kategorisi secimi zorunludur.");
            }

            if (input.ProductGroupId <= 0)
            {
                return ServiceResult.Fail("Urun grubu secimi zorunludur.");
            }

            if (input.Price < 0)
            {
                return ServiceResult.Fail("Fiyat sifirdan kucuk olamaz.");
            }

            var normalizedNames = NormalizeLocalizedNames(input.Names);
            if (normalizedNames.Count == 0)
            {
                return ServiceResult.Fail("En az bir dilde ad girilmelidir.");
            }

            var normalizedFunctionIds = NormalizePositiveIds(input.FunctionIds);
            if (normalizedFunctionIds.Count == 0)
            {
                return ServiceResult.Fail("En az bir fonksiyon secilmelidir.");
            }

            var languageValidation = await ValidateLanguagesAsync(normalizedNames.Select(x => x.LanguageId));
            if (!languageValidation.IsSuccess)
            {
                return languageValidation;
            }

            var categoryValidation = await ValidateProductCategoryAsync(input.ProductCategoryId, snapshot!);
            if (!categoryValidation.IsSuccess)
            {
                return categoryValidation;
            }

            var groupValidation = await ValidateProductGroupAsync(input.ProductGroupId, snapshot!);
            if (!groupValidation.IsSuccess)
            {
                return groupValidation;
            }

            var functionValidation = await ValidateFunctionsAsync(normalizedFunctionIds, snapshot!);
            if (!functionValidation.IsSuccess)
            {
                return functionValidation;
            }

            var duplicateExists = await ApplyProductCompanyScope(_unitOfWork.Repository<Product>().Query(), snapshot!, targetCompanyId.Value)
                .AnyAsync(x => x.Code == normalizedCode);
            if (duplicateExists)
            {
                return ServiceResult.Fail("Ayni kod ile baska bir urun kaydi mevcut.");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var productRepo = _unitOfWork.Repository<Product>();
                var xProductRepo = _unitOfWork.Repository<XProduct>();

                var nextProductId = (await productRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                await productRepo.AddAsync(new Product
                {
                    Id = nextProductId,
                    CompanyID = targetCompanyId.Value,
                    ProdCatID = input.ProductCategoryId,
                    ProdGrpID = input.ProductGroupId,
                    Code = normalizedCode,
                    Price = input.Price,
                    Invisible = input.Invisible,
                    SelectFlag = false,
                    SelectQty = 0,
                    Stamp = 1
                });

                var nextXProductId = (await xProductRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xProductRepo.AddAsync(new XProduct
                    {
                        Id = nextXProductId++,
                        ProductID = nextProductId,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        Stamp = 1
                    });
                }

                await AddProductFunctionMappingsAsync(nextProductId, normalizedFunctionIds);

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Urun kaydedildi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Urun kaydedilirken hata olustu.");
                return ServiceResult.Fail("Urun kaydedilemedi.");
            }
        }

        public async Task<ServiceResult> UpdateProductAsync(ProductPageUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Urun bilgisi bos olamaz.");
            }

            if (!input.Id.HasValue || input.Id.Value <= 0)
            {
                return ServiceResult.Fail("Urun ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Urun guncelleme icin yetki kapsami bulunamadi.");
            }

            var normalizedCode = NormalizeCode(input.Code);
            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                return ServiceResult.Fail("Urun kodu zorunludur.");
            }

            if (normalizedCode.Length > 8)
            {
                return ServiceResult.Fail("Kod en fazla 8 karakter olabilir.");
            }

            if (input.ProductCategoryId <= 0)
            {
                return ServiceResult.Fail("Urun kategorisi secimi zorunludur.");
            }

            if (input.ProductGroupId <= 0)
            {
                return ServiceResult.Fail("Urun grubu secimi zorunludur.");
            }

            if (input.Price < 0)
            {
                return ServiceResult.Fail("Fiyat sifirdan kucuk olamaz.");
            }

            var normalizedNames = NormalizeLocalizedNames(input.Names);
            if (normalizedNames.Count == 0)
            {
                return ServiceResult.Fail("En az bir dilde ad girilmelidir.");
            }

            var normalizedFunctionIds = NormalizePositiveIds(input.FunctionIds);
            if (normalizedFunctionIds.Count == 0)
            {
                return ServiceResult.Fail("En az bir fonksiyon secilmelidir.");
            }

            var languageValidation = await ValidateLanguagesAsync(normalizedNames.Select(x => x.LanguageId));
            if (!languageValidation.IsSuccess)
            {
                return languageValidation;
            }

            var categoryValidation = await ValidateProductCategoryAsync(input.ProductCategoryId, snapshot!);
            if (!categoryValidation.IsSuccess)
            {
                return categoryValidation;
            }

            var groupValidation = await ValidateProductGroupAsync(input.ProductGroupId, snapshot!);
            if (!groupValidation.IsSuccess)
            {
                return groupValidation;
            }

            var functionValidation = await ValidateFunctionsAsync(normalizedFunctionIds, snapshot!);
            if (!functionValidation.IsSuccess)
            {
                return functionValidation;
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var productRepo = _unitOfWork.Repository<Product>();
                var xProductRepo = _unitOfWork.Repository<XProduct>();

                var product = await ApplyProductScope(productRepo.Query(), snapshot!)
                    .SingleOrDefaultAsync(x => x.Id == input.Id.Value);
                if (product == null)
                {
                    return ServiceResult.NotFound("Urun bulunamadi.");
                }

                var duplicateExists = await ApplyProductCompanyScope(productRepo.Query(), snapshot!, product.CompanyID)
                    .AnyAsync(x => x.Id != product.Id && x.Code == normalizedCode);
                if (duplicateExists)
                {
                    return ServiceResult.Fail("Ayni kod ile baska bir urun kaydi mevcut.");
                }

                product.Code = normalizedCode;
                product.ProdCatID = input.ProductCategoryId;
                product.ProdGrpID = input.ProductGroupId;
                product.Price = input.Price;
                product.Invisible = input.Invisible;
                product.Stamp = 1;
                productRepo.Update(product);

                var existingNames = await xProductRepo.Query()
                    .Where(x => x.ProductID == product.Id)
                    .ToListAsync();

                foreach (var existingName in existingNames)
                {
                    xProductRepo.Remove(existingName);
                }

                var nextXProductId = (await xProductRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                foreach (var localizedName in normalizedNames)
                {
                    await xProductRepo.AddAsync(new XProduct
                    {
                        Id = nextXProductId++,
                        ProductID = product.Id,
                        LanguageID = localizedName.LanguageId,
                        Name = localizedName.Name,
                        Stamp = 1
                    });
                }

                await ReplaceProductFunctionMappingsAsync(product.Id, normalizedFunctionIds, snapshot!);

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Urun guncellendi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Urun guncellenirken hata olustu. ProductID={ProductId}", input.Id.Value);
                return ServiceResult.Fail("Urun guncellenemedi.");
            }
        }

        private IQueryable<ProductPageCategoryOptionDto> BuildProductCategoryOptionQuery(
            PermissionSnapshotDto snapshot,
            decimal languageId,
            decimal fallbackLanguageId)
        {
            var categoryQuery = ApplyProductCategoryScope(_unitOfWork.Repository<ProdCat>().Query(), snapshot);

            return
                from category in categoryQuery
                join preferredName in _unitOfWork.Repository<XProdCat>().Query().Where(x => x.LanguageID == languageId)
                    on category.Id equals preferredName.ProdCatID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XProdCat>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on category.Id equals fallbackName.ProdCatID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                select new ProductPageCategoryOptionDto
                {
                    Id = category.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    Invisible = category.Invisible
                };
        }

        private IQueryable<ProductPageGroupOptionDto> BuildProductGroupOptionQuery(
            PermissionSnapshotDto snapshot,
            decimal languageId,
            decimal fallbackLanguageId)
        {
            var groupQuery = ApplyProductGroupScope(_unitOfWork.Repository<ProdGrp>().Query(), snapshot);

            return
                from productGroup in groupQuery
                join preferredName in _unitOfWork.Repository<XProdGrp>().Query().Where(x => x.LanguageID == languageId)
                    on productGroup.Id equals preferredName.ProdGrpID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XProdGrp>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on productGroup.Id equals fallbackName.ProdGrpID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                select new ProductPageGroupOptionDto
                {
                    Id = productGroup.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    Invisible = productGroup.Invisible
                };
        }

        private IQueryable<ProductPageFunctionOptionDto> BuildFunctionOptionQuery(
            PermissionSnapshotDto snapshot,
            decimal languageId,
            decimal fallbackLanguageId)
        {
            var functionQuery = ApplyFunctionScope(_unitOfWork.Repository<Function>().Query(), snapshot);

            return
                from function in functionQuery
                join preferredName in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == languageId)
                    on function.Id equals preferredName.FunctionID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on function.Id equals fallbackName.FunctionID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                select new ProductPageFunctionOptionDto
                {
                    Id = function.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    Invisible = function.Invisible
                };
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

        private async Task<ServiceResult> ValidateFunctionsAsync(IReadOnlyCollection<decimal> functionIds, PermissionSnapshotDto snapshot)
        {
            if (functionIds.Count == 0)
            {
                return ServiceResult.Fail("En az bir fonksiyon secilmelidir.");
            }

            var validCount = await ApplyFunctionScope(_unitOfWork.Repository<Function>().Query(), snapshot)
                .Where(x => functionIds.Contains(x.Id))
                .CountAsync();

            if (validCount != functionIds.Count)
            {
                return ServiceResult.Fail("Secilen fonksiyonlardan en az biri gecersiz veya kapsam disi.");
            }

            return ServiceResult.Success();
        }

        private async Task<ServiceResult> ValidateProductCategoryAsync(decimal productCategoryId, PermissionSnapshotDto snapshot)
        {
            var exists = await ApplyProductCategoryScope(_unitOfWork.Repository<ProdCat>().Query(), snapshot)
                .AnyAsync(x => x.Id == productCategoryId);

            if (!exists)
            {
                return ServiceResult.Fail("Secilen urun kategorisi gecersiz veya kapsam disi.");
            }

            return ServiceResult.Success();
        }

        private async Task<ServiceResult> ValidateProductGroupAsync(decimal productGroupId, PermissionSnapshotDto snapshot)
        {
            var exists = await ApplyProductGroupScope(_unitOfWork.Repository<ProdGrp>().Query(), snapshot)
                .AnyAsync(x => x.Id == productGroupId);

            if (!exists)
            {
                return ServiceResult.Fail("Secilen urun grubu gecersiz veya kapsam disi.");
            }

            return ServiceResult.Success();
        }

        private async Task ReplaceProductFunctionMappingsAsync(decimal productId, IReadOnlyCollection<decimal> functionIds, PermissionSnapshotDto snapshot)
        {
            var productFuncRepo = _unitOfWork.Repository<ProdFunc>();

            var existingMappings = await productFuncRepo.Query()
                .Where(x => x.ProductID == productId
                            && x.Deleted == 0
                            && snapshot.AllowedFunctionIds.Contains(x.FunctionID))
                .ToListAsync();

            foreach (var mapping in existingMappings)
            {
                mapping.Deleted = mapping.Id;
                mapping.Stamp = 1;
                productFuncRepo.Update(mapping);
            }

            await AddProductFunctionMappingsAsync(productId, functionIds);
        }

        private async Task AddProductFunctionMappingsAsync(decimal productId, IReadOnlyCollection<decimal> functionIds)
        {
            if (functionIds.Count == 0)
            {
                return;
            }

            var productFuncRepo = _unitOfWork.Repository<ProdFunc>();
            var nextId = (await productFuncRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;

            foreach (var functionId in functionIds)
            {
                await productFuncRepo.AddAsync(new ProdFunc
                {
                    Id = nextId++,
                    ProductID = productId,
                    FunctionID = functionId,
                    Deleted = 0,
                    SelectFlag = false,
                    Stamp = 1
                });
            }
        }

        private static string NormalizeCode(string? code)
        {
            var normalized = string.IsNullOrWhiteSpace(code)
                ? string.Empty
                : code.Trim().ToUpperInvariant();

            if (normalized.Length > 8)
            {
                normalized = normalized.Substring(0, 8);
            }

            return normalized;
        }

        private static List<ProductPageLocalizedNameInputDto> NormalizeLocalizedNames(IEnumerable<ProductPageLocalizedNameInputDto>? input)
        {
            return input?
                .Select(x => new ProductPageLocalizedNameInputDto
                {
                    LanguageId = x.LanguageId,
                    Name = (x.Name ?? string.Empty).Trim()
                })
                .Where(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.LanguageId)
                .Select(x => x.First())
                .OrderBy(x => x.LanguageId)
                .ToList()
                ?? new List<ProductPageLocalizedNameInputDto>();
        }

        private static List<decimal> NormalizePositiveIds(IEnumerable<decimal>? ids)
        {
            return ids?
                .Where(x => x > 0)
                .Distinct()
                .ToList()
                ?? new List<decimal>();
        }

        private IQueryable<Product> ApplyProductScope(IQueryable<Product> query, PermissionSnapshotDto snapshot)
        {
            query = ApplyProductCompanyScope(query, snapshot, snapshot.CompanyId);

            if (snapshot.AllowedFunctionIds.Count == 0)
            {
                return query.Where(_ => false);
            }

            var allowedFunctionIds = snapshot.AllowedFunctionIds;
            return query.Where(product => _unitOfWork.Repository<ProdFunc>()
                .Query()
                .Any(mapping => mapping.ProductID == product.Id
                                && mapping.Deleted == 0
                                && allowedFunctionIds.Contains(mapping.FunctionID)));
        }

        private static IQueryable<Product> ApplyProductCompanyScope(IQueryable<Product> query, PermissionSnapshotDto snapshot, decimal? explicitCompanyId)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                var companyId = explicitCompanyId ?? snapshot.CompanyId;
                if (!companyId.HasValue)
                {
                    return query.Where(_ => false);
                }

                query = query.Where(x => x.CompanyID == companyId.Value);
            }

            return query;
        }

        private static IQueryable<ProdCat> ApplyProductCategoryScope(IQueryable<ProdCat> query, PermissionSnapshotDto snapshot)
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

        private static IQueryable<ProdGrp> ApplyProductGroupScope(IQueryable<ProdGrp> query, PermissionSnapshotDto snapshot)
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

            if (snapshot.AllowedFunctionIds.Count == 0)
            {
                return query.Where(_ => false);
            }

            query = query.Where(x => snapshot.AllowedFunctionIds.Contains(x.Id));
            return query;
        }

        private static decimal? ResolveTargetCompanyId(PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                return snapshot.CompanyId;
            }

            return snapshot.CompanyId;
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
