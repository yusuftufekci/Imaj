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
    public class TaxTypeController : BaseController
    {
        private const double ViewMethodId = 1239d;
        private const double EditMethodId = 1240d;
        private const double AddMethodId = 1241d;
        private const double BrowseMethodId = 1242d;

        private readonly ITaxTypeService _taxTypeService;

        public TaxTypeController(ITaxTypeService taxTypeService, ILogger<TaxTypeController> logger, IStringLocalizer<SharedResource> localizer)
            : base(logger, localizer)
        {
            _taxTypeService = taxTypeService;
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public IActionResult Index(string? code = null)
        {
            return View(new TaxTypeIndexViewModel
            {
                Filter = new TaxTypeFilterModel
                {
                    Page = 1,
                    PageSize = 16
                },
                CreateCode = NormalizeCode(code)
            });
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> List(TaxTypeFilterModel filter)
        {
            var normalizedFilter = filter ?? new TaxTypeFilterModel();
            normalizedFilter.Page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            normalizedFilter.PageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;

            var result = await _taxTypeService.GetTaxTypesAsync(new TaxTypeFilterDto
            {
                Code = normalizedFilter.Code,
                IsInvalid = normalizedFilter.IsInvalid,
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize
            });

            var model = new TaxTypeListViewModel
            {
                Items = result.IsSuccess && result.Data != null
                    ? result.Data.Items.Select(MapListItem).ToList()
                    : new List<TaxTypeListItemViewModel>(),
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

            var detailResult = await _taxTypeService.GetTaxTypeDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? L("TaxTypeNotFound"));
                return RedirectToAction("List");
            }

            var model = MapDetail(detailResult.Data);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/TaxType/List");

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

            var detailResult = await _taxTypeService.GetTaxTypeDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? L("TaxTypeNotFound"));
                return RedirectToAction("List");
            }

            var languages = await GetLanguageOptionsAsync();
            var model = MapEdit(detailResult.Data, languages);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/TaxType/List");

            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(AddMethodId)]
        public async Task<IActionResult> Create(string? code = null)
        {
            var languages = await GetLanguageOptionsAsync();

            return View(new TaxTypeCreateViewModel
            {
                Code = NormalizeCode(code),
                TaxPercentage = 0,
                Languages = languages,
                Names = BuildCreateLocalizedNames(languages, null)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(EditMethodId, write: true)]
        public async Task<IActionResult> Update(TaxTypeEditViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);
            ValidateEditorModel(model);

            if (!ModelState.IsValid)
            {
                return View("Edit", model);
            }

            var result = await _taxTypeService.UpdateTaxTypeAsync(MapToUpsertDto(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? L("TaxTypeUpdateFailed"));
                return View("Edit", model);
            }

            ShowSuccess(result.Message ?? L("TaxTypeUpdatedSuccess"));
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
        public async Task<IActionResult> Save(TaxTypeCreateViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);
            ValidateEditorModel(model);

            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }

            var result = await _taxTypeService.CreateTaxTypeAsync(MapToUpsertDto(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? L("TaxTypeSaveFailed"));
                return View("Create", model);
            }

            ShowSuccess(result.Message ?? L("TaxTypeSavedSuccess"));
            if (model.AutomaticForward)
            {
                return RedirectToAction("Create", new { code = model.Code });
            }

            return RedirectToAction("Index");
        }

        private string BuildCurrentReturnUrl()
        {
            var path = Request.Path.HasValue ? Request.Path.Value : "/TaxType/List";
            var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            return (path ?? "/TaxType/List") + query;
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

        private async Task<List<TaxTypeLanguageOptionViewModel>> GetLanguageOptionsAsync()
        {
            var result = await _taxTypeService.GetLanguagesAsync();
            if (!result.IsSuccess || result.Data == null)
            {
                return new List<TaxTypeLanguageOptionViewModel>();
            }

            return result.Data
                .Select(x => new TaxTypeLanguageOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }

        private async Task EnsureEditorDependenciesAsync(TaxTypeEditorViewModelBase model)
        {
            if (model == null)
            {
                return;
            }

            var languages = await GetLanguageOptionsAsync();

            model.Languages = languages;
            model.Names ??= new List<TaxTypeLocalizedNameViewModel>();

            if (model is TaxTypeCreateViewModel)
            {
                model.Names = BuildCreateLocalizedNames(languages, model.Names);
            }

            var defaultLanguageId = languages.Count > 0 ? languages[0].Id : 1m;
            if (model.Names.Count == 0)
            {
                model.Names.Add(new TaxTypeLocalizedNameViewModel
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
        }

        private void ValidateEditorModel(TaxTypeEditorViewModelBase model)
        {
            if (string.IsNullOrWhiteSpace(model.Code))
            {
                ModelState.AddModelError(nameof(model.Code), L("CodeRequired"));
            }

            if (model.Code.Trim().Length > 8)
            {
                ModelState.AddModelError(nameof(model.Code), L("CodeMaxLength8"));
            }

            if (model.TaxPercentage < 0 || model.TaxPercentage > 100)
            {
                ModelState.AddModelError(nameof(model.TaxPercentage), L("TaxRateRangeInvalid"));
            }

            var hasName = model.Names.Any(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name));
            if (!hasName)
            {
                ModelState.AddModelError(string.Empty, L("AtLeastOneLocalizedNameRequired"));
            }
        }

        private static TaxTypeListItemViewModel MapListItem(TaxTypeListItemDto dto)
        {
            return new TaxTypeListItemViewModel
            {
                Id = dto.Id,
                Code = dto.Code,
                Name = dto.Name,
                TaxPercentage = dto.TaxPercentage,
                IsInvalid = dto.Invisible
            };
        }

        private static TaxTypeDetailViewModel MapDetail(TaxTypeDetailDto dto)
        {
            return new TaxTypeDetailViewModel
            {
                Id = dto.Id,
                Code = dto.Code,
                TaxPercentage = dto.TaxPercentage,
                IsInvalid = dto.Invisible,
                Names = dto.Names.Select(MapLocalizedName).ToList()
            };
        }

        private static TaxTypeEditViewModel MapEdit(
            TaxTypeDetailDto dto,
            List<TaxTypeLanguageOptionViewModel> languages)
        {
            return new TaxTypeEditViewModel
            {
                Id = dto.Id,
                Code = dto.Code,
                TaxPercentage = dto.TaxPercentage,
                IsInvalid = dto.Invisible,
                Languages = languages,
                Names = dto.Names.Select(MapLocalizedName).ToList()
            };
        }

        private static TaxTypeLocalizedNameViewModel MapLocalizedName(TaxTypeLocalizedNameDto dto)
        {
            return new TaxTypeLocalizedNameViewModel
            {
                LanguageId = dto.LanguageId,
                LanguageName = dto.LanguageName,
                Name = dto.Name,
                InvoLinePostfix = dto.InvoLinePostfix
            };
        }

        private static TaxTypeUpsertDto MapToUpsertDto(TaxTypeEditorViewModelBase model)
        {
            return new TaxTypeUpsertDto
            {
                Id = model.Id,
                Code = NormalizeCode(model.Code),
                TaxPercentage = model.TaxPercentage,
                Invisible = model.IsInvalid,
                Names = model.Names
                    .Select(x => new TaxTypeLocalizedNameInputDto
                    {
                        LanguageId = x.LanguageId,
                        Name = (x.Name ?? string.Empty).Trim(),
                        InvoLinePostfix = (x.InvoLinePostfix ?? string.Empty).Trim()
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

        private static string NormalizeCode(string? code)
        {
            var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
            if (normalized.Length > 8)
            {
                normalized = normalized.Substring(0, 8);
            }

            return normalized;
        }

        private static List<TaxTypeLocalizedNameViewModel> BuildCreateLocalizedNames(
            IReadOnlyCollection<TaxTypeLanguageOptionViewModel> languages,
            IEnumerable<TaxTypeLocalizedNameViewModel>? existingNames)
        {
            var existingByLanguage = (existingNames ?? Enumerable.Empty<TaxTypeLocalizedNameViewModel>())
                .Where(x => x.LanguageId > 0)
                .GroupBy(x => x.LanguageId)
                .ToDictionary(x => x.Key, x => x.First());

            if (languages.Count == 0)
            {
                return existingByLanguage.Values
                    .OrderBy(x => x.LanguageId)
                    .Select(x => new TaxTypeLocalizedNameViewModel
                    {
                        LanguageId = x.LanguageId,
                        LanguageName = x.LanguageName,
                        Name = x.Name,
                        InvoLinePostfix = x.InvoLinePostfix
                    })
                    .ToList();
            }

            return languages
                .Select(language =>
                {
                    existingByLanguage.TryGetValue(language.Id, out var existing);
                    return new TaxTypeLocalizedNameViewModel
                    {
                        LanguageId = language.Id,
                        LanguageName = language.Name,
                        Name = existing?.Name ?? string.Empty,
                        InvoLinePostfix = existing?.InvoLinePostfix ?? string.Empty
                    };
                })
                .ToList();
        }
    }
}
