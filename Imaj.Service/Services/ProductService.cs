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
using Imaj.Service.Interfaces;
using Imaj.Service.Results; // Added this

namespace Imaj.Service.Services
{
    public class ProductService : BaseService, IProductService
    {
        public ProductService(IUnitOfWork unitOfWork, ILogger<ProductService> logger, Microsoft.Extensions.Configuration.IConfiguration configuration)
            : base(unitOfWork, logger, configuration)
        {
        }

        public async Task<ServiceResult<PagedResult<ProductDto>>> GetByFilterAsync(ProductFilterDto filter)
        {
            Guard.AgainstNull(filter, nameof(filter));

            try
            {
                var query = from p in _unitOfWork.Repository<Product>().Query()
                            join xp in _unitOfWork.Repository<XProduct>().Query().Where(x => x.LanguageID == 1) 
                                on p.Id equals xp.ProductID
                            
                            join pc in _unitOfWork.Repository<ProdCat>().Query() on p.ProdCatID equals pc.Id
                            join xpc in _unitOfWork.Repository<XProdCat>().Query().Where(x => x.LanguageID == 1) 
                                on pc.Id equals xpc.ProdCatID
                            
                            join pg in _unitOfWork.Repository<ProdGrp>().Query() on p.ProdGrpID equals pg.Id
                            join xpg in _unitOfWork.Repository<XProdGrp>().Query().Where(x => x.LanguageID == 1) 
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
                    query = query.Where(x => x.Code.Contains(filter.Code));

                // Filter by Category Name (since filter passes string)
                if (!string.IsNullOrEmpty(filter.Category))
                    query = query.Where(x => x.CategoryName != null && x.CategoryName.Contains(filter.Category));

                // Filter by Product Group Name
                if (!string.IsNullOrEmpty(filter.ProductGroup))
                    query = query.Where(x => x.GroupName != null && x.GroupName.Contains(filter.ProductGroup));

                // Helper to get total count
                var totalCount = await query.CountAsync();

                // Pagination
                var items = await query
                    .OrderBy(x => x.Code)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                var result = new PagedResult<ProductDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = filter.Page,
                    PageSize = filter.PageSize
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
            return await GetTranslatedListAsync<ProdCat, XProdCat, ProductCategoryDto>(
                pc => pc.Id,
                xpc => xpc.ProdCatID,
                (pc, xpc) => new ProductCategoryDto
                {
                    Id = pc.Id,
                    Name = xpc.Name,
                    TaxTypeId = pc.TaxTypeID,
                    Sequence = pc.Sequence
                },
                orderBySelector: dto => dto.Sequence
            );
        }

        public async Task<ServiceResult<List<ProductGroupDto>>> GetProductGroupsAsync()
        {
            return await GetTranslatedListAsync<ProdGrp, XProdGrp, ProductGroupDto>(
                pg => pg.Id,
                xpg => xpg.ProdGrpID,
                (pg, xpg) => new ProductGroupDto { Id = pg.Id, Name = xpg.Name }
            );
        }

        public async Task<ServiceResult<List<FunctionDto>>> GetFunctionsAsync()
        {
            return await GetTranslatedListAsync<Function, XFunction, FunctionDto>(
                f => f.Id,
                xf => xf.FunctionID,
                (f, xf) => new FunctionDto { Id = f.Id, Name = xf.Name }
            );
        }
    }
}
