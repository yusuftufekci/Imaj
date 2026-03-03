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
    public class UserController : BaseController
    {
        private const double BrowseMethodId = 969d;
        private const double ViewMethodId = 970d;
        private const double EditMethodId = 971d;
        private const double AddMethodId = 972d;
        private const double ChangePasswordMethodId = 1039d;

        private readonly IUserService _userService;

        public UserController(IUserService userService, ILogger<UserController> logger, IStringLocalizer<SharedResource> localizer)
            : base(logger, localizer)
        {
            _userService = userService;
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> Index()
        {
            var companyContext = await _userService.GetCompanyContextAsync();
            var contextData = companyContext.IsSuccess && companyContext.Data != null
                ? companyContext.Data
                : new UserCompanyContextDto();

            var model = new UserIndexViewModel
            {
                CompanyId = contextData.CompanyId,
                CompanyName = contextData.CompanyName,
                Filter = new UserFilterModel
                {
                    First = 16,
                    Page = 1,
                    PageSize = 16
                }
            };

            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> List(UserFilterModel filter)
        {
            var normalizedFilter = filter ?? new UserFilterModel();

            normalizedFilter.Page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            normalizedFilter.PageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 16;
            normalizedFilter.First = normalizedFilter.First.HasValue && normalizedFilter.First.Value > 0
                ? normalizedFilter.First.Value
                : 16;

            var serviceFilter = new UserFilterDto
            {
                Code = normalizedFilter.Code,
                Name = normalizedFilter.Name,
                IsInvalid = normalizedFilter.IsInvalid,
                First = normalizedFilter.First,
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize
            };

            var result = await _userService.GetUsersAsync(serviceFilter);

            var items = result.IsSuccess && result.Data != null
                ? result.Data.Items.Select(MapToListItem).ToList()
                : new List<UserListItemViewModel>();

            var model = new UserListViewModel
            {
                Items = items,
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
        public async Task<IActionResult> Detail(string? id, string[]? selectedIds = null, int currentIndex = 0, string? returnUrl = null)
        {
            var resolved = ResolveSelection(id, selectedIds, currentIndex);
            if (string.IsNullOrWhiteSpace(resolved.ResolvedId))
            {
                return RedirectToAction("Index");
            }

            var detailResult = await _userService.GetUserDetailAsync(resolved.ResolvedId);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? L("UserNotFound"));
                return RedirectToAction("List");
            }

            var languageOptions = await GetLanguageOptionsAsync();
            var model = MapToDetailViewModel(detailResult.Data, languageOptions);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/User/List");

            return View(model);
        }

        [AcceptVerbs("GET", "POST")]
        [RequireMethodPermission(EditMethodId)]
        public async Task<IActionResult> Edit(string? id, string[]? selectedIds = null, int currentIndex = 0, string? returnUrl = null)
        {
            var resolved = ResolveSelection(id, selectedIds, currentIndex);
            if (string.IsNullOrWhiteSpace(resolved.ResolvedId))
            {
                return RedirectToAction("Index");
            }

            var detailResult = await _userService.GetUserDetailAsync(resolved.ResolvedId);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                ShowError(detailResult.Message ?? L("UserNotFound"));
                return RedirectToAction("List");
            }

            var languageOptions = await GetLanguageOptionsAsync();
            var model = MapToEditViewModel(detailResult.Data, languageOptions);
            model.SelectedIds = resolved.SelectedIds;
            model.CurrentIndex = resolved.CurrentIndex;
            model.TotalSelected = resolved.SelectedIds.Count;
            model.ReturnUrl = NormalizeReturnUrl(returnUrl, "/User/List");

            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(AddMethodId)]
        public async Task<IActionResult> Create(string? code = null)
        {
            var languageOptions = await GetLanguageOptionsAsync();
            var companyContextResult = await _userService.GetCompanyContextAsync();
            var companyContext = companyContextResult.IsSuccess && companyContextResult.Data != null
                ? companyContextResult.Data
                : new UserCompanyContextDto();

            var trimmedCode = string.IsNullOrWhiteSpace(code)
                ? string.Empty
                : code.Trim().ToUpperInvariant();

            if (trimmedCode.Length > 16)
            {
                trimmedCode = trimmedCode.Substring(0, 16);
            }

            var model = new UserCreateViewModel
            {
                Code = trimmedCode,
                CompanyId = companyContext.CompanyId,
                CompanyName = companyContext.CompanyName,
                LanguageId = languageOptions.Count > 0 ? languageOptions[0].Id : 1,
                Languages = languageOptions
            };

            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(ChangePasswordMethodId)]
        public IActionResult ChangePassword()
        {
            return View(new UserChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(ChangePasswordMethodId, write: true)]
        public async Task<IActionResult> ChangePassword(UserChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _userService.ChangeCurrentUserPasswordAsync(new ChangeCurrentUserPasswordDto
            {
                CurrentPassword = model.CurrentPassword,
                NewPassword = model.NewPassword
            });

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? L("PasswordChangeFailed"));
                return View(model);
            }

            ShowSuccess(result.Message ?? L("PasswordChangedSuccess"));
            return RedirectToAction(nameof(ChangePassword));
        }

        [HttpGet]
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> SearchRoles([FromQuery] RoleLookupFilterModel filter)
        {
            var normalizedFilter = filter ?? new RoleLookupFilterModel();

            var result = await _userService.SearchRolesAsync(new RoleLookupFilterDto
            {
                Name = normalizedFilter.Name,
                IsInvalid = normalizedFilter.IsInvalid,
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize,
                ExcludeIds = ParseDecimalCsv(normalizedFilter.ExcludeIds)
            });

            if (!result.IsSuccess || result.Data == null)
            {
                return BadRequest(result.Message ?? L("RoleListUnavailable"));
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
        public async Task<IActionResult> SearchFunctions([FromQuery] FunctionLookupFilterModel filter)
        {
            var normalizedFilter = filter ?? new FunctionLookupFilterModel();

            var result = await _userService.SearchFunctionsAsync(new FunctionLookupFilterDto
            {
                Name = normalizedFilter.Name,
                IsInvalid = normalizedFilter.IsInvalid,
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize,
                ExcludeIds = ParseDecimalCsv(normalizedFilter.ExcludeIds)
            });

            if (!result.IsSuccess || result.Data == null)
            {
                return BadRequest(result.Message ?? L("FunctionListUnavailable"));
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
        public async Task<IActionResult> Update(UserEditViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);

            if (!ModelState.IsValid)
            {
                return View("Edit", model);
            }

            var input = MapToUpsertDto(model);
            var result = await _userService.UpdateUserAsync(input);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? L("UserUpdateFailed"));
                return View("Edit", model);
            }

            ShowSuccess(result.Message ?? L("UserUpdatedSuccess"));
            return RedirectToAction("Detail", new
            {
                id = model.Code,
                selectedIds = model.SelectedIds,
                currentIndex = model.CurrentIndex,
                returnUrl = model.ReturnUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(AddMethodId, write: true)]
        public async Task<IActionResult> Save(UserCreateViewModel model)
        {
            await EnsureEditorDependenciesAsync(model);

            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }

            var input = MapToUpsertDto(model);
            var result = await _userService.CreateUserAsync(input);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? L("UserSaveFailed"));
                return View("Create", model);
            }

            ShowSuccess(result.Message ?? L("UserSavedSuccess"));
            return RedirectToAction("Index");
        }

        private string BuildCurrentReturnUrl()
        {
            var path = Request.Path.HasValue ? Request.Path.Value : "/User/List";
            var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            return (path ?? "/User/List") + query;
        }

        private static string NormalizeReturnUrl(string? returnUrl, string fallback)
        {
            if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/'))
            {
                return fallback;
            }

            return returnUrl;
        }

        private static (string? ResolvedId, List<string> SelectedIds, int CurrentIndex) ResolveSelection(
            string? id,
            IEnumerable<string>? selectedIds,
            int currentIndex)
        {
            var normalized = selectedIds?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (string.IsNullOrWhiteSpace(id) && normalized.Count > 0)
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
            else if (!string.IsNullOrWhiteSpace(id))
            {
                normalized.Add(id.Trim());
                currentIndex = 0;
            }

            return (id, normalized, currentIndex);
        }

        private async Task<List<UserLanguageOptionViewModel>> GetLanguageOptionsAsync()
        {
            var languagesResult = await _userService.GetLanguagesAsync();
            if (!languagesResult.IsSuccess || languagesResult.Data == null)
            {
                return new List<UserLanguageOptionViewModel>();
            }

            return languagesResult.Data
                .Select(x => new UserLanguageOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }

        private async Task EnsureEditorDependenciesAsync(UserEditorViewModelBase model)
        {
            if (model == null)
            {
                return;
            }

            var languages = await GetLanguageOptionsAsync();
            model.Languages = languages;

            if (model.LanguageId <= 0 && languages.Count > 0)
            {
                model.LanguageId = languages[0].Id;
            }

            if (string.IsNullOrWhiteSpace(model.CompanyName))
            {
                var companyContextResult = await _userService.GetCompanyContextAsync();
                if (companyContextResult.IsSuccess && companyContextResult.Data != null)
                {
                    model.CompanyName = companyContextResult.Data.CompanyName;
                    model.CompanyId ??= companyContextResult.Data.CompanyId;
                }
            }
        }

        private static UserListItemViewModel MapToListItem(UserListItemDto dto)
        {
            return new UserListItemViewModel
            {
                Id = dto.Id,
                Code = dto.Code,
                Name = dto.Name,
                LanguageId = dto.LanguageId,
                LanguageName = dto.LanguageName,
                AllEmployee = dto.AllEmployee,
                IsInvalid = dto.Invisible,
                CompanyId = dto.CompanyId,
                CompanyName = dto.CompanyName
            };
        }

        private static UserDetailViewModel MapToDetailViewModel(UserDetailDto dto, List<UserLanguageOptionViewModel> languages)
        {
            return new UserDetailViewModel
            {
                Id = dto.Id,
                Code = dto.Code,
                Name = dto.Name,
                LanguageId = dto.LanguageId,
                LanguageName = dto.LanguageName,
                CompanyId = dto.CompanyId,
                CompanyName = dto.CompanyName,
                AllEmployee = dto.AllEmployee,
                IsInvalid = dto.Invisible,
                Languages = languages,
                Roles = dto.Roles.Select(MapRole).ToList(),
                Functions = dto.Functions.Select(MapFunction).ToList(),
                Employees = dto.Employees.Select(MapEmployee).ToList()
            };
        }

        private static UserEditViewModel MapToEditViewModel(UserDetailDto dto, List<UserLanguageOptionViewModel> languages)
        {
            return new UserEditViewModel
            {
                Id = dto.Id,
                Code = dto.Code,
                Name = dto.Name,
                LanguageId = dto.LanguageId,
                LanguageName = dto.LanguageName,
                CompanyId = dto.CompanyId,
                CompanyName = dto.CompanyName,
                AllEmployee = dto.AllEmployee,
                IsInvalid = dto.Invisible,
                Languages = languages,
                Roles = dto.Roles.Select(MapRole).ToList(),
                Functions = dto.Functions.Select(MapFunction).ToList(),
                Employees = dto.Employees.Select(MapEmployee).ToList()
            };
        }

        private static UserRoleAssignmentViewModel MapRole(UserRoleAssignmentDto dto)
        {
            return new UserRoleAssignmentViewModel
            {
                RoleId = dto.RoleId,
                Name = dto.Name,
                Invisible = dto.Invisible,
                Global = dto.Global,
                AllMenu = dto.AllMenu
            };
        }

        private static UserFunctionAssignmentViewModel MapFunction(UserFunctionAssignmentDto dto)
        {
            return new UserFunctionAssignmentViewModel
            {
                FunctionId = dto.FunctionId,
                Name = dto.Name,
                Invisible = dto.Invisible
            };
        }

        private static UserEmployeeAssignmentViewModel MapEmployee(UserEmployeeAssignmentDto dto)
        {
            return new UserEmployeeAssignmentViewModel
            {
                EmployeeId = dto.EmployeeId,
                Code = dto.Code,
                Name = dto.Name,
                Invisible = dto.Invisible
            };
        }

        private static UserUpsertDto MapToUpsertDto(UserEditorViewModelBase model)
        {
            return new UserUpsertDto
            {
                Id = model.Id,
                Code = model.Code,
                Name = model.Name,
                Password = model.Password ?? string.Empty,
                LanguageId = model.LanguageId,
                CompanyId = model.CompanyId,
                AllEmployee = model.AllEmployee,
                Invisible = model.IsInvalid,
                RoleIds = model.Roles.Select(x => x.RoleId).Distinct().ToList(),
                FunctionIds = model.Functions.Select(x => x.FunctionId).Distinct().ToList(),
                EmployeeIds = model.Employees.Select(x => x.EmployeeId).Distinct().ToList()
            };
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
