using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Imaj.Web.Models
{
    public class UserFilterModel
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public bool? IsInvalid { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class UserIndexViewModel
    {
        public UserFilterModel Filter { get; set; } = new();
        public decimal? CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? NewUserCode { get; set; }
    }

    public class UserListItemViewModel
    {
        public decimal Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal LanguageId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public bool AllEmployee { get; set; }
        public bool IsInvalid { get; set; }
        public decimal? CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
    }

    public class UserListViewModel
    {
        public List<UserListItemViewModel> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
        public int TotalCount { get; set; }
        public UserFilterModel Filter { get; set; } = new();
        public string ReturnUrl { get; set; } = "/User/List";
    }

    public class UserLanguageOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class UserRoleAssignmentViewModel
    {
        public decimal RoleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
        public bool Global { get; set; }
        public bool AllMenu { get; set; }
    }

    public class UserFunctionAssignmentViewModel
    {
        public decimal FunctionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class UserEmployeeAssignmentViewModel
    {
        public decimal EmployeeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public abstract class UserEditorViewModelBase
    {
        public decimal? Id { get; set; }

        [Required(ErrorMessage = "Kod zorunludur.")]
        [StringLength(16, ErrorMessage = "Kod en fazla 16 karakter olabilir.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad zorunludur.")]
        [StringLength(48, ErrorMessage = "Ad en fazla 48 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(32, ErrorMessage = "Sifre en fazla 32 karakter olabilir.")]
        public string? Password { get; set; }

        [Range(1, 999, ErrorMessage = "Dil secimi zorunludur.")]
        public decimal LanguageId { get; set; } = 1;

        public string LanguageName { get; set; } = string.Empty;
        public decimal? CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public bool AllEmployee { get; set; }
        public bool IsInvalid { get; set; }

        public List<UserLanguageOptionViewModel> Languages { get; set; } = new();
        public List<UserRoleAssignmentViewModel> Roles { get; set; } = new();
        public List<UserFunctionAssignmentViewModel> Functions { get; set; } = new();
        public List<UserEmployeeAssignmentViewModel> Employees { get; set; } = new();
    }

    public class UserDetailViewModel : UserEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
        public string ReturnUrl { get; set; } = "/User/List";
    }

    public class UserEditViewModel : UserEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
        public string ReturnUrl { get; set; } = "/User/List";
        public bool AutomaticForward { get; set; }
    }

    public class UserCreateViewModel : UserEditorViewModelBase
    {
        public bool AutomaticForward { get; set; }
    }

    public class RoleLookupFilterModel
    {
        public string? Name { get; set; }
        public bool? IsInvalid { get; set; }
        public string? ExcludeIds { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class FunctionLookupFilterModel
    {
        public string? Name { get; set; }
        public bool? IsInvalid { get; set; }
        public string? ExcludeIds { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
