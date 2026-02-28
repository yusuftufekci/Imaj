using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Web.Authorization;
using Imaj.Web.Controllers.Base;
using Imaj.Web;
using Imaj.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Imaj.Web.Controllers
{
    public class ProdCatController : BaseController
    {
        private const double ViewMethodId = 1253d;
        private const double EditMethodId = 1254d;
        private const double BrowseMethodId = 1255d;
        private const double AddMethodId = 1256d;

        private readonly IProdCatService _prodCatService;

        public ProdCatController(IProdCatService prodCatService, ILogger<ProdCatController> logger, IStringLocalizer<SharedResource> localizer)
            : base(logger, localizer)
        {
            _prodCatService = prodCatService;
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public IActionResult Index()
        {
            return View(new ProdCatIndexViewModel
            {
                Filter = new ProdCatFilterModel
                {
                    Page = 1,
                    PageSize = 16
                }
            });
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> List(ProdCatFilterModel filter)
        {
            var normalizedFilter = filter ?? new ProdCatFilterModel();
            normalizedFilter.Page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            normalizedFilter.PageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;
            normalizedFilter.First = normalizedFilter.First.HasValue && normalizedFilter.First.Value > 0 ? normalizedFilter.First.Value : normalizedFilter.PageSize;

            var result = await _prodCatService.GetProdCatsAsync(new ProdCatFilterDto
            {
                IsInvalid = normalizedFilter.IsInvalid,
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize,
                First = normalizedFilter.First
            });

            var model = new ProdCatListViewModel
            {
                Items = result.IsSuccess && result.Data != null
                    ? result.Data.Items.Select(MapListItem).ToList()
                    : new List<ProdCatListItemViewModel>(),
                Page = result.Data?.Page ?? normalizedFilter.Page,
                PageSize = result.Data?.PageSize ?? normalizedFilter.PageSize,
                TotalCount = result.Data?.TotalCount ?? 0,
                Filter = normalizedFilter,
                ReturnUrl = BuildCurrentReturnUrl()
            };

            return View(model);
        }

        [AcceptVerbs("GET", "POST")]
        [RequireMethodPermission(ViewMethodId)]
        public async Task<IActionResult> Detail(decimal? id, string[]? selectedIds = null, int currentIndex = 0, string? returnUrl = null)
        {
            var resolved = ResolveSelection(id, selectedIds, currentIndex);
            if (!resolved.ResolvedId.HasValue)
            {
                return RedirectToAction("Index");
            }

            var detailResult = await _prodCatService.GetProdCatDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? L("ProductCategoryNotFound"));
                return RedirectToAction("List");
            }

            var model = MapDetail(detailResult.Data);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/ProdCat/List");

            return View(model);
        }

        [AcceptVerbs("GET", "POST")]
        [RequireMethodPermission(EditMethodId)]
        public async Task<IActionResult> Edit(decimal? id, string[]? selectedIds = null, int currentIndex = 0, string? returnUrl = null)
        {
            var resolved = ResolveSelection(id, selectedIds, currentIndex);
            if (!resolved.ResolvedId.HasValue)
            {
                return RedirectToAction("Index");
            }

            var detailResult = await _prodCatService.GetProdCatDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? L("ProductCategoryNotFound"));
                return RedirectToAction("List");
            }

            var model = MapEdit(detailResult.Data);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/ProdCat/List");

            await EnsureEditorDependenciesAsync(model);
            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(AddMethodId)]
        public async Task<IActionResult> Create()
        {
            var model = new ProdCatCreateViewModel
            {
                Sequence = 0
            };

            await EnsureEditorDependenciesAsync(model);

            if (model.TaxTypeOptions.Count > 0)
            {
                model.TaxTypeId = model.TaxTypeOptions[0].Id;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(EditMethodId, write: true)]
        public async Task<IActionResult> Update(ProdCatEditViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);
            ValidateEditorModel(model);

            if (!ModelState.IsValid)
            {
                return View("Edit", model);
            }

            var result = await _prodCatService.UpdateProdCatAsync(MapToUpsertDto(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? L("ProductCategoryUpdateFailed"));
                return View("Edit", model);
            }

            ShowSuccess(result.Message ?? L("ProductCategoryUpdatedSuccess"));
            return RedirectToAction("Detail", new
            {
                id = model.Id,
                selectedIds = model.SelectedIds,
                currentIndex = model.CurrentIndex,
                returnUrl = model.ReturnUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(AddMethodId, write: true)]
        public async Task<IActionResult> Save(ProdCatCreateViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);
            ValidateEditorModel(model);

            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }

            var result = await _prodCatService.CreateProdCatAsync(MapToUpsertDto(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? L("ProductCategorySaveFailed"));
                return View("Create", model);
            }

            ShowSuccess(result.Message ?? L("ProductCategorySavedSuccess"));
            if (model.AutomaticForward)
            {
                return RedirectToAction("Create");
            }

            return RedirectToAction("Index");
        }

        private async Task EnsureEditorDependenciesAsync(ProdCatEditorViewModelBase model)
        {
            if (model == null)
            {
                return;
            }

            var languageResult = await _prodCatService.GetLanguagesAsync();
            var taxTypeResult = await _prodCatService.GetTaxTypeOptionsAsync();

            model.Languages = languageResult.IsSuccess && languageResult.Data != null
                ? languageResult.Data
                    .Select(x => new ProdCatLanguageOptionViewModel
                    {
                        Id = x.Id,
                        Name = x.Name
                    })
                    .ToList()
                : new List<ProdCatLanguageOptionViewModel>();

            model.TaxTypeOptions = taxTypeResult.IsSuccess && taxTypeResult.Data != null
                ? taxTypeResult.Data
                    .Select(x => new ProdCatTaxTypeOptionViewModel
                    {
                        Id = x.Id,
                        Code = x.Code,
                        Name = x.Name
                    })
                    .ToList()
                : new List<ProdCatTaxTypeOptionViewModel>();

            model.Names ??= new List<ProdCatLocalizedNameViewModel>();

            if (model is ProdCatCreateViewModel)
            {
                model.Names = BuildCreateLocalizedNames(model.Languages, model.Names);
            }

            var defaultLanguageId = model.Languages.Count > 0 ? model.Languages[0].Id : 1m;
            if (model.Names.Count == 0)
            {
                model.Names.Add(new ProdCatLocalizedNameViewModel
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

            var languageNameById = model.Languages
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

            if (model.TaxTypeId <= 0 && model.TaxTypeOptions.Count > 0)
            {
                model.TaxTypeId = model.TaxTypeOptions[0].Id;
            }
        }

        private void ValidateEditorModel(ProdCatEditorViewModelBase model)
        {
            if (model.TaxTypeId <= 0)
            {
                ModelState.AddModelError(nameof(model.TaxTypeId), L("TaxTypeSelectionRequired"));
            }

            if (model.Sequence < 0)
            {
                ModelState.AddModelError(nameof(model.Sequence), L("SequenceCannotBeNegative"));
            }

            var hasName = model.Names.Any(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name));
            if (!hasName)
            {
                ModelState.AddModelError(string.Empty, L("AtLeastOneLocalizedNameRequired"));
            }
        }

        private string BuildCurrentReturnUrl()
        {
            var path = Request.Path.HasValue ? Request.Path.Value : "/ProdCat/List";
            var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            return (path ?? "/ProdCat/List") + query;
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

        private static ProdCatListItemViewModel MapListItem(ProdCatListItemDto dto)
        {
            return new ProdCatListItemViewModel
            {
                Id = dto.Id,
                Name = dto.Name,
                TaxCode = dto.TaxCode,
                TaxName = dto.TaxName,
                Sequence = dto.Sequence,
                IsInvalid = dto.Invisible
            };
        }

        private static ProdCatDetailViewModel MapDetail(ProdCatDetailDto dto)
        {
            return new ProdCatDetailViewModel
            {
                Id = dto.Id,
                TaxTypeId = dto.TaxTypeId,
                TaxCode = dto.TaxCode,
                TaxName = dto.TaxName,
                Sequence = dto.Sequence,
                IsInvalid = dto.Invisible,
                Names = dto.Names.Select(MapLocalizedName).ToList()
            };
        }

        private static ProdCatEditViewModel MapEdit(ProdCatDetailDto dto)
        {
            return new ProdCatEditViewModel
            {
                Id = dto.Id,
                TaxTypeId = dto.TaxTypeId,
                Sequence = dto.Sequence,
                IsInvalid = dto.Invisible,
                Names = dto.Names.Select(MapLocalizedName).ToList()
            };
        }

        private static ProdCatLocalizedNameViewModel MapLocalizedName(ProdCatLocalizedNameDto dto)
        {
            return new ProdCatLocalizedNameViewModel
            {
                LanguageId = dto.LanguageId,
                LanguageName = dto.LanguageName,
                Name = dto.Name
            };
        }

        private static ProdCatUpsertDto MapToUpsertDto(ProdCatEditorViewModelBase model)
        {
            return new ProdCatUpsertDto
            {
                Id = model.Id,
                TaxTypeId = model.TaxTypeId,
                Sequence = model.Sequence,
                Invisible = model.IsInvalid,
                Names = model.Names
                    .Where(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name))
                    .Select(x => new ProdCatLocalizedNameInputDto
                    {
                        LanguageId = x.LanguageId,
                        Name = (x.Name ?? string.Empty).Trim()
                    })
                    .ToList()
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

        private static List<ProdCatLocalizedNameViewModel> BuildCreateLocalizedNames(
            IReadOnlyCollection<ProdCatLanguageOptionViewModel> languages,
            IEnumerable<ProdCatLocalizedNameViewModel>? existingNames)
        {
            var existingByLanguage = (existingNames ?? Enumerable.Empty<ProdCatLocalizedNameViewModel>())
                .Where(x => x.LanguageId > 0)
                .GroupBy(x => x.LanguageId)
                .ToDictionary(x => x.Key, x => x.First());

            if (languages.Count == 0)
            {
                return existingByLanguage.Values
                    .OrderBy(x => x.LanguageId)
                    .Select(x => new ProdCatLocalizedNameViewModel
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
                    return new ProdCatLocalizedNameViewModel
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
