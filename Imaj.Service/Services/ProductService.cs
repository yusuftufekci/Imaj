using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Imaj.Core.Entities;
using Imaj.Core.Extensions;
using Imaj.Core.Guards;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Service.DTOs;
using Imaj.Service.DTOs.Security;
using Imaj.Service.Interfaces;
using Imaj.Service.Results; // Added this

namespace Imaj.Service.Services
{
    public class ProductService : BaseService, IProductService
    {
        private readonly ICurrentPermissionContext _currentPermissionContext;

        public ProductService(
            IUnitOfWork unitOfWork,
            ILogger<ProductService> logger,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            ICurrentPermissionContext currentPermissionContext)
            : base(unitOfWork, logger, configuration)
        {
            _currentPermissionContext = currentPermissionContext;
        }

        public async Task<ServiceResult<PagedResult<ProductDto>>> GetByFilterAsync(ProductFilterDto filter)
        {
            Guard.AgainstNull(filter, nameof(filter));

            try
            {
                var languageId = CurrentLanguageId;
                var snapshot = await _currentPermissionContext.GetSnapshotAsync();
                if (IsDataScopeDenied(snapshot))
                {
                    return ServiceResult<PagedResult<ProductDto>>.Success(new PagedResult<ProductDto>
                    {
                        Items = new List<ProductDto>(),
                        TotalCount = 0,
                        PageNumber = filter.Page > 0 ? filter.Page : 1,
                        PageSize = filter.PageSize > 0 ? filter.PageSize : 10
                    });
                }

                var activeSnapshot = snapshot!;
                var products = _unitOfWork.Repository<Product>().Query();
                if (activeSnapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound && activeSnapshot.CompanyId.HasValue)
                {
                    products = products.Where(p => p.CompanyID == activeSnapshot.CompanyId.Value);
                }

                var query = from p in products
                            join xp in _unitOfWork.Repository<XProduct>().Query().Where(x => x.LanguageID == languageId)
                                on p.Id equals xp.ProductID
                            
                            join pc in _unitOfWork.Repository<ProdCat>().Query() on p.ProdCatID equals pc.Id
                            join xpc in _unitOfWork.Repository<XProdCat>().Query().Where(x => x.LanguageID == languageId)
                                on pc.Id equals xpc.ProdCatID
                            
                            join pg in _unitOfWork.Repository<ProdGrp>().Query() on p.ProdGrpID equals pg.Id
                            join xpg in _unitOfWork.Repository<XProdGrp>().Query().Where(x => x.LanguageID == languageId)
                                on pg.Id equals xpg.ProdGrpID
                            
                            // Filter IsInvalid (Geçersiz) logic:
                            where (!filter.IsInvalid.HasValue || p.Invisible == filter.IsInvalid.Value)

                            select new ProductDto
                            {
                                Id = p.Id,
                                Code = p.Code,
                                Name = xp.Name,
                                Price = p.Price,
                                CategoryId = pc.Id,
                                CategoryName = xpc.Name,
                                GroupId = pg.Id,
                                GroupName = xpg.Name
                            };

                // Apply Filters
                if (!string.IsNullOrEmpty(filter.Code))
                    query = query.Where(x => x.Code != null && x.Code.Contains(filter.Code));

                // Filter by Category Name (since filter passes string)
                if (!string.IsNullOrEmpty(filter.Category))
                    query = query.Where(x => x.CategoryName != null && x.CategoryName.Contains(filter.Category));

                // Filter by Product Group Name
                if (!string.IsNullOrEmpty(filter.ProductGroup))
                    query = query.Where(x => x.GroupName != null && x.GroupName.Contains(filter.ProductGroup));

                var page = filter.Page > 0 ? filter.Page : 1;
                var pageSize = filter.PageSize > 0 ? filter.PageSize : 10;

                // Helper to get total count
                var totalCount = await query.CountAsync();

                // Pagination
                var items = await query
                    .OrderBy(x => x.Code)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = new PagedResult<ProductDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                };

                return ServiceResult<PagedResult<ProductDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürünler listelenirken hata oluştu.");
                return ServiceResult<PagedResult<ProductDto>>.Fail("Ürünler listelenirken hata oluştu.");
            }
        }

        public async Task<ServiceResult<List<ProductCategoryDto>>> GetCategoriesAsync()
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsDataScopeDenied(snapshot))
            {
                return ServiceResult<List<ProductCategoryDto>>.Success(new List<ProductCategoryDto>());
            }

            var activeSnapshot = snapshot!;
            var categories = _unitOfWork.Repository<ProdCat>().Query()
                .Where(pc => !pc.Invisible);

            if (activeSnapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound && activeSnapshot.CompanyId.HasValue)
            {
                categories = categories.Where(pc => pc.CompanyID == activeSnapshot.CompanyId.Value);
            }

            var query = from pc in categories
                        join xpc in _unitOfWork.Repository<XProdCat>().Query()
                            on pc.Id equals xpc.ProdCatID
                        where xpc.LanguageID == CurrentLanguageId
                        orderby pc.Sequence
                        select new ProductCategoryDto
                        {
                            Id = pc.Id,
                            Name = xpc.Name,
                            TaxTypeId = pc.TaxTypeID,
                            Sequence = pc.Sequence
                        };

            return ServiceResult<List<ProductCategoryDto>>.Success(await query.ToListAsync());
        }

        public async Task<ServiceResult<List<ProductGroupDto>>> GetProductGroupsAsync()
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsDataScopeDenied(snapshot))
            {
                return ServiceResult<List<ProductGroupDto>>.Success(new List<ProductGroupDto>());
            }

            var activeSnapshot = snapshot!;
            var groups = _unitOfWork.Repository<ProdGrp>().Query()
                .Where(pg => !pg.Invisible);

            if (activeSnapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound && activeSnapshot.CompanyId.HasValue)
            {
                groups = groups.Where(pg => pg.CompanyID == activeSnapshot.CompanyId.Value);
            }

            var query = from pg in groups
                        join xpg in _unitOfWork.Repository<XProdGrp>().Query()
                            on pg.Id equals xpg.ProdGrpID
                        where xpg.LanguageID == CurrentLanguageId
                        orderby xpg.Name
                        select new ProductGroupDto
                        {
                            Id = pg.Id,
                            Name = xpg.Name
                        };

            return ServiceResult<List<ProductGroupDto>>.Success(await query.ToListAsync());
        }

        public async Task<ServiceResult<List<FunctionDto>>> GetFunctionsAsync()
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsDataScopeDenied(snapshot))
            {
                return ServiceResult<List<FunctionDto>>.Success(new List<FunctionDto>());
            }

            var activeSnapshot = snapshot!;
            var functions = _unitOfWork.Repository<Function>().Query()
                .Where(f => !f.Invisible && activeSnapshot.AllowedFunctionIds.Contains(f.Id));

            if (activeSnapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound && activeSnapshot.CompanyId.HasValue)
            {
                functions = functions.Where(f => f.CompanyID == activeSnapshot.CompanyId.Value);
            }

            var query = from f in functions
                        join xf in _unitOfWork.Repository<XFunction>().Query()
                            on f.Id equals xf.FunctionID
                        where xf.LanguageID == CurrentLanguageId
                        orderby xf.Name
                        select new FunctionDto
                        {
                            Id = f.Id,
                            Name = xf.Name
                        };

            return ServiceResult<List<FunctionDto>>.Success(await query.ToListAsync());
        }

        private static bool IsDataScopeDenied(PermissionSnapshotDto? snapshot)
        {
            return snapshot == null
                   || snapshot.IsDenied
                   || snapshot.CompanyScopeMode == CompanyScopeMode.Deny
                   || snapshot.AllowedFunctionIds.Count == 0;
        }
    }
}
