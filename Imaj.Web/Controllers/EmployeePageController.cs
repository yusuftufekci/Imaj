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
    [Route("Employee")]
    public class EmployeePageController : BaseController
    {
        private const double ViewMethodId = 1217d;
        private const double AddMethodId = 1218d;
        private const double BrowseMethodId = 1219d;
        private const double EditMethodId = 1220d;

        private readonly IEmployeeService _employeeService;

        public EmployeePageController(IEmployeeService employeeService, ILogger<EmployeePageController> logger, IStringLocalizer<SharedResource> localizer)
            : base(logger, localizer)
        {
            _employeeService = employeeService;
        }

        [HttpGet("")]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> Index()
        {
            var functionOptions = await GetFilterFunctionOptionsAsync();

            return View(new EmployeePageIndexViewModel
            {
                Filter = new EmployeePageFilterModel
                {
                    Page = 1,
                    PageSize = 16
                },
                FunctionOptions = functionOptions
            });
        }

        [HttpGet("List")]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> List(EmployeePageFilterModel filter)
        {
            var normalizedFilter = filter ?? new EmployeePageFilterModel();
            normalizedFilter.Page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            normalizedFilter.PageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;

            var result = await _employeeService.GetEmployeesAsync(new EmployeeFilterDto
            {
                Code = normalizedFilter.Code,
                Name = normalizedFilter.Name,
                FunctionID = normalizedFilter.FunctionId,
                Status = ResolveStatus(normalizedFilter.IsInvalid),
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize
            });

            var model = new EmployeePageListViewModel
            {
                Items = result.IsSuccess && result.Data != null
                    ? result.Data.Items.Select(MapListItem).ToList()
                    : new List<EmployeePageListItemViewModel>(),
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

            var detailResult = await _employeeService.GetEmployeeDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? L("EmployeeNotFound"));
                return RedirectToAction("List");
            }

            var model = MapDetail(detailResult.Data);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/Employee/List");

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

            var detailResult = await _employeeService.GetEmployeeDetailAsync(resolved.ResolvedId.Value);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? L("EmployeeNotFound"));
                return RedirectToAction("List");
            }

            var model = MapEdit(detailResult.Data);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/Employee/List");

            await EnsureEditorDependenciesAsync(model);
            return View(model);
        }

        [HttpGet("Create")]
        [RequireMethodPermission(AddMethodId)]
        public async Task<IActionResult> Create(string? code = null)
        {
            var model = new EmployeePageCreateViewModel
            {
                Code = NormalizeCode(code)
            };

            await EnsureEditorDependenciesAsync(model);
            return View(model);
        }

        [HttpPost("Save")]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(AddMethodId, write: true)]
        public async Task<IActionResult> Save(EmployeePageCreateViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);
            ValidateEditorModel(model);

            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }

            var result = await _employeeService.CreateEmployeeAsync(MapToUpsert(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? L("EmployeeSaveFailed"));
                return View("Create", model);
            }

            ShowSuccess(result.Message ?? L("EmployeeSavedSuccess"));
            if (model.AutomaticForward)
            {
                return RedirectToAction("Create", new { code = model.Code });
            }

            return RedirectToAction("Index");
        }

        [HttpPost("Update")]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(EditMethodId, write: true)]
        public async Task<IActionResult> Update(EmployeePageEditViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);
            ValidateEditorModel(model);

            if (!ModelState.IsValid)
            {
                return View("Edit", model);
            }

            var result = await _employeeService.UpdateEmployeeAsync(MapToUpsert(model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? L("EmployeeUpdateFailed"));
                return View("Edit", model);
            }

            ShowSuccess(result.Message ?? L("EmployeeUpdatedSuccess"));
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
            var path = Request.Path.HasValue ? Request.Path.Value : "/Employee/List";
            var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            return (path ?? "/Employee/List") + query;
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

        private async Task<List<EmployeePageFunctionFilterOptionViewModel>> GetFilterFunctionOptionsAsync()
        {
            var result = await _employeeService.GetFunctionsAsync();
            if (!result.IsSuccess || result.Data == null)
            {
                return new List<EmployeePageFunctionFilterOptionViewModel>();
            }

            return result.Data
                .Select(x => new EmployeePageFunctionFilterOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToList();
        }

        private async Task EnsureEditorDependenciesAsync(EmployeePageEditorViewModelBase model)
        {
            if (model == null)
            {
                return;
            }

            var functionOptions = await _employeeService.GetEmployeeFunctionOptionsAsync();
            var workTypeOptions = await _employeeService.GetEmployeeWorkTypeOptionsAsync();
            var timeTypeOptions = await _employeeService.GetEmployeeTimeTypeOptionsAsync();

            model.AvailableFunctions = functionOptions.IsSuccess && functionOptions.Data != null
                ? functionOptions.Data.Select(MapLookupOption).OrderBy(x => x.Name).ThenBy(x => x.Id).ToList()
                : new List<EmployeePageLookupOptionViewModel>();

            model.AvailableWorkTypes = workTypeOptions.IsSuccess && workTypeOptions.Data != null
                ? workTypeOptions.Data.Select(MapLookupOption).OrderBy(x => x.Name).ThenBy(x => x.Id).ToList()
                : new List<EmployeePageLookupOptionViewModel>();

            model.AvailableTimeTypes = timeTypeOptions.IsSuccess && timeTypeOptions.Data != null
                ? timeTypeOptions.Data.Select(MapLookupOption).OrderBy(x => x.Name).ThenBy(x => x.Id).ToList()
                : new List<EmployeePageLookupOptionViewModel>();

            model.Functions = model.Functions
                .Where(x => x.FunctionId > 0)
                .GroupBy(x => x.FunctionId)
                .Select(x => new EmployeePageFunctionAssignmentViewModel
                {
                    FunctionId = x.Key,
                    FunctionName = x.FirstOrDefault(y => !string.IsNullOrWhiteSpace(y.FunctionName))?.FunctionName ?? string.Empty,
                    WorkAmountUpdate = x.Any(y => y.WorkAmountUpdate)
                })
                .OrderBy(x => x.FunctionName)
                .ThenBy(x => x.FunctionId)
                .ToList();

            model.WorkTypes = model.WorkTypes
                .Where(x => x.WorkTypeId > 0)
                .GroupBy(x => x.WorkTypeId)
                .Select(x => new EmployeePageWorkTypeAssignmentViewModel
                {
                    WorkTypeId = x.Key,
                    WorkTypeName = x.FirstOrDefault(y => !string.IsNullOrWhiteSpace(y.WorkTypeName))?.WorkTypeName ?? string.Empty,
                    IsDefault = x.Any(y => y.IsDefault)
                })
                .OrderBy(x => x.WorkTypeName)
                .ThenBy(x => x.WorkTypeId)
                .ToList();

            model.TimeTypes = model.TimeTypes
                .Where(x => x.TimeTypeId > 0)
                .GroupBy(x => x.TimeTypeId)
                .Select(x => new EmployeePageTimeTypeAssignmentViewModel
                {
                    TimeTypeId = x.Key,
                    TimeTypeName = x.FirstOrDefault(y => !string.IsNullOrWhiteSpace(y.TimeTypeName))?.TimeTypeName ?? string.Empty,
                    IsDefault = x.Any(y => y.IsDefault)
                })
                .OrderBy(x => x.TimeTypeName)
                .ThenBy(x => x.TimeTypeId)
                .ToList();

            var functionNameMap = model.AvailableFunctions.ToDictionary(x => x.Id, x => x.Name);
            foreach (var assignment in model.Functions)
            {
                if (string.IsNullOrWhiteSpace(assignment.FunctionName) && functionNameMap.TryGetValue(assignment.FunctionId, out var name))
                {
                    assignment.FunctionName = name;
                }
            }

            var workTypeNameMap = model.AvailableWorkTypes.ToDictionary(x => x.Id, x => x.Name);
            foreach (var assignment in model.WorkTypes)
            {
                if (string.IsNullOrWhiteSpace(assignment.WorkTypeName) && workTypeNameMap.TryGetValue(assignment.WorkTypeId, out var name))
                {
                    assignment.WorkTypeName = name;
                }
            }

            var timeTypeNameMap = model.AvailableTimeTypes.ToDictionary(x => x.Id, x => x.Name);
            foreach (var assignment in model.TimeTypes)
            {
                if (string.IsNullOrWhiteSpace(assignment.TimeTypeName) && timeTypeNameMap.TryGetValue(assignment.TimeTypeId, out var name))
                {
                    assignment.TimeTypeName = name;
                }
            }
        }

        private void ValidateEditorModel(EmployeePageEditorViewModelBase model)
        {
            if (string.IsNullOrWhiteSpace(model.Code))
            {
                ModelState.AddModelError(nameof(model.Code), L("CodeRequired"));
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), L("NameRequired"));
            }

            if (model.Code != null && model.Code.Trim().Length > 8)
            {
                ModelState.AddModelError(nameof(model.Code), L("CodeMaxLength8"));
            }

            if (model.Name != null && model.Name.Trim().Length > 32)
            {
                ModelState.AddModelError(nameof(model.Name), L("NameMaxLength32"));
            }
        }

        private static EmployeePageLookupOptionViewModel MapLookupOption(EmployeeLookupOptionDto dto)
        {
            return new EmployeePageLookupOptionViewModel
            {
                Id = dto.Id,
                Name = dto.Name,
                IsInvalid = dto.Invisible
            };
        }

        private static EmployeePageListItemViewModel MapListItem(EmployeeDto dto)
        {
            return new EmployeePageListItemViewModel
            {
                Id = dto.Id,
                Code = dto.Code,
                Name = dto.Name,
                IsInvalid = dto.Invisible
            };
        }

        private static EmployeePageDetailViewModel MapDetail(EmployeeDetailDto dto)
        {
            return new EmployeePageDetailViewModel
            {
                Id = dto.Id,
                Code = dto.Code,
                Name = dto.Name,
                IsInvalid = dto.Invisible,
                Functions = dto.Functions.Select(x => new EmployeePageFunctionAssignmentViewModel
                {
                    FunctionId = x.FunctionId,
                    FunctionName = x.Name,
                    WorkAmountUpdate = x.WorkAmountUpdate
                }).ToList(),
                WorkTypes = dto.WorkTypes.Select(x => new EmployeePageWorkTypeAssignmentViewModel
                {
                    WorkTypeId = x.WorkTypeId,
                    WorkTypeName = x.Name,
                    IsDefault = x.IsDefault
                }).ToList(),
                TimeTypes = dto.TimeTypes.Select(x => new EmployeePageTimeTypeAssignmentViewModel
                {
                    TimeTypeId = x.TimeTypeId,
                    TimeTypeName = x.Name,
                    IsDefault = x.IsDefault
                }).ToList()
            };
        }

        private static EmployeePageEditViewModel MapEdit(EmployeeDetailDto dto)
        {
            return new EmployeePageEditViewModel
            {
                Id = dto.Id,
                Code = dto.Code,
                Name = dto.Name,
                IsInvalid = dto.Invisible,
                Functions = dto.Functions.Select(x => new EmployeePageFunctionAssignmentViewModel
                {
                    FunctionId = x.FunctionId,
                    FunctionName = x.Name,
                    WorkAmountUpdate = x.WorkAmountUpdate
                }).ToList(),
                WorkTypes = dto.WorkTypes.Select(x => new EmployeePageWorkTypeAssignmentViewModel
                {
                    WorkTypeId = x.WorkTypeId,
                    WorkTypeName = x.Name,
                    IsDefault = x.IsDefault
                }).ToList(),
                TimeTypes = dto.TimeTypes.Select(x => new EmployeePageTimeTypeAssignmentViewModel
                {
                    TimeTypeId = x.TimeTypeId,
                    TimeTypeName = x.Name,
                    IsDefault = x.IsDefault
                }).ToList()
            };
        }

        private static EmployeeUpsertDto MapToUpsert(EmployeePageEditorViewModelBase model)
        {
            return new EmployeeUpsertDto
            {
                Id = model.Id,
                Code = NormalizeCode(model.Code),
                Name = NormalizeName(model.Name),
                Invisible = model.IsInvalid,
                Functions = model.Functions
                    .Where(x => x.FunctionId > 0)
                    .Select(x => new EmployeeFunctionAssignmentInputDto
                    {
                        FunctionId = x.FunctionId,
                        WorkAmountUpdate = x.WorkAmountUpdate
                    })
                    .ToList(),
                WorkTypes = model.WorkTypes
                    .Where(x => x.WorkTypeId > 0)
                    .Select(x => new EmployeeWorkTypeAssignmentInputDto
                    {
                        WorkTypeId = x.WorkTypeId,
                        IsDefault = x.IsDefault
                    })
                    .ToList(),
                TimeTypes = model.TimeTypes
                    .Where(x => x.TimeTypeId > 0)
                    .Select(x => new EmployeeTimeTypeAssignmentInputDto
                    {
                        TimeTypeId = x.TimeTypeId,
                        IsDefault = x.IsDefault
                    })
                    .ToList()
            };
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

        private static string NormalizeName(string? name)
        {
            var normalized = (name ?? string.Empty).Trim();
            if (normalized.Length > 32)
            {
                normalized = normalized.Substring(0, 32);
            }

            return normalized;
        }

        private static int ResolveStatus(bool? isInvalid)
        {
            if (!isInvalid.HasValue)
            {
                return 0;
            }

            return isInvalid.Value ? 2 : 1;
        }
    }
}
