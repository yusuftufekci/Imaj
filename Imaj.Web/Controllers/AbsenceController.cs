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
    public class AbsenceController : BaseController
    {
        private const double AddMethodId = 1086d;
        private const double BrowseMethodId = 1087d;
        private const double ViewMethodId = 1105d;
        private const decimal ConfirmMethodId = 1133m;
        private const decimal UndoConfirmMethodId = 1132m;
        private const decimal UtilizeMethodId = 1135m;
        private const decimal UndoUtilizeMethodId = 1134m;
        private const decimal WasteMethodId = 1136m;
        private const decimal UndoWasteMethodId = 1137m;
        private const decimal DropMethodId = 1138m;
        private const decimal DiscardMethodId = 1123m;
        private const decimal EvaluateMethodId = 1140m;
        private const decimal UndoEvaluateMethodId = 1486m;
        private const decimal ChangeStartDateMethodId = 1177m;
        private const decimal ChangeEndDateMethodId = 1178m;
        private const decimal MoveStartDateMethodId = 2972m;
        private const double ViewLogMethodId = 1124d;
        private const decimal OpenStateId = 10m;

        private readonly IAbsenceService _absenceService;
        private readonly IPermissionViewService _permissionViewService;

        public AbsenceController(
            IAbsenceService absenceService,
            IPermissionViewService permissionViewService,
            ILogger<AbsenceController> logger,
            IStringLocalizer<SharedResource> localizer)
            : base(logger, localizer)
        {
            _absenceService = absenceService;
            _permissionViewService = permissionViewService;
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> Index()
        {
            var functionOptions = await GetFunctionOptionsAsync();
            var reasonOptions = await GetReasonOptionsAsync();
            var stateOptions = await GetStateOptionsAsync();
            var now = DateTime.Now;

            return View(new AbsenceIndexViewModel
            {
                Filter = new AbsenceFilterModel
                {
                    Page = 1,
                    PageSize = 16
                },
                FunctionOptions = functionOptions,
                ReasonOptions = reasonOptions,
                StateOptions = stateOptions,
                CreateFunctionId = functionOptions.Count > 0 ? functionOptions[0].Id : null,
                CreateStartDate = now,
                CreateEndDate = now
            });
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> List(AbsenceFilterModel filter)
        {
            var normalizedFilter = filter ?? new AbsenceFilterModel();
            normalizedFilter.Page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            normalizedFilter.PageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;
            normalizedFilter.First = normalizedFilter.First.HasValue && normalizedFilter.First.Value > 0 ? normalizedFilter.First.Value : normalizedFilter.PageSize;
            normalizedFilter.ResourceIds = normalizedFilter.ResourceIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (normalizedFilter.StartDateFrom.HasValue
                && normalizedFilter.StartDateTo.HasValue
                && normalizedFilter.StartDateFrom.Value > normalizedFilter.StartDateTo.Value)
            {
                ModelState.AddModelError(string.Empty, L("StartDateRangeInvalid"));
                normalizedFilter.StartDateTo = normalizedFilter.StartDateFrom;
            }

            if (normalizedFilter.EndDateFrom.HasValue
                && normalizedFilter.EndDateTo.HasValue
                && normalizedFilter.EndDateFrom.Value > normalizedFilter.EndDateTo.Value)
            {
                ModelState.AddModelError(string.Empty, L("EndDateRangeInvalid"));
                normalizedFilter.EndDateTo = normalizedFilter.EndDateFrom;
            }

            var result = await _absenceService.GetAbsencesAsync(new AbsenceFilterDto
            {
                FunctionId = normalizedFilter.FunctionId,
                ReasonId = normalizedFilter.ReasonId,
                Name = normalizedFilter.Name,
                Contact = normalizedFilter.Contact,
                StartDateFrom = normalizedFilter.StartDateFrom,
                StartDateTo = normalizedFilter.StartDateTo,
                EndDateFrom = normalizedFilter.EndDateFrom,
                EndDateTo = normalizedFilter.EndDateTo,
                StateId = normalizedFilter.StateId,
                Evaluated = normalizedFilter.Evaluated,
                ResourceIds = normalizedFilter.ResourceIds,
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize,
                First = normalizedFilter.First
            });

            var model = new AbsenceListViewModel
            {
                Items = result.IsSuccess && result.Data != null
                    ? result.Data.Items.Select(MapListItem).ToList()
                    : new List<AbsenceListItemViewModel>(),
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

            var detailResult = await _absenceService.GetAbsenceDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? L("AbsenceNotFound"));
                return RedirectToAction("List");
            }

            var model = MapDetail(detailResult.Data);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/Absence/List");

            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(ViewLogMethodId)]
        public async Task<IActionResult> History(decimal? id, string[]? selectedIds = null, int currentIndex = 0, string? returnUrl = null)
        {
            var resolved = ResolveSelection(id, selectedIds, currentIndex);
            if (!resolved.ResolvedId.HasValue)
            {
                return RedirectToAction("Index");
            }

            var detailResult = await _absenceService.GetAbsenceDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? L("AbsenceNotFound"));
                return RedirectToAction("List");
            }

            var historyResult = await _absenceService.GetAbsenceHistoryAsync(resolved.ResolvedId.Value);
            if (!historyResult.IsSuccess || historyResult.Data == null)
            {
                ShowError(historyResult.Message ?? L("GenericError"));
                return RedirectToAction("Detail", new
                {
                    id = resolved.ResolvedId.Value,
                    selectedIds = resolved.SelectedIds,
                    currentIndex = resolved.CurrentIndex,
                    returnUrl = NormalizeReturnUrl(returnUrl, "/Absence/List")
                });
            }

            var detailModel = MapDetail(detailResult.Data);
            detailModel.SelectedIds = resolved.SelectedIds;
            detailModel.CurrentIndex = resolved.CurrentIndex;
            detailModel.TotalSelected = resolved.SelectedIds.Count;
            detailModel.ReturnUrl = NormalizeReturnUrl(returnUrl, "/Absence/List");

            var model = new AbsenceHistoryViewModel
            {
                Absence = detailModel,
                Items = historyResult.Data.Select(x => new AbsenceHistoryItemViewModel
                {
                    Date = x.LogDate,
                    UserCode = x.UserCode,
                    UserName = x.UserName,
                    Action = x.ActionName
                }).ToList(),
                ReturnUrl = NormalizeReturnUrl(returnUrl, "/Absence/List")
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WorkflowAction(AbsenceWorkflowActionRequest request)
        {
            if (request.Id <= 0)
            {
                ShowError(L("AbsenceNotFound"));
                return RedirectToAction("List");
            }

            var methodId = ResolveWorkflowMethodId(request.Action);
            if (!methodId.HasValue)
            {
                ShowError(L("GenericError"));
                return RedirectToAbsenceDetail(request);
            }

            var hasPermission = await _permissionViewService.CanExecuteMethodAsync(methodId.Value, write: true);
            if (!hasPermission)
            {
                ShowError(L("AccessDeniedMessage"));
                return RedirectToAbsenceDetail(request);
            }

            var result = await _absenceService.ExecuteWorkflowActionAsync(request.Id, request.Action);
            if (result.IsSuccess)
            {
                ShowSuccess(result.Message ?? L("SuccessTitle"));
            }
            else
            {
                ShowError(result.Message ?? L("GenericError"));
            }

            return RedirectToAbsenceDetail(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleAction(AbsenceScheduleActionRequest request)
        {
            if (request.Id <= 0)
            {
                ShowError(L("AbsenceNotFound"));
                return RedirectToAction("List");
            }

            if (request.NewDate == default)
            {
                ShowError(L("PleaseEnterStartDate"));
                return RedirectToAbsenceDetail(new AbsenceWorkflowActionRequest
                {
                    Id = request.Id,
                    SelectedIds = request.SelectedIds,
                    CurrentIndex = request.CurrentIndex,
                    ReturnUrl = request.ReturnUrl
                });
            }

            var methodId = ResolveScheduleMethodId(request.Action);
            if (!methodId.HasValue)
            {
                ShowError(L("GenericError"));
                return RedirectToAbsenceDetail(new AbsenceWorkflowActionRequest
                {
                    Id = request.Id,
                    SelectedIds = request.SelectedIds,
                    CurrentIndex = request.CurrentIndex,
                    ReturnUrl = request.ReturnUrl
                });
            }

            var hasPermission = await _permissionViewService.CanExecuteMethodAsync(methodId.Value, write: true);
            if (!hasPermission)
            {
                ShowError(L("AccessDeniedMessage"));
                return RedirectToAbsenceDetail(new AbsenceWorkflowActionRequest
                {
                    Id = request.Id,
                    SelectedIds = request.SelectedIds,
                    CurrentIndex = request.CurrentIndex,
                    ReturnUrl = request.ReturnUrl
                });
            }

            var result = await _absenceService.UpdateAbsenceScheduleAsync(request.Id, request.NewDate, request.Action);
            if (result.IsSuccess)
            {
                ShowSuccess(result.Message ?? L("SuccessTitle"));
            }
            else
            {
                ShowError(result.Message ?? L("GenericError"));
            }

            return RedirectToAbsenceDetail(new AbsenceWorkflowActionRequest
            {
                Id = request.Id,
                SelectedIds = request.SelectedIds,
                CurrentIndex = request.CurrentIndex,
                ReturnUrl = request.ReturnUrl
            });
        }

        [HttpGet]
        [RequireMethodPermission(AddMethodId)]
        public async Task<IActionResult> Create(decimal? functionId = null, DateTime? startDate = null, DateTime? endDate = null, string? returnUrl = null)
        {
            var functionOptions = await GetFunctionOptionsAsync();
            var reasonOptions = await GetReasonOptionsAsync();
            var openStateName = await GetOpenStateNameAsync();

            var now = DateTime.Now;
            var resolvedStartDate = startDate ?? now;
            var resolvedEndDate = endDate ?? resolvedStartDate;

            if (resolvedEndDate < resolvedStartDate)
            {
                resolvedEndDate = resolvedStartDate;
            }

            var model = new AbsenceCreateViewModel
            {
                FunctionOptions = functionOptions,
                ReasonOptions = reasonOptions,
                FunctionId = ResolveFunctionSelection(functionId, functionOptions),
                ReasonId = reasonOptions.Count > 0 ? reasonOptions[0].Id : null,
                StartDate = resolvedStartDate,
                EndDate = resolvedEndDate,
                StateId = OpenStateId,
                StateName = openStateName,
                ReturnUrl = NormalizeReturnUrl(returnUrl, "/Absence")
            };

            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> SearchResources([FromQuery] AbsenceResourceLookupFilterModel filter)
        {
            var normalizedFilter = filter ?? new AbsenceResourceLookupFilterModel();

            var result = await _absenceService.SearchResourcesAsync(new AbsenceResourceLookupFilterDto
            {
                Code = normalizedFilter.Code,
                Name = normalizedFilter.Name,
                FunctionId = normalizedFilter.FunctionId,
                IsInvalid = normalizedFilter.IsInvalid,
                ExcludeIds = ParseDecimalCsv(normalizedFilter.ExcludeIds),
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize,
                First = normalizedFilter.First
            });

            if (!result.IsSuccess || result.Data == null)
            {
                return BadRequest(Imaj.Web.Extensions.ControllerMessageLocalizationExtensions.LocalizeUiMessage(this, result.Message, L("ResourceListUnavailable")));
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
        [RequireMethodPermission(AddMethodId, write: true)]
        public async Task<IActionResult> Save(AbsenceCreateViewModel model)
        {
            await EnsureCreateDependenciesAsync(model);
            ValidateCreateModel(model);

            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }

            var result = await _absenceService.CreateAbsenceAsync(MapToCreateDto(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? L("AbsenceSaveFailed"));
                return View("Create", model);
            }

            ShowSuccess(result.Message ?? L("AbsenceSavedSuccess"));
            return RedirectToAction("Index");
        }

        private IActionResult RedirectToAbsenceDetail(AbsenceWorkflowActionRequest request)
        {
            var selectedIds = request.SelectedIds?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList() ?? new List<string>();

            var idText = request.Id.ToString(CultureInfo.InvariantCulture);
            if (!selectedIds.Contains(idText))
            {
                selectedIds.Add(idText);
            }

            var currentIndex = request.CurrentIndex;
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }
            if (currentIndex >= selectedIds.Count)
            {
                currentIndex = selectedIds.Count - 1;
            }

            var returnUrl = string.IsNullOrWhiteSpace(request.ReturnUrl) || !request.ReturnUrl.StartsWith('/')
                ? "/Absence/List"
                : request.ReturnUrl;

            return RedirectToAction("Detail", new
            {
                id = idText,
                selectedIds,
                currentIndex,
                returnUrl
            });
        }

        private static decimal? ResolveWorkflowMethodId(AbsenceWorkflowAction action)
        {
            return action switch
            {
                AbsenceWorkflowAction.Confirm => ConfirmMethodId,
                AbsenceWorkflowAction.UndoConfirm => UndoConfirmMethodId,
                AbsenceWorkflowAction.Utilize => UtilizeMethodId,
                AbsenceWorkflowAction.UndoUtilize => UndoUtilizeMethodId,
                AbsenceWorkflowAction.Waste => WasteMethodId,
                AbsenceWorkflowAction.UndoWaste => UndoWasteMethodId,
                AbsenceWorkflowAction.Drop => DropMethodId,
                AbsenceWorkflowAction.Discard => DiscardMethodId,
                AbsenceWorkflowAction.Evaluate => EvaluateMethodId,
                AbsenceWorkflowAction.UndoEvaluate => UndoEvaluateMethodId,
                _ => null
            };
        }

        private static decimal? ResolveScheduleMethodId(AbsenceScheduleAction action)
        {
            return action switch
            {
                AbsenceScheduleAction.ChangeStartDate => ChangeStartDateMethodId,
                AbsenceScheduleAction.ChangeEndDate => ChangeEndDateMethodId,
                AbsenceScheduleAction.MoveStartDate => MoveStartDateMethodId,
                _ => null
            };
        }

        private string BuildCurrentReturnUrl()
        {
            var path = Request.Path.HasValue ? Request.Path.Value : "/Absence/List";
            var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            return (path ?? "/Absence/List") + query;
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

        private async Task<List<AbsenceFunctionOptionViewModel>> GetFunctionOptionsAsync()
        {
            var result = await _absenceService.GetFunctionOptionsAsync();
            if (!result.IsSuccess || result.Data == null)
            {
                return new List<AbsenceFunctionOptionViewModel>();
            }

            return result.Data
                .Select(x => new AbsenceFunctionOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }

        private async Task<List<AbsenceReasonOptionViewModel>> GetReasonOptionsAsync()
        {
            var result = await _absenceService.GetReasonOptionsAsync();
            if (!result.IsSuccess || result.Data == null)
            {
                return new List<AbsenceReasonOptionViewModel>();
            }

            return result.Data
                .Select(x => new AbsenceReasonOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }

        private async Task<List<AbsenceStateOptionViewModel>> GetStateOptionsAsync()
        {
            var result = await _absenceService.GetStateOptionsAsync();
            if (!result.IsSuccess || result.Data == null)
            {
                return new List<AbsenceStateOptionViewModel>();
            }

            return result.Data
                .Select(x => new AbsenceStateOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }

        private async Task<string> GetOpenStateNameAsync()
        {
            var states = await GetStateOptionsAsync();
            return states.FirstOrDefault(x => x.Id == OpenStateId)?.Name ?? "Acik";
        }

        private async Task EnsureCreateDependenciesAsync(AbsenceCreateViewModel model)
        {
            if (model == null)
            {
                return;
            }

            var functions = await GetFunctionOptionsAsync();
            var reasons = await GetReasonOptionsAsync();

            model.FunctionOptions = functions;
            model.ReasonOptions = reasons;
            model.Resources ??= new List<AbsenceResourceViewModel>();
            model.Resources = model.Resources
                .Where(x => x.ResourceId > 0)
                .GroupBy(x => x.ResourceId)
                .Select(g => g.First())
                .ToList();

            if (model.FunctionId.HasValue && functions.All(x => x.Id != model.FunctionId.Value))
            {
                model.FunctionId = null;
            }

            if (model.ReasonId.HasValue && reasons.All(x => x.Id != model.ReasonId.Value))
            {
                model.ReasonId = null;
            }

            model.StateId = OpenStateId;
            model.StateName = await GetOpenStateNameAsync();
            model.Name = (model.Name ?? string.Empty).Trim();
            model.Contact = (model.Contact ?? string.Empty).Trim();
            model.Notes = model.Notes ?? string.Empty;
        }

        private void ValidateCreateModel(AbsenceCreateViewModel model)
        {
            if (!model.FunctionId.HasValue || model.FunctionId.Value <= 0)
            {
                ModelState.AddModelError(nameof(model.FunctionId), L("FunctionSelectionRequired"));
            }

            if (!model.ReasonId.HasValue || model.ReasonId.Value <= 0)
            {
                ModelState.AddModelError(nameof(model.ReasonId), L("ReasonSelectionRequired"));
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), L("NameFieldRequired"));
            }

            if (model.StartDate >= model.EndDate)
            {
                ModelState.AddModelError(nameof(model.EndDate), L("EndDateMustBeAfterStartDate"));
            }
        }

        private static decimal? ResolveFunctionSelection(decimal? requestedFunctionId, List<AbsenceFunctionOptionViewModel> options)
        {
            if (requestedFunctionId.HasValue
                && requestedFunctionId.Value > 0
                && options.Any(x => x.Id == requestedFunctionId.Value))
            {
                return requestedFunctionId.Value;
            }

            return options.Count > 0 ? options[0].Id : null;
        }

        private static AbsenceListItemViewModel MapListItem(AbsenceListItemDto dto)
        {
            return new AbsenceListItemViewModel
            {
                Id = dto.Id,
                FunctionId = dto.FunctionId,
                FunctionName = dto.FunctionName,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                ReasonId = dto.ReasonId,
                ReasonName = dto.ReasonName,
                Name = dto.Name,
                StateId = dto.StateId,
                StateName = dto.StateName,
                Evaluated = dto.Evaluated
            };
        }

        private static AbsenceDetailViewModel MapDetail(AbsenceDetailDto dto)
        {
            return new AbsenceDetailViewModel
            {
                Id = dto.Id,
                FunctionId = dto.FunctionId,
                FunctionName = dto.FunctionName,
                ReasonId = dto.ReasonId,
                ReasonName = dto.ReasonName,
                Name = dto.Name,
                Contact = dto.Contact,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                StateId = dto.StateId,
                StateName = dto.StateName,
                Evaluated = dto.Evaluated,
                Notes = dto.Notes,
                Resources = dto.Resources.Select(MapResource).ToList()
            };
        }

        private static AbsenceResourceViewModel MapResource(AbsenceResourceItemDto dto)
        {
            return new AbsenceResourceViewModel
            {
                ResourceId = dto.ResourceId,
                Code = dto.Code,
                Name = dto.Name,
                FunctionId = dto.FunctionId,
                FunctionName = dto.FunctionName,
                ResoCatId = dto.ResoCatId,
                ResoCatName = dto.ResoCatName,
                Invisible = dto.Invisible
            };
        }

        private static AbsenceCreateDto MapToCreateDto(AbsenceCreateViewModel model)
        {
            return new AbsenceCreateDto
            {
                FunctionId = model.FunctionId ?? 0,
                ReasonId = model.ReasonId ?? 0,
                Name = model.Name,
                Contact = model.Contact,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Evaluated = model.Evaluated,
                Notes = model.Notes,
                ResourceIds = model.Resources
                    .Where(x => x.ResourceId > 0)
                    .Select(x => x.ResourceId)
                    .Distinct()
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
    }
}
