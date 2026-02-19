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
    public class ResourceController : BaseController
    {
        private const double ViewMethodId = 1029d;
        private const double BrowseMethodId = 1030d;
        private const double EditMethodId = 1031d;
        private const double AddMethodId = 1032d;

        private readonly IResourceService _resourceService;

        public ResourceController(IResourceService resourceService, ILogger<ResourceController> logger)
            : base(logger)
        {
            _resourceService = resourceService;
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> Index()
        {
            var functionOptions = await GetFunctionOptionsAsync();
            var resoCatOptions = await GetResoCatOptionsAsync();

            return View(new ResourceIndexViewModel
            {
                Filter = new ResourceFilterModel
                {
                    Page = 1,
                    PageSize = 16
                },
                FunctionOptions = functionOptions,
                ResoCatOptions = resoCatOptions,
                CreateFunctionId = functionOptions.Count > 0 ? functionOptions[0].Id : null,
                CreateResoCatId = resoCatOptions.Count > 0 ? resoCatOptions[0].Id : null
            });
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> List(ResourceFilterModel filter)
        {
            var normalizedFilter = filter ?? new ResourceFilterModel();
            normalizedFilter.Page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            normalizedFilter.PageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;

            if (normalizedFilter.SequenceFrom.HasValue && normalizedFilter.SequenceTo.HasValue
                && normalizedFilter.SequenceFrom.Value > normalizedFilter.SequenceTo.Value)
            {
                ModelState.AddModelError(string.Empty, "Sira No baslangic degeri bitis degerinden buyuk olamaz.");
                normalizedFilter.SequenceTo = normalizedFilter.SequenceFrom;
            }

            var result = await _resourceService.GetResourcesAsync(new ResourceFilterDto
            {
                Code = normalizedFilter.Code,
                SequenceFrom = normalizedFilter.SequenceFrom,
                SequenceTo = normalizedFilter.SequenceTo,
                FunctionId = normalizedFilter.FunctionId,
                ResoCatId = normalizedFilter.ResoCatId,
                IsInvalid = normalizedFilter.IsInvalid,
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize
            });

            var model = new ResourceListViewModel
            {
                Items = result.IsSuccess && result.Data != null
                    ? result.Data.Items.Select(MapListItem).ToList()
                    : new List<ResourceListItemViewModel>(),
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

            var detailResult = await _resourceService.GetResourceDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? "Kaynak bulunamadi.");
                return RedirectToAction("List");
            }

            var model = MapDetail(detailResult.Data);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/Resource/List");

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

            var detailResult = await _resourceService.GetResourceDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? "Kaynak bulunamadi.");
                return RedirectToAction("List");
            }

            var languages = await GetLanguageOptionsAsync();
            var functions = await GetFunctionOptionsAsync();
            var resoCats = await GetResoCatOptionsAsync();
            var model = MapEdit(detailResult.Data, languages, functions, resoCats);

            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/Resource/List");

            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(AddMethodId)]
        public async Task<IActionResult> Create(decimal? functionId = null, decimal? resoCatId = null, string? code = null)
        {
            var languages = await GetLanguageOptionsAsync();
            var functions = await GetFunctionOptionsAsync();
            var resoCats = await GetResoCatOptionsAsync();
            var defaultLanguageId = languages.Count > 0 ? languages[0].Id : 1m;

            var normalizedCode = NormalizeCode(code);

            return View(new ResourceCreateViewModel
            {
                FunctionId = functionId.HasValue && functionId.Value > 0
                    ? functionId
                    : null,
                ResoCatId = resoCatId.HasValue && resoCatId.Value > 0
                    ? resoCatId
                    : null,
                Code = normalizedCode,
                Sequence = 0,
                Languages = languages,
                FunctionOptions = functions,
                ResoCatOptions = resoCats,
                Names = new List<ResourceLocalizedNameViewModel>
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
        public async Task<IActionResult> Update(ResourceEditViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);
            ValidateEditorModel(model);

            if (!ModelState.IsValid)
            {
                return View("Edit", model);
            }

            var result = await _resourceService.UpdateResourceAsync(MapToUpsertDto(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Kaynak guncellenemedi.");
                return View("Edit", model);
            }

            ShowSuccess(result.Message ?? "Kaynak guncellendi.");
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
        public async Task<IActionResult> Save(ResourceCreateViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);
            ValidateEditorModel(model);

            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }

            var result = await _resourceService.CreateResourceAsync(MapToUpsertDto(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Kaynak kaydedilemedi.");
                return View("Create", model);
            }

            ShowSuccess(result.Message ?? "Kaynak kaydedildi.");
            if (model.AutomaticForward)
            {
                return RedirectToAction("Create", new
                {
                    functionId = model.FunctionId,
                    resoCatId = model.ResoCatId,
                    code = model.Code
                });
            }

            return RedirectToAction("Index");
        }

        private string BuildCurrentReturnUrl()
        {
            var path = Request.Path.HasValue ? Request.Path.Value : "/Resource/List";
            var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            return (path ?? "/Resource/List") + query;
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

        private async Task<List<ResourceLanguageOptionViewModel>> GetLanguageOptionsAsync()
        {
            var result = await _resourceService.GetLanguagesAsync();
            if (!result.IsSuccess || result.Data == null)
            {
                return new List<ResourceLanguageOptionViewModel>();
            }

            return result.Data
                .Select(x => new ResourceLanguageOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }

        private async Task<List<ResourceFunctionOptionViewModel>> GetFunctionOptionsAsync()
        {
            var result = await _resourceService.GetFunctionOptionsAsync();
            if (!result.IsSuccess || result.Data == null)
            {
                return new List<ResourceFunctionOptionViewModel>();
            }

            return result.Data
                .Select(x => new ResourceFunctionOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }

        private async Task<List<ResourceResoCatOptionViewModel>> GetResoCatOptionsAsync()
        {
            var result = await _resourceService.GetResoCatOptionsAsync();
            if (!result.IsSuccess || result.Data == null)
            {
                return new List<ResourceResoCatOptionViewModel>();
            }

            return result.Data
                .Select(x => new ResourceResoCatOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }

        private async Task EnsureEditorDependenciesAsync(ResourceEditorViewModelBase model)
        {
            if (model == null)
            {
                return;
            }

            var languages = await GetLanguageOptionsAsync();
            var functions = await GetFunctionOptionsAsync();
            var resoCats = await GetResoCatOptionsAsync();

            model.Languages = languages;
            model.FunctionOptions = functions;
            model.ResoCatOptions = resoCats;
            model.Code = NormalizeCode(model.Code);
            model.Names ??= new List<ResourceLocalizedNameViewModel>();

            var defaultLanguageId = languages.Count > 0 ? languages[0].Id : 1m;
            if (model.Names.Count == 0)
            {
                model.Names.Add(new ResourceLocalizedNameViewModel
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

            if (model.FunctionId.HasValue && functions.All(x => x.Id != model.FunctionId.Value))
            {
                model.FunctionId = null;
            }

            if (model.ResoCatId.HasValue && resoCats.All(x => x.Id != model.ResoCatId.Value))
            {
                model.ResoCatId = null;
            }
        }

        private void ValidateEditorModel(ResourceEditorViewModelBase model)
        {
            if (!model.FunctionId.HasValue || model.FunctionId.Value <= 0)
            {
                ModelState.AddModelError(nameof(model.FunctionId), "Fonksiyon secimi zorunludur.");
            }

            if (!model.ResoCatId.HasValue || model.ResoCatId.Value <= 0)
            {
                ModelState.AddModelError(nameof(model.ResoCatId), "Kaynak kategorisi secimi zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(model.Code))
            {
                ModelState.AddModelError(nameof(model.Code), "Kaynak kodu zorunludur.");
            }

            if (model.Sequence < 0)
            {
                ModelState.AddModelError(nameof(model.Sequence), "Sira no en az 0 olabilir.");
            }
        }

        private static ResourceListItemViewModel MapListItem(ResourceListItemDto dto)
        {
            return new ResourceListItemViewModel
            {
                Id = dto.Id,
                Sequence = dto.Sequence,
                Code = dto.Code,
                Name = dto.Name,
                FunctionName = dto.FunctionName,
                ResoCatName = dto.ResoCatName,
                IsInvalid = dto.Invisible
            };
        }

        private static ResourceDetailViewModel MapDetail(ResourceDetailDto dto)
        {
            return new ResourceDetailViewModel
            {
                Id = dto.Id,
                FunctionId = dto.FunctionId,
                FunctionName = dto.FunctionName,
                ResoCatId = dto.ResoCatId,
                ResoCatName = dto.ResoCatName,
                Code = dto.Code,
                Sequence = dto.Sequence,
                IsInvalid = dto.Invisible,
                Names = dto.Names.Select(MapLocalizedName).ToList()
            };
        }

        private static ResourceEditViewModel MapEdit(
            ResourceDetailDto dto,
            List<ResourceLanguageOptionViewModel> languages,
            List<ResourceFunctionOptionViewModel> functions,
            List<ResourceResoCatOptionViewModel> resoCats)
        {
            return new ResourceEditViewModel
            {
                Id = dto.Id,
                FunctionId = dto.FunctionId,
                FunctionName = dto.FunctionName,
                ResoCatId = dto.ResoCatId,
                ResoCatName = dto.ResoCatName,
                Code = dto.Code,
                Sequence = dto.Sequence,
                IsInvalid = dto.Invisible,
                Languages = languages,
                FunctionOptions = functions,
                ResoCatOptions = resoCats,
                Names = dto.Names.Select(MapLocalizedName).ToList()
            };
        }

        private static ResourceLocalizedNameViewModel MapLocalizedName(ResourceLocalizedNameDto dto)
        {
            return new ResourceLocalizedNameViewModel
            {
                LanguageId = dto.LanguageId,
                LanguageName = dto.LanguageName,
                Name = dto.Name
            };
        }

        private static ResourceUpsertDto MapToUpsertDto(ResourceEditorViewModelBase model)
        {
            return new ResourceUpsertDto
            {
                Id = model.Id,
                FunctionId = model.FunctionId ?? 0,
                ResoCatId = model.ResoCatId ?? 0,
                Sequence = model.Sequence,
                Code = NormalizeCode(model.Code),
                Invisible = model.IsInvalid,
                Names = model.Names
                    .Where(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name))
                    .Select(x => new ResourceLocalizedNameInputDto
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
    }
}
