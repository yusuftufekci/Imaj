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
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IUnitOfWork unitOfWork, ILogger<ProductService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
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
                            // If IsInvalid is true, show only invisible? Or show both?
                            // Based on typical UI: 'Geçersiz' checkbox usually means "Include Invalid" or "Show Only Invalid".
                            // If IsInvalid is false (default), we only show valid (Invisible == false).
                            // If IsInvalid true, maybe show only Invalid?
                            // Let's assume IsInvalid filter means "Match Invisible column". 
                            // If user says IsInvalid=true, they want Invalid products.
                            where p.Invisible == filter.IsInvalid

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
    }
}
