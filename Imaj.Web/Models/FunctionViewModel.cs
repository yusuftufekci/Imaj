using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Imaj.Web.Models
{
    public class FunctionFilterModel
    {
        public bool? Reservable { get; set; }
        public decimal? IntervalId { get; set; }
        public bool? IsInvalid { get; set; }
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class FunctionIndexViewModel
    {
        public FunctionFilterModel Filter { get; set; } = new();
        public List<FunctionIntervalOptionViewModel> Intervals { get; set; } = new();

        public bool CreateReservable { get; set; }
        public decimal? CreateIntervalId { get; set; }
    }

    public class FunctionListItemViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Reservable { get; set; }
        public decimal? IntervalId { get; set; }
        public string IntervalName { get; set; } = string.Empty;
        public bool IsInvalid { get; set; }
    }

    public class FunctionListViewModel
    {
        public List<FunctionListItemViewModel> Items { get; set; } = new();
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
        public int TotalCount { get; set; }
        public FunctionFilterModel Filter { get; set; } = new();
        public string ReturnUrl { get; set; } = "/Function/List";
    }

    public class FunctionLanguageOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class FunctionIntervalOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class FunctionLocalizedNameViewModel
    {
        [Range(1, 999, ErrorMessage = "Dil secimi zorunludur.")]
        public decimal LanguageId { get; set; }

        public string LanguageName { get; set; } = string.Empty;

        [StringLength(32, ErrorMessage = "Ad en fazla 32 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;
    }

    public class FunctionProductAssignmentViewModel
    {
        public decimal ProductId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class FunctionRuleResoCatViewModel
    {
        public decimal ResoCatId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class FunctionRuleViewModel
    {
        [StringLength(32, ErrorMessage = "Kural adi en fazla 32 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;

        public short MinValue { get; set; }
        public short MaxValue { get; set; }

        public List<FunctionRuleResoCatViewModel> ResoCats { get; set; } = new();
    }

    public abstract class FunctionEditorViewModelBase
    {
        public decimal? Id { get; set; }

        public bool Reservable { get; set; }

        public bool WorkMandatory { get; set; }

        public bool ProdMandatory { get; set; }

        public bool IsInvalid { get; set; }

        public decimal? IntervalId { get; set; }

        public string IntervalName { get; set; } = string.Empty;

        public bool AutomaticForward { get; set; }

        public string ReturnUrl { get; set; } = "/Function/List";

        public List<FunctionLanguageOptionViewModel> Languages { get; set; } = new();
        public List<FunctionIntervalOptionViewModel> Intervals { get; set; } = new();
        public List<FunctionLocalizedNameViewModel> Names { get; set; } = new();
        public List<FunctionProductAssignmentViewModel> Products { get; set; } = new();
        public List<FunctionRuleViewModel> Rules { get; set; } = new();
    }

    public class FunctionDetailViewModel : FunctionEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class FunctionEditViewModel : FunctionEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class FunctionCreateViewModel : FunctionEditorViewModelBase
    {
        public bool SourceReservable { get; set; }
        public decimal? SourceIntervalId { get; set; }
        public string SourceIntervalName { get; set; } = string.Empty;
    }

    public class FunctionProductLookupFilterModel
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public bool? IsInvalid { get; set; }
        public string? ExcludeIds { get; set; }
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class FunctionResoCatLookupFilterModel
    {
        public string? Name { get; set; }
        public bool? IsInvalid { get; set; }
        public string? ExcludeIds { get; set; }
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
