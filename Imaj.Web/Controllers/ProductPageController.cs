using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Web.Authorization;
using Imaj.Web.Controllers.Base;
using Imaj.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Imaj.Web.Controllers
{
    [Route("Product")]
    public class ProductPageController : BaseController
    {
        private const double ViewMethodId = 1272d;
        private const double EditMethodId = 1273d;
        private const double BrowseMethodId = 1274d;
        private const double AddMethodId = 1275d;

        private readonly IProductPageService _productPageService;

        public ProductPageController(IProductPageService productPageService, ILogger<ProductPageController> logger)
            : base(logger)
        {
            _productPageService = productPageService;
        }

        [HttpGet("")]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> Index()
        {
            var categoryOptions = await GetProductCategoryOptionsAsync();
            var groupOptions = await GetProductGroupOptionsAsync();
            var functionOptions = await GetFunctionOptionsAsync();

            return View(new ProductPageIndexViewModel
            {
                Filter = new ProductPageFilterModel
                {
                    Page = 1,
                    PageSize = 16
                },
                ProductCategoryOptions = categoryOptions,
                ProductGroupOptions = groupOptions,
                FunctionOptions = functionOptions
            });
        }

        [HttpGet("List")]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> List(ProductPageFilterModel filter)
        {
            var normalizedFilter = filter ?? new ProductPageFilterModel();
            normalizedFilter.Page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            normalizedFilter.PageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;

            var result = await _productPageService.GetProductsAsync(new ProductPageFilterDto
            {
                Code = normalizedFilter.Code,
                ProductCategoryId = normalizedFilter.ProductCategoryId,
                ProductGroupId = normalizedFilter.ProductGroupId,
                FunctionId = normalizedFilter.FunctionId,
                IsInvalid = normalizedFilter.IsInvalid,
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize
            });

            var model = new ProductPageListViewModel
            {
                Items = result.IsSuccess && result.Data != null
                    ? result.Data.Items.Select(MapListItem).ToList()
                    : new List<ProductPageListItemViewModel>(),
                Page = result.Data?.Page ?? normalizedFilter.Page,
                PageSize = result.Data?.PageSize ?? normalizedFilter.PageSize,
                TotalCount = result.Data?.TotalCount ?? 0,
                Filter = normalizedFilter,
                ReturnUrl = BuildCurrentReturnUrl()
            };

            return View(model);
        }

        [HttpGet("Detail")]
        [HttpPost("Detail")]
        [RequireMethodPermission(ViewMethodId)]
        public async Task<IActionResult> Detail(decimal? id, string[]? selectedIds = null, int currentIndex = 0, string? returnUrl = null)
        {
            var resolved = ResolveSelection(id, selectedIds, currentIndex);
            if (!resolved.ResolvedId.HasValue)
            {
                return RedirectToAction("Index");
            }

            var detailResult = await _productPageService.GetProductDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? "Urun bulunamadi.");
                return RedirectToAction("List");
            }

            var model = MapDetail(detailResult.Data);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/Product/List");

            return View(model);
        }

        [HttpGet("Edit")]
        [HttpPost("Edit")]
        [RequireMethodPermission(EditMethodId)]
        public async Task<IActionResult> Edit(decimal? id, string[]? selectedIds = null, int currentIndex = 0, string? returnUrl = null)
        {
            var resolved = ResolveSelection(id, selectedIds, currentIndex);
            if (!resolved.ResolvedId.HasValue)
            {
                return RedirectToAction("Index");
            }

            var detailResult = await _productPageService.GetProductDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? "Urun bulunamadi.");
                return RedirectToAction("List");
            }

            var model = MapEdit(detailResult.Data);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/Product/List");

            await EnsureEditorDependenciesAsync(model);
            return View(model);
        }

        [HttpGet("Create")]
        [RequireMethodPermission(AddMethodId)]
        public async Task<IActionResult> Create(decimal? productCategoryId = null, decimal? productGroupId = null, string? code = null)
        {
            var model = new ProductPageCreateViewModel
            {
                Code = NormalizeCode(code),
                ProductCategoryId = productCategoryId.GetValueOrDefault(),
                ProductGroupId = productGroupId.GetValueOrDefault(),
                Price = 0
            };

            await EnsureEditorDependenciesAsync(model);
            return View(model);
        }

        [HttpGet("SearchFunctions")]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> SearchFunctions([FromQuery] ProductPageFunctionLookupFilterModel filter)
        {
            var normalizedFilter = filter ?? new ProductPageFunctionLookupFilterModel();

            var result = await _productPageService.SearchFunctionsAsync(new ProductPageFunctionLookupFilterDto
            {
                Name = normalizedFilter.Name,
                IsInvalid = normalizedFilter.IsInvalid,
                ExcludeIds = ParseDecimalCsv(normalizedFilter.ExcludeIds),
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize
            });

            if (!result.IsSuccess || result.Data == null)
            {
                return BadRequest(result.Message ?? "Fonksiyon listesi alinamadi.");
            }

            return Json(new
            {
                items = result.Data.Items,
                totalCount = result.Data.TotalCount,
                page = result.Data.Page,
                pageSize = result.Data.PageSize
            });
        }

        [HttpPost("Save")]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(AddMethodId, write: true)]
        public async Task<IActionResult> Save(ProductPageCreateViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);
            model.Code = NormalizeCode(model.Code);
            ValidateEditorModel(model);

            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }

            var result = await _productPageService.CreateProductAsync(MapToUpsert(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Urun kaydedilemedi.");
                return View("Create", model);
            }

            ShowSuccess(result.Message ?? "Urun kaydedildi.");
            if (model.AutomaticForward)
            {
                return RedirectToAction("Create", new
                {
                    code = model.Code,
                    productCategoryId = model.ProductCategoryId,
                    productGroupId = model.ProductGroupId
                });
            }

            return RedirectToAction("Index");
        }

        [HttpPost("Update")]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(EditMethodId, write: true)]
        public async Task<IActionResult> Update(ProductPageEditViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);
            model.Code = NormalizeCode(model.Code);
            ValidateEditorModel(model);

            if (!ModelState.IsValid)
            {
                return View("Edit", model);
            }

            var result = await _productPageService.UpdateProductAsync(MapToUpsert(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Urun guncellenemedi.");
                return View("Edit", model);
            }

            ShowSuccess(result.Message ?? "Urun guncellendi.");
            return RedirectToAction("Detail", new
            {
                id = model.Id,
                selectedIds = model.SelectedIds,
                currentIndex = model.CurrentIndex,
                returnUrl = model.ReturnUrl
            });
        }

        private string BuildCurrentReturnUrl()
        {
            var path = Request.Path.HasValue ? Request.Path.Value : "/Product/List";
            var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            return (path ?? "/Product/List") + query;
        }

        private static string NormalizeReturnUrl(string? returnUrl, string fallback)
        {
            if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/'))
            {
                return fallback;
            }

            return returnUrl;
        }

        private static (decimal? ResolvedId, List<string> SelectedIds, int CurrentIndex) ResolveSelection(
            decimal? id,
            IEnumerable<string>? selectedIds,
            int currentIndex)
        {
            var normalized = selectedIds?
                .Select(x => x?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(ParsePositiveDecimal)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToList()
                ?? new List<decimal>();

            if (!id.HasValue && normalized.Count > 0)
            {
                id = normalized[0];
                currentIndex = 0;
            }

            if (normalized.Count > 0)
            {
                if (currentIndex < 0)
                {
                    currentIndex = 0;
                }

                if (currentIndex >= normalized.Count)
                {
                    currentIndex = normalized.Count - 1;
                }

                id = normalized[currentIndex];
            }
            else if (id.HasValue && id.Value > 0)
            {
                normalized.Add(id.Value);
                currentIndex = 0;
            }

            return (
                id,
                normalized.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
                currentIndex);
        }

        private async Task EnsureEditorDependenciesAsync(ProductPageEditorViewModelBase model)
        {
            if (model == null)
            {
                return;
            }

            var languages = await GetLanguageOptionsAsync();
            var categories = await GetProductCategoryOptionsAsync();
            var groups = await GetProductGroupOptionsAsync();
            var functions = await GetFunctionOptionsAsync();

            model.Languages = languages;
            model.ProductCategoryOptions = categories;
            model.ProductGroupOptions = groups;
            model.AvailableFunctions = functions;

            model.Names ??= new List<ProductPageLocalizedNameViewModel>();
            model.Functions ??= new List<ProductPageFunctionAssignmentViewModel>();

            if (model is ProductPageCreateViewModel)
            {
                model.Names = BuildCreateLocalizedNames(languages, model.Names);
            }

            var defaultLanguageId = languages.Count > 0 ? languages[0].Id : 1m;
            if (model.Names.Count == 0)
            {
                model.Names.Add(new ProductPageLocalizedNameViewModel
                {
                    LanguageId = defaultLanguageId
                });
            }

            foreach (var localizedName in model.Names)
            {
                if (localizedName.LanguageId <= 0)
                {
                    localizedName.LanguageId = defaultLanguageId;
                }
            }

            var languageNameById = languages
                .GroupBy(x => x.Id)
                .ToDictionary(x => x.Key, x => x.First().Name);

            foreach (var localizedName in model.Names)
            {
                if (string.IsNullOrWhiteSpace(localizedName.LanguageName) &&
                    languageNameById.TryGetValue(localizedName.LanguageId, out var languageName) &&
                    !string.IsNullOrWhiteSpace(languageName))
                {
                    localizedName.LanguageName = languageName;
                }
            }

            if (model.ProductCategoryId <= 0 && categories.Count > 0)
            {
                model.ProductCategoryId = categories[0].Id;
            }

            if (model.ProductGroupId <= 0 && groups.Count > 0)
            {
                model.ProductGroupId = groups[0].Id;
            }

            var functionNameMap = functions
                .GroupBy(x => x.Id)
                .ToDictionary(
                    x => x.Key,
                    x => x.First(),
                    EqualityComparer<decimal>.Default);

            model.Functions = model.Functions
                .Where(x => x.FunctionId > 0)
                .GroupBy(x => x.FunctionId)
                .Select(x =>
                {
                    var first = x.First();
                    if (string.IsNullOrWhiteSpace(first.FunctionName) && functionNameMap.TryGetValue(x.Key, out var option))
                    {
                        first.FunctionName = option.Name;
                        first.IsInvalid = option.IsInvalid;
                    }
                    else if (string.IsNullOrWhiteSpace(first.FunctionName))
                    {
                        first.FunctionName = x.Key.ToString(CultureInfo.InvariantCulture);
                    }

                    return new ProductPageFunctionAssignmentViewModel
                    {
                        FunctionId = first.FunctionId,
                        FunctionName = first.FunctionName,
                        IsInvalid = first.IsInvalid
                    };
                })
                .OrderBy(x => x.FunctionName)
                .ThenBy(x => x.FunctionId)
                .ToList();
        }

        private void ValidateEditorModel(ProductPageEditorViewModelBase model)
        {
            model.Code = NormalizeCode(model.Code);

            if (string.IsNullOrWhiteSpace(model.Code))
            {
                ModelState.AddModelError(nameof(model.Code), "Kod zorunludur.");
            }

            if (model.Code.Length > 8)
            {
                ModelState.AddModelError(nameof(model.Code), "Kod en fazla 8 karakter olabilir.");
            }

            if (model.ProductCategoryId <= 0)
            {
                ModelState.AddModelError(nameof(model.ProductCategoryId), "Urun kategorisi secimi zorunludur.");
            }

            if (model.ProductGroupId <= 0)
            {
                ModelState.AddModelError(nameof(model.ProductGroupId), "Urun grubu secimi zorunludur.");
            }

            if (model.Price < 0)
            {
                ModelState.AddModelError(nameof(model.Price), "Fiyat sifirdan kucuk olamaz.");
            }

            var hasName = model.Names.Any(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name));
            if (!hasName)
            {
                ModelState.AddModelError(string.Empty, "En az bir dilde ad girilmelidir.");
            }

            var hasFunction = model.Functions.Any(x => x.FunctionId > 0);
            if (!hasFunction)
            {
                ModelState.AddModelError(string.Empty, "En az bir fonksiyon secilmelidir.");
            }
        }

        private static ProductPageUpsertDto MapToUpsert(ProductPageEditorViewModelBase model)
        {
            return new ProductPageUpsertDto
            {
                Id = model.Id,
                Code = model.Code,
                ProductCategoryId = model.ProductCategoryId,
                ProductGroupId = model.ProductGroupId,
                Price = model.Price,
                Invisible = model.IsInvalid,
                Names = model.Names
                    .Where(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name))
                    .GroupBy(x => x.LanguageId)
                    .Select(x => new ProductPageLocalizedNameInputDto
                    {
                        LanguageId = x.Key,
                        Name = x.First().Name
                    })
                    .ToList(),
                FunctionIds = model.Functions
                    .Where(x => x.FunctionId > 0)
                    .Select(x => x.FunctionId)
                    .Distinct()
                    .ToList()
            };
        }

        private async Task<List<ProductPageLanguageOptionViewModel>> GetLanguageOptionsAsync()
        {
            var result = await _productPageService.GetLanguagesAsync();
            if (!result.IsSuccess || result.Data == null)
            {
                return new List<ProductPageLanguageOptionViewModel>();
            }

            return result.Data
                .Select(x => new ProductPageLanguageOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToList();
        }

        private async Task<List<ProductPageCategoryOptionViewModel>> GetProductCategoryOptionsAsync()
        {
            var result = await _productPageService.GetProductCategoryOptionsAsync();
            if (!result.IsSuccess || result.Data == null)
            {
                return new List<ProductPageCategoryOptionViewModel>();
            }

            return result.Data
                .Select(x => new ProductPageCategoryOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsInvalid = x.Invisible
                })
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToList();
        }

        private async Task<List<ProductPageGroupOptionViewModel>> GetProductGroupOptionsAsync()
        {
            var result = await _productPageService.GetProductGroupOptionsAsync();
            if (!result.IsSuccess || result.Data == null)
            {
                return new List<ProductPageGroupOptionViewModel>();
            }

            return result.Data
                .Select(x => new ProductPageGroupOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsInvalid = x.Invisible
                })
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToList();
        }

        private async Task<List<ProductPageFunctionOptionViewModel>> GetFunctionOptionsAsync()
        {
            var result = await _productPageService.SearchFunctionsAsync(new ProductPageFunctionLookupFilterDto
            {
                Page = 1,
                PageSize = 2000
            });

            if (!result.IsSuccess || result.Data == null)
            {
                return new List<ProductPageFunctionOptionViewModel>();
            }

            return result.Data.Items
                .Select(x => new ProductPageFunctionOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsInvalid = x.Invisible
                })
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToList();
        }

        private static ProductPageListItemViewModel MapListItem(ProductPageListItemDto dto)
        {
            return new ProductPageListItemViewModel
            {
                Id = dto.Id,
                Code = dto.Code,
                Name = dto.Name,
                ProductCategoryName = dto.ProductCategoryName,
                ProductGroupName = dto.ProductGroupName,
                Price = dto.Price,
                IsInvalid = dto.Invisible
            };
        }

        private static ProductPageDetailViewModel MapDetail(ProductPageDetailDto dto)
        {
            return new ProductPageDetailViewModel
            {
                Id = dto.Id,
                Code = dto.Code,
                ProductCategoryId = dto.ProductCategoryId,
                ProductCategoryName = dto.ProductCategoryName,
                ProductGroupId = dto.ProductGroupId,
                ProductGroupName = dto.ProductGroupName,
                Price = dto.Price,
                IsInvalid = dto.Invisible,
                Names = dto.Names.Select(x => new ProductPageLocalizedNameViewModel
                {
                    LanguageId = x.LanguageId,
                    LanguageName = x.LanguageName,
                    Name = x.Name
                }).ToList(),
                Functions = dto.Functions.Select(x => new ProductPageFunctionAssignmentViewModel
                {
                    FunctionId = x.Id,
                    FunctionName = x.Name,
                    IsInvalid = x.Invisible
                }).ToList()
            };
        }

        private static ProductPageEditViewModel MapEdit(ProductPageDetailDto dto)
        {
            return new ProductPageEditViewModel
            {
                Id = dto.Id,
                Code = dto.Code,
                ProductCategoryId = dto.ProductCategoryId,
                ProductCategoryName = dto.ProductCategoryName,
                ProductGroupId = dto.ProductGroupId,
                ProductGroupName = dto.ProductGroupName,
                Price = dto.Price,
                IsInvalid = dto.Invisible,
                Names = dto.Names.Select(x => new ProductPageLocalizedNameViewModel
                {
                    LanguageId = x.LanguageId,
                    LanguageName = x.LanguageName,
                    Name = x.Name
                }).ToList(),
                Functions = dto.Functions.Select(x => new ProductPageFunctionAssignmentViewModel
                {
                    FunctionId = x.Id,
                    FunctionName = x.Name,
                    IsInvalid = x.Invisible
                }).ToList()
            };
        }

        private static decimal? ParsePositiveDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
                ? parsed
                : null;
        }

        private static List<decimal> ParseDecimalCsv(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
            {
                return new List<decimal>();
            }

            return csv
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(ParsePositiveDecimal)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();
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

        private static List<ProductPageLocalizedNameViewModel> BuildCreateLocalizedNames(
            IReadOnlyCollection<ProductPageLanguageOptionViewModel> languages,
            IEnumerable<ProductPageLocalizedNameViewModel>? existingNames)
        {
            var existingByLanguage = (existingNames ?? Enumerable.Empty<ProductPageLocalizedNameViewModel>())
                .Where(x => x.LanguageId > 0)
                .GroupBy(x => x.LanguageId)
                .ToDictionary(x => x.Key, x => x.First());

            if (languages.Count == 0)
            {
                return existingByLanguage.Values
                    .OrderBy(x => x.LanguageId)
                    .Select(x => new ProductPageLocalizedNameViewModel
                    {
                        LanguageId = x.LanguageId,
                        LanguageName = x.LanguageName,
                        Name = x.Name
                    })
                    .ToList();
            }

            return languages
                .Select(language =>
                {
                    existingByLanguage.TryGetValue(language.Id, out var existing);
                    return new ProductPageLocalizedNameViewModel
                    {
                        LanguageId = language.Id,
                        LanguageName = language.Name,
                        Name = existing?.Name ?? string.Empty
                    };
                })
                .ToList();
        }
    }
}
