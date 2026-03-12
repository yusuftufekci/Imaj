using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Web;
using Imaj.Web.Authorization;
using Imaj.Web.Controllers.Base;
using Imaj.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Imaj.Web.Controllers
{
    public class FunctionController : BaseController
    {
        private const double AddMethodId = 1002d;
        private const double BrowseMethodId = 1003d;
        private const double ViewMethodId = 1004d;
        private const double EditMethodId = 1005d;

        private readonly IFunctionService _functionService;

        public FunctionController(
            IFunctionService functionService,
            ILogger<FunctionController> logger,
            IStringLocalizer<SharedResource> localizer)
            : base(logger, localizer)
        {
            _functionService = functionService;
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> Index()
        {
            var intervals = await GetIntervalOptionsAsync();
            var model = new FunctionIndexViewModel
            {
                Filter = new FunctionFilterModel
                {
                    Page = 1,
                    PageSize = 16
                },
                Intervals = intervals
            };

            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> List(FunctionFilterModel filter)
        {
            var normalizedFilter = filter ?? new FunctionFilterModel();
            normalizedFilter.Page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            normalizedFilter.PageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;
            normalizedFilter.First = normalizedFilter.First.HasValue && normalizedFilter.First.Value > 0 ? normalizedFilter.First.Value : normalizedFilter.PageSize;

            var result = await _functionService.GetFunctionsAsync(new FunctionFilterDto
            {
                Reservable = normalizedFilter.Reservable,
                IntervalId = normalizedFilter.IntervalId,
                IsInvalid = normalizedFilter.IsInvalid,
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize,
                First = normalizedFilter.First
            });

            var model = new FunctionListViewModel
            {
                Items = result.IsSuccess && result.Data != null
                    ? result.Data.Items.Select(MapListItem).ToList()
                    : new List<FunctionListItemViewModel>(),
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

            var normalizedReturnUrl = NormalizeReturnUrl(returnUrl, "/Function/List");
            if (Microsoft.AspNetCore.Http.HttpMethods.IsPost(Request.Method))
            {
                return RedirectToAction(nameof(Detail), new
                {
                    id = resolved.ResolvedId.Value,
                    selectedIds = resolved.SelectedIds,
                    currentIndex = resolved.CurrentIndex,
                    returnUrl = normalizedReturnUrl
                });
            }

            var detailResult = await _functionService.GetFunctionDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? L("FunctionNotFound"));
                return RedirectToAction("List");
            }

            var model = MapDetail(detailResult.Data);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = normalizedReturnUrl;

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

            var normalizedReturnUrl = NormalizeReturnUrl(returnUrl, "/Function/List");
            if (Microsoft.AspNetCore.Http.HttpMethods.IsPost(Request.Method))
            {
                return RedirectToAction(nameof(Edit), new
                {
                    id = resolved.ResolvedId.Value,
                    selectedIds = resolved.SelectedIds,
                    currentIndex = resolved.CurrentIndex,
                    returnUrl = normalizedReturnUrl
                });
            }

            var detailResult = await _functionService.GetFunctionDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? L("FunctionNotFound"));
                return RedirectToAction("List");
            }

            var languageOptions = await GetLanguageOptionsAsync();
            var intervalOptions = await GetIntervalOptionsAsync();
            var model = MapEdit(detailResult.Data, languageOptions, intervalOptions);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = normalizedReturnUrl;

            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(AddMethodId)]
        public async Task<IActionResult> Create(bool? reservable = null, decimal? intervalId = null)
        {
            var languageOptions = await GetLanguageOptionsAsync();
            var intervalOptions = await GetIntervalOptionsAsync();
            var normalizedReservable = reservable == true;
            var normalizedIntervalId = intervalId.HasValue && intervalId.Value > 0 ? intervalId : null;

            if (!IsReservationPairValid(normalizedReservable, normalizedIntervalId))
            {
                ShowError(L("ReservationPairRequired"));
                return RedirectToAction(nameof(Index));
            }

            var normalizedIntervalName = normalizedIntervalId.HasValue
                ? intervalOptions.FirstOrDefault(x => x.Id == normalizedIntervalId.Value)?.Name ?? string.Empty
                : string.Empty;

            var model = new FunctionCreateViewModel
            {
                Reservable = normalizedReservable,
                IntervalId = normalizedIntervalId,
                Languages = languageOptions,
                Intervals = intervalOptions,
                Names = BuildCreateLocalizedNames(languageOptions, null),
                SourceReservable = normalizedReservable,
                SourceIntervalId = normalizedIntervalId,
                SourceIntervalName = normalizedIntervalName,
                IntervalName = normalizedIntervalName
            };

            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> SearchProducts([FromQuery] FunctionProductLookupFilterModel filter)
        {
            var normalizedFilter = filter ?? new FunctionProductLookupFilterModel();

            var result = await _functionService.SearchProductsAsync(new FunctionProductLookupFilterDto
            {
                Code = normalizedFilter.Code,
                Name = normalizedFilter.Name,
                IsInvalid = normalizedFilter.IsInvalid,
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize,
                First = normalizedFilter.First,
                ExcludeIds = ParseDecimalCsv(normalizedFilter.ExcludeIds)
            });

            if (!result.IsSuccess || result.Data == null)
            {
                return BadRequest(Imaj.Web.Extensions.ControllerMessageLocalizationExtensions.LocalizeUiMessage(this, result.Message, L("ProductListUnavailable")));
            }

            return Json(new
            {
                items = result.Data.Items,
                totalCount = result.Data.TotalCount,
                page = result.Data.Page,
                pageSize = result.Data.PageSize
            });
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> SearchResoCategories([FromQuery] FunctionResoCatLookupFilterModel filter)
        {
            var normalizedFilter = filter ?? new FunctionResoCatLookupFilterModel();

            var result = await _functionService.SearchResoCategoriesAsync(new FunctionResoCatLookupFilterDto
            {
                Name = normalizedFilter.Name,
                IsInvalid = normalizedFilter.IsInvalid,
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize,
                First = normalizedFilter.First,
                ExcludeIds = ParseDecimalCsv(normalizedFilter.ExcludeIds)
            });

            if (!result.IsSuccess || result.Data == null)
            {
                return BadRequest(Imaj.Web.Extensions.ControllerMessageLocalizationExtensions.LocalizeUiMessage(this, result.Message, L("ResoCatListUnavailable")));
            }

            return Json(new
            {
                items = result.Data.Items,
                totalCount = result.Data.TotalCount,
                page = result.Data.Page,
                pageSize = result.Data.PageSize
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(EditMethodId, write: true)]
        public async Task<IActionResult> Update(FunctionEditViewModel model)
        {
            if (!model.Id.HasValue || model.Id.Value <= 0)
            {
                ShowError(L("FunctionIdRequired"));
                return RedirectToAction("List");
            }

            var currentDetail = await _functionService.GetFunctionDetailAsync(model.Id.Value);
            if (!currentDetail.IsSuccess || currentDetail.Data == null)
            {
                ShowError(currentDetail.Message ?? L("FunctionNotFound"));
                return RedirectToAction("List");
            }

            // Legacy parity: edit ekraninda bu alanlar degismez, mevcut kayittan sabitlenir.
            model.Reservable = currentDetail.Data.Reservable;
            model.IntervalId = currentDetail.Data.IntervalId;
            model.IntervalName = currentDetail.Data.IntervalName;

            await EnsureEditorDependenciesAsync(model);
            ValidateReservationPair(model);

            if (!ModelState.IsValid)
            {
                return View("Edit", model);
            }

            var result = await _functionService.UpdateFunctionAsync(MapToUpsertDto(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, Ui(result.Message, L("FunctionUpdateFailed")));
                return View("Edit", model);
            }

            ShowSuccess(result.Message ?? L("FunctionUpdatedSuccess"));
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
        public async Task<IActionResult> Save(FunctionCreateViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);
            ValidateReservationPair(model);

            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }

            var result = await _functionService.CreateFunctionAsync(MapToUpsertDto(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, Ui(result.Message, L("FunctionSaveFailed")));
                return View("Create", model);
            }

            ShowSuccess(result.Message ?? L("FunctionSavedSuccess"));

            return RedirectToAction("Index");
        }

        private string BuildCurrentReturnUrl()
        {
            var path = Request.Path.HasValue ? Request.Path.Value : "/Function/List";
            var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            return (path ?? "/Function/List") + query;
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

        private async Task<List<FunctionLanguageOptionViewModel>> GetLanguageOptionsAsync()
        {
            var languagesResult = await _functionService.GetLanguagesAsync();
            if (!languagesResult.IsSuccess || languagesResult.Data == null)
            {
                return new List<FunctionLanguageOptionViewModel>();
            }

            return languagesResult.Data
                .Select(x => new FunctionLanguageOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }

        private async Task<List<FunctionIntervalOptionViewModel>> GetIntervalOptionsAsync()
        {
            var intervalsResult = await _functionService.GetIntervalsAsync();
            if (!intervalsResult.IsSuccess || intervalsResult.Data == null)
            {
                return new List<FunctionIntervalOptionViewModel>();
            }

            return intervalsResult.Data
                .Select(x => new FunctionIntervalOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }

        private async Task EnsureEditorDependenciesAsync(FunctionEditorViewModelBase model)
        {
            if (model == null)
            {
                return;
            }

            var languages = await GetLanguageOptionsAsync();
            var intervals = await GetIntervalOptionsAsync();

            model.Languages = languages;
            model.Intervals = intervals;
            model.Names ??= new List<FunctionLocalizedNameViewModel>();
            model.Products ??= new List<FunctionProductAssignmentViewModel>();
            model.Rules ??= new List<FunctionRuleViewModel>();

            if (model is FunctionCreateViewModel createModel)
            {
                model.Names = BuildCreateLocalizedNames(languages, model.Names);

                var sourceIntervalId = createModel.SourceIntervalId.HasValue && createModel.SourceIntervalId.Value > 0
                    ? createModel.SourceIntervalId
                    : null;

                var sourceIntervalName = sourceIntervalId.HasValue
                    ? intervals.FirstOrDefault(x => x.Id == sourceIntervalId.Value)?.Name ?? string.Empty
                    : string.Empty;

                createModel.SourceIntervalId = sourceIntervalId;
                createModel.SourceIntervalName = sourceIntervalName;
                createModel.IntervalId = sourceIntervalId;
                createModel.IntervalName = sourceIntervalName;
                createModel.Reservable = createModel.SourceReservable;
            }

            var defaultLanguageId = languages.Count > 0 ? languages[0].Id : 1m;
            if (model.Names.Count == 0)
            {
                model.Names.Add(new FunctionLocalizedNameViewModel
                {
                    LanguageId = defaultLanguageId
                });
            }

            foreach (var name in model.Names)
            {
                if (name.LanguageId <= 0)
                {
                    name.LanguageId = defaultLanguageId;
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

            foreach (var rule in model.Rules)
            {
                rule.ResoCats ??= new List<FunctionRuleResoCatViewModel>();
            }
        }

        private void ValidateReservationPair(FunctionEditorViewModelBase model)
        {
            var intervalId = model.IntervalId.HasValue && model.IntervalId.Value > 0
                ? model.IntervalId
                : null;

            model.IntervalId = intervalId;

            if (!IsReservationPairValid(model.Reservable, intervalId))
            {
                ModelState.AddModelError(string.Empty, L("ReservationPairRequired"));
            }
        }

        private static bool IsReservationPairValid(bool reservable, decimal? intervalId)
        {
            var hasInterval = intervalId.HasValue && intervalId.Value > 0;
            return reservable == hasInterval;
        }

        private static FunctionListItemViewModel MapListItem(FunctionListItemDto dto)
        {
            return new FunctionListItemViewModel
            {
                Id = dto.Id,
                Name = dto.Name,
                Reservable = dto.Reservable,
                IntervalId = dto.IntervalId,
                IntervalName = dto.IntervalName,
                IsInvalid = dto.Invisible
            };
        }

        private static FunctionDetailViewModel MapDetail(FunctionDetailDto dto)
        {
            return new FunctionDetailViewModel
            {
                Id = dto.Id,
                Reservable = dto.Reservable,
                WorkMandatory = dto.WorkMandatory,
                ProdMandatory = dto.ProdMandatory,
                IsInvalid = dto.Invisible,
                IntervalId = dto.IntervalId,
                IntervalName = dto.IntervalName,
                Names = dto.Names.Select(MapLocalizedName).ToList(),
                Products = dto.Products.Select(MapProduct).ToList(),
                Rules = dto.Rules.Select(MapRule).ToList()
            };
        }

        private static FunctionEditViewModel MapEdit(
            FunctionDetailDto dto,
            List<FunctionLanguageOptionViewModel> languages,
            List<FunctionIntervalOptionViewModel> intervals)
        {
            return new FunctionEditViewModel
            {
                Id = dto.Id,
                Reservable = dto.Reservable,
                WorkMandatory = dto.WorkMandatory,
                ProdMandatory = dto.ProdMandatory,
                IsInvalid = dto.Invisible,
                IntervalId = dto.IntervalId,
                IntervalName = dto.IntervalName,
                Languages = languages,
                Intervals = intervals,
                Names = dto.Names.Select(MapLocalizedName).ToList(),
                Products = dto.Products.Select(MapProduct).ToList(),
                Rules = dto.Rules.Select(MapRule).ToList()
            };
        }

        private static FunctionLocalizedNameViewModel MapLocalizedName(FunctionLocalizedNameDto dto)
        {
            return new FunctionLocalizedNameViewModel
            {
                LanguageId = dto.LanguageId,
                LanguageName = dto.LanguageName,
                Name = dto.Name
            };
        }

        private static FunctionProductAssignmentViewModel MapProduct(FunctionProductAssignmentDto dto)
        {
            return new FunctionProductAssignmentViewModel
            {
                ProductId = dto.ProductId,
                Code = dto.Code,
                Name = dto.Name,
                Invisible = dto.Invisible
            };
        }

        private static FunctionRuleViewModel MapRule(FunctionRuleDto dto)
        {
            return new FunctionRuleViewModel
            {
                Name = dto.Name,
                MinValue = dto.MinValue,
                MaxValue = dto.MaxValue,
                ResoCats = dto.ResoCats
                    .Select(x => new FunctionRuleResoCatViewModel
                    {
                        ResoCatId = x.ResoCatId,
                        Name = x.Name,
                        Invisible = x.Invisible
                    })
                    .ToList()
            };
        }

        private static FunctionUpsertDto MapToUpsertDto(FunctionEditorViewModelBase model)
        {
            return new FunctionUpsertDto
            {
                Id = model.Id,
                Reservable = model.Reservable,
                WorkMandatory = model.WorkMandatory,
                ProdMandatory = model.ProdMandatory,
                Invisible = model.IsInvalid,
                IntervalId = model.IntervalId,
                Names = model.Names
                    .Where(x => x.LanguageId > 0 && !string.IsNullOrWhiteSpace(x.Name))
                    .Select(x => new FunctionLocalizedNameInputDto
                    {
                        LanguageId = x.LanguageId,
                        Name = x.Name
                    })
                    .ToList(),
                ProductIds = model.Products
                    .Where(x => x.ProductId > 0)
                    .Select(x => x.ProductId)
                    .Distinct()
                    .ToList(),
                Rules = model.Rules
                    .Where(x => !string.IsNullOrWhiteSpace(x.Name)
                                || x.MinValue != 0
                                || x.MaxValue != 0
                                || x.ResoCats.Any())
                    .Select(x => new FunctionRuleInputDto
                    {
                        Name = x.Name,
                        MinValue = x.MinValue,
                        MaxValue = x.MaxValue,
                        ResoCatIds = x.ResoCats
                            .Where(rc => rc.ResoCatId > 0)
                            .Select(rc => rc.ResoCatId)
                            .Distinct()
                            .ToList()
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

        private static List<decimal> ParseDecimalCsv(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
            {
                return new List<decimal>();
            }

            return csv
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => decimal.TryParse(x.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                    ? (decimal?)value
                    : null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();
        }

        private static List<FunctionLocalizedNameViewModel> BuildCreateLocalizedNames(
            IReadOnlyCollection<FunctionLanguageOptionViewModel> languages,
            IEnumerable<FunctionLocalizedNameViewModel>? existingNames)
        {
            var existingByLanguage = (existingNames ?? Enumerable.Empty<FunctionLocalizedNameViewModel>())
                .Where(x => x.LanguageId > 0)
                .GroupBy(x => x.LanguageId)
                .ToDictionary(x => x.Key, x => x.First());

            if (languages.Count == 0)
            {
                return existingByLanguage.Values
                    .OrderBy(x => x.LanguageId)
                    .Select(x => new FunctionLocalizedNameViewModel
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
                    return new FunctionLocalizedNameViewModel
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
