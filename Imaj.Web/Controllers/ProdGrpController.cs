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
    public class ProdGrpController : BaseController
    {
        private const double ViewMethodId = 1458d;
        private const double AddMethodId = 1459d;
        private const double BrowseMethodId = 1460d;
        private const double EditMethodId = 1461d;

        private readonly IProdGrpService _prodGrpService;

        public ProdGrpController(IProdGrpService prodGrpService, ILogger<ProdGrpController> logger)
            : base(logger)
        {
            _prodGrpService = prodGrpService;
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public IActionResult Index()
        {
            return View(new ProdGrpIndexViewModel
            {
                Filter = new ProdGrpFilterModel
                {
                    Page = 1,
                    PageSize = 16
                }
            });
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> List(ProdGrpFilterModel filter)
        {
            var normalizedFilter = filter ?? new ProdGrpFilterModel();
            normalizedFilter.Page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            normalizedFilter.PageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;

            var result = await _prodGrpService.GetProdGrpsAsync(new ProdGrpFilterDto
            {
                IsInvalid = normalizedFilter.IsInvalid,
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize
            });

            var model = new ProdGrpListViewModel
            {
                Items = result.IsSuccess && result.Data != null
                    ? result.Data.Items.Select(MapListItem).ToList()
                    : new List<ProdGrpListItemViewModel>(),
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

            var detailResult = await _prodGrpService.GetProdGrpDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? "Urun grubu bulunamadi.");
                return RedirectToAction("List");
            }

            var model = MapDetail(detailResult.Data);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/ProdGrp/List");

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

            var detailResult = await _prodGrpService.GetProdGrpDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? "Urun grubu bulunamadi.");
                return RedirectToAction("List");
            }

            var languageOptions = await GetLanguageOptionsAsync();
            var model = MapEdit(detailResult.Data, languageOptions);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/ProdGrp/List");

            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(AddMethodId)]
        public async Task<IActionResult> Create()
        {
            var languageOptions = await GetLanguageOptionsAsync();

            return View(new ProdGrpCreateViewModel
            {
                Languages = languageOptions,
                Names = BuildCreateLocalizedNames(languageOptions, null)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(EditMethodId, write: true)]
        public async Task<IActionResult> Update(ProdGrpEditViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);
            ValidateEditorModel(model);

            if (!ModelState.IsValid)
            {
                return View("Edit", model);
            }

            var result = await _prodGrpService.UpdateProdGrpAsync(MapToUpsertDto(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Urun grubu guncellenemedi.");
                return View("Edit", model);
            }

            ShowSuccess(result.Message ?? "Urun grubu guncellendi.");
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
        public async Task<IActionResult> Save(ProdGrpCreateViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);
            ValidateEditorModel(model);

            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }

            var result = await _prodGrpService.CreateProdGrpAsync(MapToUpsertDto(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Urun grubu kaydedilemedi.");
                return View("Create", model);
            }

            ShowSuccess(result.Message ?? "Urun grubu kaydedildi.");
            if (model.AutomaticForward)
            {
                return RedirectToAction("Create");
            }

            return RedirectToAction("Index");
        }

        private string BuildCurrentReturnUrl()
        {
            var path = Request.Path.HasValue ? Request.Path.Value : "/ProdGrp/List";
            var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            return (path ?? "/ProdGrp/List") + query;
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

        private async Task<List<ProdGrpLanguageOptionViewModel>> GetLanguageOptionsAsync()
        {
            var result = await _prodGrpService.GetLanguagesAsync();
            if (!result.IsSuccess || result.Data == null)
            {
                return new List<ProdGrpLanguageOptionViewModel>();
            }

            return result.Data
                .Select(x => new ProdGrpLanguageOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }

        private async Task EnsureEditorDependenciesAsync(ProdGrpEditorViewModelBase model)
        {
            if (model == null)
            {
                return;
            }

            var languages = await GetLanguageOptionsAsync();
            model.Languages = languages;

            model.Names ??= new List<ProdGrpLocalizedNameViewModel>();

            if (model is ProdGrpCreateViewModel)
            {
                model.Names = BuildCreateLocalizedNames(languages, model.Names);
            }

            var defaultLanguageId = languages.Count > 0 ? languages[0].Id : 1m;
            if (model.Names.Count == 0)
            {
                model.Names.Add(new ProdGrpLocalizedNameViewModel
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

        private void ValidateEditorModel(ProdGrpEditorViewModelBase model)
        {
            var hasName = model.Names.Any(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name));
            if (!hasName)
            {
                ModelState.AddModelError(string.Empty, "En az bir dilde ad girilmelidir.");
            }
        }

        private static ProdGrpListItemViewModel MapListItem(ProdGrpListItemDto dto)
        {
            return new ProdGrpListItemViewModel
            {
                Id = dto.Id,
                Name = dto.Name,
                IsInvalid = dto.Invisible
            };
        }

        private static ProdGrpDetailViewModel MapDetail(ProdGrpDetailDto dto)
        {
            return new ProdGrpDetailViewModel
            {
                Id = dto.Id,
                IsInvalid = dto.Invisible,
                Names = dto.Names.Select(MapLocalizedName).ToList()
            };
        }

        private static ProdGrpEditViewModel MapEdit(
            ProdGrpDetailDto dto,
            List<ProdGrpLanguageOptionViewModel> languages)
        {
            return new ProdGrpEditViewModel
            {
                Id = dto.Id,
                IsInvalid = dto.Invisible,
                Languages = languages,
                Names = dto.Names.Select(MapLocalizedName).ToList()
            };
        }

        private static ProdGrpLocalizedNameViewModel MapLocalizedName(ProdGrpLocalizedNameDto dto)
        {
            return new ProdGrpLocalizedNameViewModel
            {
                LanguageId = dto.LanguageId,
                LanguageName = dto.LanguageName,
                Name = dto.Name
            };
        }

        private static ProdGrpUpsertDto MapToUpsertDto(ProdGrpEditorViewModelBase model)
        {
            return new ProdGrpUpsertDto
            {
                Id = model.Id,
                Invisible = model.IsInvalid,
                Names = model.Names
                    .Where(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name))
                    .Select(x => new ProdGrpLocalizedNameInputDto
                    {
                        LanguageId = x.LanguageId,
                        Name = x.Name
                    })
                    .ToList()
            };
        }

        private static decimal? ParsePositiveDecimal(string? value)
        {
            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                return null;
            }

            return parsed > 0 ? parsed : null;
        }

        private static List<ProdGrpLocalizedNameViewModel> BuildCreateLocalizedNames(
            IReadOnlyCollection<ProdGrpLanguageOptionViewModel> languages,
            IEnumerable<ProdGrpLocalizedNameViewModel>? existingNames)
        {
            var existingByLanguage = (existingNames ?? Enumerable.Empty<ProdGrpLocalizedNameViewModel>())
                .Where(x => x.LanguageId > 0)
                .GroupBy(x => x.LanguageId)
                .ToDictionary(x => x.Key, x => x.First());

            if (languages.Count == 0)
            {
                return existingByLanguage.Values
                    .OrderBy(x => x.LanguageId)
                    .Select(x => new ProdGrpLocalizedNameViewModel
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
                    return new ProdGrpLocalizedNameViewModel
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
