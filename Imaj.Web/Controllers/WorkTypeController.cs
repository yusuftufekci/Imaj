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
    public class WorkTypeController : BaseController
    {
        private const double ViewMethodId = 1184d;
        private const double EditMethodId = 1185d;
        private const double BrowseMethodId = 1186d;
        private const double AddMethodId = 1183d;

        private readonly IWorkTypeService _workTypeService;

        public WorkTypeController(IWorkTypeService workTypeService, ILogger<WorkTypeController> logger)
            : base(logger)
        {
            _workTypeService = workTypeService;
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public IActionResult Index()
        {
            return View(new WorkTypeIndexViewModel
            {
                Filter = new WorkTypeFilterModel
                {
                    Page = 1,
                    PageSize = 16
                }
            });
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> List(WorkTypeFilterModel filter)
        {
            var normalizedFilter = filter ?? new WorkTypeFilterModel();
            normalizedFilter.Page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            normalizedFilter.PageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;

            var result = await _workTypeService.GetWorkTypesAsync(new WorkTypeFilterDto
            {
                IsInvalid = normalizedFilter.IsInvalid,
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize
            });

            var model = new WorkTypeListViewModel
            {
                Items = result.IsSuccess && result.Data != null
                    ? result.Data.Items.Select(MapListItem).ToList()
                    : new List<WorkTypeListItemViewModel>(),
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

            var detailResult = await _workTypeService.GetWorkTypeDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? "Gorev tipi bulunamadi.");
                return RedirectToAction("List");
            }

            var model = MapDetail(detailResult.Data);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/WorkType/List");

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

            var detailResult = await _workTypeService.GetWorkTypeDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? "Gorev tipi bulunamadi.");
                return RedirectToAction("List");
            }

            var languages = await GetLanguageOptionsAsync();
            var model = MapEdit(detailResult.Data, languages);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/WorkType/List");

            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(AddMethodId)]
        public async Task<IActionResult> Create()
        {
            var languages = await GetLanguageOptionsAsync();
            var defaultLanguageId = languages.Count > 0 ? languages[0].Id : 1m;

            return View(new WorkTypeCreateViewModel
            {
                Languages = languages,
                Names = new List<WorkTypeLocalizedNameViewModel>
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
        public async Task<IActionResult> Update(WorkTypeEditViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);
            ValidateEditorModel(model);

            if (!ModelState.IsValid)
            {
                return View("Edit", model);
            }

            var result = await _workTypeService.UpdateWorkTypeAsync(MapToUpsertDto(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Gorev tipi guncellenemedi.");
                return View("Edit", model);
            }

            ShowSuccess(result.Message ?? "Gorev tipi guncellendi.");
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
        public async Task<IActionResult> Save(WorkTypeCreateViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);
            ValidateEditorModel(model);

            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }

            var result = await _workTypeService.CreateWorkTypeAsync(MapToUpsertDto(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Gorev tipi kaydedilemedi.");
                return View("Create", model);
            }

            ShowSuccess(result.Message ?? "Gorev tipi kaydedildi.");
            if (model.AutomaticForward)
            {
                return RedirectToAction("Create");
            }

            return RedirectToAction("Index");
        }

        private string BuildCurrentReturnUrl()
        {
            var path = Request.Path.HasValue ? Request.Path.Value : "/WorkType/List";
            var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            return (path ?? "/WorkType/List") + query;
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

        private async Task<List<WorkTypeLanguageOptionViewModel>> GetLanguageOptionsAsync()
        {
            var result = await _workTypeService.GetLanguagesAsync();
            if (!result.IsSuccess || result.Data == null)
            {
                return new List<WorkTypeLanguageOptionViewModel>();
            }

            return result.Data
                .Select(x => new WorkTypeLanguageOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }

        private async Task EnsureEditorDependenciesAsync(WorkTypeEditorViewModelBase model)
        {
            if (model == null)
            {
                return;
            }

            var languages = await GetLanguageOptionsAsync();

            model.Languages = languages;
            model.Names ??= new List<WorkTypeLocalizedNameViewModel>();

            var defaultLanguageId = languages.Count > 0 ? languages[0].Id : 1m;
            if (model.Names.Count == 0)
            {
                model.Names.Add(new WorkTypeLocalizedNameViewModel
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
        }

        private void ValidateEditorModel(WorkTypeEditorViewModelBase model)
        {
            var hasName = model.Names.Any(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name));
            if (!hasName)
            {
                ModelState.AddModelError(string.Empty, "En az bir dilde ad girilmelidir.");
            }
        }

        private static WorkTypeListItemViewModel MapListItem(WorkTypeListItemDto dto)
        {
            return new WorkTypeListItemViewModel
            {
                Id = dto.Id,
                Name = dto.Name,
                IsInvalid = dto.Invisible
            };
        }

        private static WorkTypeDetailViewModel MapDetail(WorkTypeDetailDto dto)
        {
            return new WorkTypeDetailViewModel
            {
                Id = dto.Id,
                IsInvalid = dto.Invisible,
                Names = dto.Names.Select(MapLocalizedName).ToList()
            };
        }

        private static WorkTypeEditViewModel MapEdit(
            WorkTypeDetailDto dto,
            List<WorkTypeLanguageOptionViewModel> languages)
        {
            return new WorkTypeEditViewModel
            {
                Id = dto.Id,
                IsInvalid = dto.Invisible,
                Languages = languages,
                Names = dto.Names.Select(MapLocalizedName).ToList()
            };
        }

        private static WorkTypeLocalizedNameViewModel MapLocalizedName(WorkTypeLocalizedNameDto dto)
        {
            return new WorkTypeLocalizedNameViewModel
            {
                LanguageId = dto.LanguageId,
                LanguageName = dto.LanguageName,
                Name = dto.Name
            };
        }

        private static WorkTypeUpsertDto MapToUpsertDto(WorkTypeEditorViewModelBase model)
        {
            return new WorkTypeUpsertDto
            {
                Id = model.Id,
                Invisible = model.IsInvalid,
                Names = model.Names
                    .Where(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name))
                    .Select(x => new WorkTypeLocalizedNameInputDto
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
