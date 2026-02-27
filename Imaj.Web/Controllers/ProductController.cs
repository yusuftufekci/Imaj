using Imaj.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Imaj.Service.Interfaces;
using Imaj.Service.DTOs;

namespace Imaj.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpPost]
        public async Task<IActionResult> Search([FromBody] ProductFilterModel? filter)
        {
            var f = filter ?? new ProductFilterModel();
            
            var filterDto = new ProductFilterDto
            {
                Code = f.Code,
                Category = IsAllOption(f.Category) ? null : f.Category,
                ProductGroup = IsAllOption(f.ProductGroup) ? null : f.ProductGroup,
                Function = IsAllOption(f.Function) ? null : f.Function,
                IsInvalid = f.IsInvalid,
                Page = f.Page,
                PageSize = f.PageSize > 0 ? f.PageSize : 10
            };

            var result = await _productService.GetByFilterAsync(filterDto);
            
            if (!result.IsSuccess || result.Data == null)
            {
                 return Json(new { items = new List<ProductSearchResult>(), totalCount = 0, page = f.Page, pageSize = f.PageSize });
            }

            var items = result.Data.Items.Select(p => new ProductSearchResult
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Category = p.CategoryName,
                ProductGroup = p.GroupName,
                Price = p.Price
            }).ToList();

            return Json(new { items, totalCount = result.Data.TotalCount, page = f.Page, pageSize = f.PageSize });
        }


        [HttpGet]
        [Route("Product/GetCategories")]
        public async Task<IActionResult> GetCategories()
        {
            var result = await _productService.GetCategoriesAsync();
            if (result.IsSuccess)
            {
                return Json(result.Data);
            }
            return BadRequest(result.Message);
        }

        [HttpGet]
        [Route("Product/GetProductGroups")]
        public async Task<IActionResult> GetProductGroups()
        {
            var result = await _productService.GetProductGroupsAsync();
            if (result.IsSuccess)
            {
                return Json(result.Data);
            }
            return BadRequest(result.Message);
        }

        [HttpGet]
        [Route("Product/GetFunctions")]
        public async Task<IActionResult> GetFunctions()
        {
            var result = await _productService.GetFunctionsAsync();
            if (result.IsSuccess)
            {
                return Json(result.Data);
            }
            return BadRequest(result.Message);
        }

        private static bool IsAllOption(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            return value.Equals("Tümü", StringComparison.OrdinalIgnoreCase)
                || value.Equals("Tumu", StringComparison.OrdinalIgnoreCase)
                || value.Equals("All", StringComparison.OrdinalIgnoreCase);
        }
    }
}
