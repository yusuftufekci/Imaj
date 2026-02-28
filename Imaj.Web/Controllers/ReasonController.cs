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
    public class ReasonController : BaseController
    {
        private const double ViewMethodId = 1049d;
        private const double EditMethodId = 1050d;
        private const double BrowseMethodId = 1051d;
        private const double AddMethodId = 1052d;

        private readonly IReasonService _reasonService;

        public ReasonController(IReasonService reasonService, ILogger<ReasonController> logger, IStringLocalizer<SharedResource> localizer)
            : base(logger, localizer)
        {
            _reasonService = reasonService;
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> Index()
        {
            var reasonCatOptions = await GetReasonCatOptionsAsync();

            return View(new ReasonIndexViewModel
            {
                Filter = new ReasonFilterModel
                {
                    Page = 1,
                    PageSize = 16
                },
                ReasonCatOptions = reasonCatOptions,
                CreateReasonCatId = reasonCatOptions.Count > 0 ? reasonCatOptions[0].Id : null
            });
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> List(ReasonFilterModel filter)
        {
            var normalizedFilter = filter ?? new ReasonFilterModel();
            normalizedFilter.Page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            normalizedFilter.PageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;
            normalizedFilter.First = normalizedFilter.First.HasValue && normalizedFilter.First.Value > 0 ? normalizedFilter.First.Value : normalizedFilter.PageSize;

            var result = await _reasonService.GetReasonsAsync(new ReasonFilterDto
            {
                Code = normalizedFilter.Code,
                ReasonCatId = normalizedFilter.ReasonCatId,
                IsInvalid = normalizedFilter.IsInvalid,
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize,
                First = normalizedFilter.First
            });

            var model = new ReasonListViewModel
            {
                Items = result.IsSuccess && result.Data != null
                    ? result.Data.Items.Select(MapListItem).ToList()
                    : new List<ReasonListItemViewModel>(),
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

            var detailResult = await _reasonService.GetReasonDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? L("ReasonNotFound"));
                return RedirectToAction("List");
            }

            var model = MapDetail(detailResult.Data);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/Reason/List");

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

            var detailResult = await _reasonService.GetReasonDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? L("ReasonNotFound"));
                return RedirectToAction("List");
            }

            var languages = await GetLanguageOptionsAsync();
            var reasonCats = await GetReasonCatOptionsAsync();
            EnsureReasonCatOptionExists(reasonCats, detailResult.Data.ReasonCatId, detailResult.Data.ReasonCatName);
            var model = MapEdit(detailResult.Data, languages, reasonCats);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/Reason/List");

            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(AddMethodId)]
        public async Task<IActionResult> Create(decimal? reasonCatId = null, string? code = null)
        {
            var languages = await GetLanguageOptionsAsync();
            var reasonCats = await GetReasonCatOptionsAsync();
            var defaultLanguageId = languages.Count > 0 ? languages[0].Id : 1m;

            return View(new ReasonCreateViewModel
            {
                ReasonCatId = reasonCatId.HasValue && reasonCatId.Value > 0
                    ? reasonCatId
                    : null,
                Code = NormalizeCode(code),
                Languages = languages,
                ReasonCatOptions = reasonCats,
                Names = new List<ReasonLocalizedNameViewModel>
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
        public async Task<IActionResult> Update(ReasonEditViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);
            ValidateEditorModel(model);

            if (!ModelState.IsValid)
            {
                return View("Edit", model);
            }

            var result = await _reasonService.UpdateReasonAsync(MapToUpsertDto(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? L("ReasonUpdateFailed"));
                return View("Edit", model);
            }

            ShowSuccess(result.Message ?? L("ReasonUpdatedSuccess"));
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
        public async Task<IActionResult> Save(ReasonCreateViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);
            ValidateEditorModel(model);

            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }

            var result = await _reasonService.CreateReasonAsync(MapToUpsertDto(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? L("ReasonSaveFailed"));
                return View("Create", model);
            }

            ShowSuccess(result.Message ?? L("ReasonSavedSuccess"));
            if (model.AutomaticForward)
            {
                return RedirectToAction("Create", new
                {
                    reasonCatId = model.ReasonCatId,
                    code = model.Code
                });
            }

            return RedirectToAction("Index");
        }

        private string BuildCurrentReturnUrl()
        {
            var path = Request.Path.HasValue ? Request.Path.Value : "/Reason/List";
            var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            return (path ?? "/Reason/List") + query;
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

        private async Task<List<ReasonLanguageOptionViewModel>> GetLanguageOptionsAsync()
        {
            var result = await _reasonService.GetLanguagesAsync();
            if (!result.IsSuccess || result.Data == null)
            {
                return new List<ReasonLanguageOptionViewModel>();
            }

            return result.Data
                .Select(x => new ReasonLanguageOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }

        private async Task<List<ReasonCatOptionViewModel>> GetReasonCatOptionsAsync()
        {
            var result = await _reasonService.GetReasonCatOptionsAsync();
            if (!result.IsSuccess || result.Data == null)
            {
                return new List<ReasonCatOptionViewModel>();
            }

            return result.Data
                .Select(x => new ReasonCatOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }

        private async Task EnsureEditorDependenciesAsync(ReasonEditorViewModelBase model)
        {
            if (model == null)
            {
                return;
            }

            var languages = await GetLanguageOptionsAsync();
            var reasonCats = await GetReasonCatOptionsAsync();

            model.Languages = languages;
            model.ReasonCatOptions = reasonCats;
            model.Code = NormalizeCode(model.Code);
            model.Names ??= new List<ReasonLocalizedNameViewModel>();

            var defaultLanguageId = languages.Count > 0 ? languages[0].Id : 1m;
            if (model.Names.Count == 0)
            {
                model.Names.Add(new ReasonLocalizedNameViewModel
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

            if (model is ReasonEditViewModel editModel)
            {
                EnsureReasonCatOptionExists(reasonCats, editModel.ReasonCatId, editModel.ReasonCatName);
            }
            else if (model.ReasonCatId.HasValue && reasonCats.All(x => x.Id != model.ReasonCatId.Value))
            {
                model.ReasonCatId = null;
            }
        }

        private void ValidateEditorModel(ReasonEditorViewModelBase model)
        {
            if (!model.ReasonCatId.HasValue || model.ReasonCatId.Value <= 0)
            {
                ModelState.AddModelError(nameof(model.ReasonCatId), L("ReasonCategorySelectionRequired"));
            }

            if (string.IsNullOrWhiteSpace(model.Code))
            {
                ModelState.AddModelError(nameof(model.Code), L("ReasonCodeRequired"));
            }
        }

        private static ReasonListItemViewModel MapListItem(ReasonListItemDto dto)
        {
            return new ReasonListItemViewModel
            {
                Id = dto.Id,
                Code = dto.Code,
                Name = dto.Name,
                ReasonCatName = dto.ReasonCatName,
                IsInvalid = dto.Invisible
            };
        }

        private static ReasonDetailViewModel MapDetail(ReasonDetailDto dto)
        {
            return new ReasonDetailViewModel
            {
                Id = dto.Id,
                ReasonCatId = dto.ReasonCatId,
                ReasonCatName = dto.ReasonCatName,
                Code = dto.Code,
                IsInvalid = dto.Invisible,
                Names = dto.Names.Select(MapLocalizedName).ToList()
            };
        }

        private static ReasonEditViewModel MapEdit(
            ReasonDetailDto dto,
            List<ReasonLanguageOptionViewModel> languages,
            List<ReasonCatOptionViewModel> reasonCats)
        {
            return new ReasonEditViewModel
            {
                Id = dto.Id,
                ReasonCatId = dto.ReasonCatId,
                ReasonCatName = dto.ReasonCatName,
                Code = dto.Code,
                IsInvalid = dto.Invisible,
                Languages = languages,
                ReasonCatOptions = reasonCats,
                Names = dto.Names.Select(MapLocalizedName).ToList()
            };
        }

        private static ReasonLocalizedNameViewModel MapLocalizedName(ReasonLocalizedNameDto dto)
        {
            return new ReasonLocalizedNameViewModel
            {
                LanguageId = dto.LanguageId,
                LanguageName = dto.LanguageName,
                Name = dto.Name
            };
        }

        private static ReasonUpsertDto MapToUpsertDto(ReasonEditorViewModelBase model)
        {
            return new ReasonUpsertDto
            {
                Id = model.Id,
                ReasonCatId = model.ReasonCatId ?? 0,
                Code = NormalizeCode(model.Code),
                Invisible = model.IsInvalid,
                Names = model.Names
                    .Where(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name))
                    .Select(x => new ReasonLocalizedNameInputDto
                    {
                        LanguageId = x.LanguageId,
                        Name = x.Name
                    })
                    .ToList()
            };
        }

        private static string NormalizeCode(string? code)
        {
            return (code ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static decimal? ParsePositiveDecimal(string? value)
        {
            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                return null;
            }

            return parsed > 0 ? parsed : null;
        }

        private static void EnsureReasonCatOptionExists(
            ICollection<ReasonCatOptionViewModel> options,
            decimal? currentId,
            string? currentName)
        {
            if (!currentId.HasValue || currentId.Value <= 0 || options.Any(x => x.Id == currentId.Value))
            {
                return;
            }

            options.Add(new ReasonCatOptionViewModel
            {
                Id = currentId.Value,
                Name = !string.IsNullOrWhiteSpace(currentName)
                    ? currentName
                    : currentId.Value.ToString(CultureInfo.InvariantCulture)
            });
        }
    }
}
