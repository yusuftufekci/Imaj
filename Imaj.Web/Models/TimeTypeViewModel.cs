using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Imaj.Web.Models
{
    public class TimeTypeFilterModel
    {
        public bool? IsInvalid { get; set; }
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class TimeTypeIndexViewModel
    {
        public TimeTypeFilterModel Filter { get; set; } = new();
    }

    public class TimeTypeListItemViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsInvalid { get; set; }
    }

    public class TimeTypeListViewModel
    {
        public List<TimeTypeListItemViewModel> Items { get; set; } = new();
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
        public int TotalCount { get; set; }
        public TimeTypeFilterModel Filter { get; set; } = new();
        public string ReturnUrl { get; set; } = "/TimeType/List";
    }

    public class TimeTypeLanguageOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class TimeTypeLocalizedNameViewModel
    {
        [Range(1, 999, ErrorMessage = "Dil secimi zorunludur.")]
        public decimal LanguageId { get; set; }

        public string LanguageName { get; set; } = string.Empty;

        [StringLength(32, ErrorMessage = "Ad en fazla 32 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;
    }

    public abstract class TimeTypeEditorViewModelBase
    {
        public decimal? Id { get; set; }

        public bool IsInvalid { get; set; }
        public bool AutomaticForward { get; set; }
        public string ReturnUrl { get; set; } = "/TimeType/List";

        public List<TimeTypeLanguageOptionViewModel> Languages { get; set; } = new();
        public List<TimeTypeLocalizedNameViewModel> Names { get; set; } = new();
    }

    public class TimeTypeDetailViewModel : TimeTypeEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class TimeTypeEditViewModel : TimeTypeEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class TimeTypeCreateViewModel : TimeTypeEditorViewModelBase
    {
    }
}
