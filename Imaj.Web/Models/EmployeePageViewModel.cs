using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Imaj.Web.Models
{
    public class EmployeePageFilterModel
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public decimal? FunctionId { get; set; }
        public bool? IsInvalid { get; set; }
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class EmployeePageFunctionFilterOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class EmployeePageIndexViewModel
    {
        public EmployeePageFilterModel Filter { get; set; } = new();
        public List<EmployeePageFunctionFilterOptionViewModel> FunctionOptions { get; set; } = new();
        public string CreateCode { get; set; } = string.Empty;
    }

    public class EmployeePageListItemViewModel
    {
        public decimal Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsInvalid { get; set; }
    }

    public class EmployeePageListViewModel
    {
        public List<EmployeePageListItemViewModel> Items { get; set; } = new();
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
        public int TotalCount { get; set; }
        public EmployeePageFilterModel Filter { get; set; } = new();
        public string ReturnUrl { get; set; } = "/Employee/List";
    }

    public class EmployeePageLookupOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsInvalid { get; set; }
    }

    public class EmployeePageFunctionAssignmentViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Fonksiyon secimi zorunludur.")]
        public decimal FunctionId { get; set; }
        public string FunctionName { get; set; } = string.Empty;
        public bool WorkAmountUpdate { get; set; }
    }

    public class EmployeePageWorkTypeAssignmentViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Gorev tipi secimi zorunludur.")]
        public decimal WorkTypeId { get; set; }
        public string WorkTypeName { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public class EmployeePageTimeTypeAssignmentViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Mesai tipi secimi zorunludur.")]
        public decimal TimeTypeId { get; set; }
        public string TimeTypeName { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public abstract class EmployeePageEditorViewModelBase
    {
        public decimal? Id { get; set; }

        [Required(ErrorMessage = "Kod zorunludur.")]
        [StringLength(8, ErrorMessage = "Kod en fazla 8 karakter olabilir.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad zorunludur.")]
        [StringLength(32, ErrorMessage = "Ad en fazla 32 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;

        public bool IsInvalid { get; set; }
        public bool AutomaticForward { get; set; }
        public string ReturnUrl { get; set; } = "/Employee/List";

        public List<EmployeePageLookupOptionViewModel> AvailableFunctions { get; set; } = new();
        public List<EmployeePageLookupOptionViewModel> AvailableWorkTypes { get; set; } = new();
        public List<EmployeePageLookupOptionViewModel> AvailableTimeTypes { get; set; } = new();

        public List<EmployeePageFunctionAssignmentViewModel> Functions { get; set; } = new();
        public List<EmployeePageWorkTypeAssignmentViewModel> WorkTypes { get; set; } = new();
        public List<EmployeePageTimeTypeAssignmentViewModel> TimeTypes { get; set; } = new();
    }

    public class EmployeePageDetailViewModel : EmployeePageEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class EmployeePageEditViewModel : EmployeePageEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class EmployeePageCreateViewModel : EmployeePageEditorViewModelBase
    {
    }
}
