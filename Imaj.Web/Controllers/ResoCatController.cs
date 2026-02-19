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
    public class ResoCatController : BaseController
    {
        private const double BrowseMethodId = 986d;
        private const double ViewMethodId = 987d;
        private const double EditMethodId = 988d;
        private const double AddMethodId = 989d;

        private readonly IResoCatService _resoCatService;

        public ResoCatController(IResoCatService resoCatService, ILogger<ResoCatController> logger)
            : base(logger)
        {
            _resoCatService = resoCatService;
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public IActionResult Index()
        {
            return View(new ResoCatIndexViewModel
            {
                Filter = new ResoCatFilterModel
                {
                    Page = 1,
                    PageSize = 16
                }
            });
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> List(ResoCatFilterModel filter)
        {
            var normalizedFilter = filter ?? new ResoCatFilterModel();
            normalizedFilter.Page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            normalizedFilter.PageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;

            var result = await _resoCatService.GetResoCatsAsync(new ResoCatFilterDto
            {
                Name = normalizedFilter.Name,
                IsInvalid = normalizedFilter.IsInvalid,
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize
            });

            var model = new ResoCatListViewModel
            {
                Items = result.IsSuccess && result.Data != null
                    ? result.Data.Items.Select(MapListItem).ToList()
                    : new List<ResoCatListItemViewModel>(),
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

            var detailResult = await _resoCatService.GetResoCatDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? "Kaynak kategorisi bulunamadi.");
                return RedirectToAction("List");
            }

            var model = MapDetail(detailResult.Data);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/ResoCat/List");

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

            var detailResult = await _resoCatService.GetResoCatDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? "Kaynak kategorisi bulunamadi.");
                return RedirectToAction("List");
            }

            var languageOptions = await GetLanguageOptionsAsync();
            var model = MapEdit(detailResult.Data, languageOptions);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/ResoCat/List");

            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(AddMethodId)]
        public async Task<IActionResult> Create()
        {
            var languageOptions = await GetLanguageOptionsAsync();
            var defaultLanguageId = languageOptions.Count > 0 ? languageOptions[0].Id : 1m;

            return View(new ResoCatCreateViewModel
            {
                Languages = languageOptions,
                Names = new List<ResoCatLocalizedNameViewModel>
                {
                    new()
                    {
                        LanguageId = defaultLanguageId
                    }
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(EditMethodId, write: true)]
        public async Task<IActionResult> Update(ResoCatEditViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);

            if (!ModelState.IsValid)
            {
                return View("Edit", model);
            }

            var result = await _resoCatService.UpdateResoCatAsync(MapToUpsertDto(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Kaynak kategorisi guncellenemedi.");
                return View("Edit", model);
            }

            ShowSuccess(result.Message ?? "Kaynak kategorisi guncellendi.");
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
        public async Task<IActionResult> Save(ResoCatCreateViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);

            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }

            var result = await _resoCatService.CreateResoCatAsync(MapToUpsertDto(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Kaynak kategorisi kaydedilemedi.");
                return View("Create", model);
            }

            ShowSuccess(result.Message ?? "Kaynak kategorisi kaydedildi.");
            if (model.AutomaticForward)
            {
                return RedirectToAction("Create");
            }

            return RedirectToAction("Index");
        }

        private string BuildCurrentReturnUrl()
        {
            var path = Request.Path.HasValue ? Request.Path.Value : "/ResoCat/List";
            var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            return (path ?? "/ResoCat/List") + query;
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

        private async Task<List<ResoCatLanguageOptionViewModel>> GetLanguageOptionsAsync()
        {
            var languagesResult = await _resoCatService.GetLanguagesAsync();
            if (!languagesResult.IsSuccess || languagesResult.Data == null)
            {
                return new List<ResoCatLanguageOptionViewModel>();
            }

            return languagesResult.Data
                .Select(x => new ResoCatLanguageOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }

        private async Task EnsureEditorDependenciesAsync(ResoCatEditorViewModelBase model)
        {
            if (model == null)
            {
                return;
            }

            var languages = await GetLanguageOptionsAsync();
            model.Languages = languages;

            if (model.Names == null)
            {
                model.Names = new List<ResoCatLocalizedNameViewModel>();
            }

            if (model.Names.Count == 0)
            {
                var defaultLanguageId = languages.Count > 0 ? languages[0].Id : 1m;
                model.Names.Add(new ResoCatLocalizedNameViewModel
                {
                    LanguageId = defaultLanguageId
                });
            }

            var fallbackLanguageId = languages.Count > 0 ? languages[0].Id : 1m;
            foreach (var localizedName in model.Names)
            {
                if (localizedName.LanguageId <= 0)
                {
                    localizedName.LanguageId = fallbackLanguageId;
                }
            }
        }

        private static ResoCatListItemViewModel MapListItem(ResoCatListItemDto dto)
        {
            return new ResoCatListItemViewModel
            {
                Id = dto.Id,
                Name = dto.Name,
                IsInvalid = dto.Invisible
            };
        }

        private static ResoCatDetailViewModel MapDetail(ResoCatDetailDto dto)
        {
            return new ResoCatDetailViewModel
            {
                Id = dto.Id,
                IsInvalid = dto.Invisible,
                Names = dto.Names.Select(MapLocalizedName).ToList()
            };
        }

        private static ResoCatEditViewModel MapEdit(ResoCatDetailDto dto, List<ResoCatLanguageOptionViewModel> languages)
        {
            return new ResoCatEditViewModel
            {
                Id = dto.Id,
                IsInvalid = dto.Invisible,
                Languages = languages,
                Names = dto.Names.Select(MapLocalizedName).ToList()
            };
        }

        private static ResoCatLocalizedNameViewModel MapLocalizedName(ResoCatLocalizedNameDto dto)
        {
            return new ResoCatLocalizedNameViewModel
            {
                LanguageId = dto.LanguageId,
                LanguageName = dto.LanguageName,
                Name = dto.Name
            };
        }

        private static ResoCatUpsertDto MapToUpsertDto(ResoCatEditorViewModelBase model)
        {
            return new ResoCatUpsertDto
            {
                Id = model.Id,
                Invisible = model.IsInvalid,
                Names = model.Names
                    .Where(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name))
                    .Select(x => new ResoCatLocalizedNameInputDto
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
    }
}
