using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Imaj.Web.Models
{
    public class WorkTypeFilterModel
    {
        public bool? IsInvalid { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class WorkTypeIndexViewModel
    {
        public WorkTypeFilterModel Filter { get; set; } = new();
    }

    public class WorkTypeListItemViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsInvalid { get; set; }
    }

    public class WorkTypeListViewModel
    {
        public List<WorkTypeListItemViewModel> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
        public int TotalCount { get; set; }
        public WorkTypeFilterModel Filter { get; set; } = new();
        public string ReturnUrl { get; set; } = "/WorkType/List";
    }

    public class WorkTypeLanguageOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class WorkTypeLocalizedNameViewModel
    {
        [Range(1, 999, ErrorMessage = "Dil secimi zorunludur.")]
        public decimal LanguageId { get; set; }

        public string LanguageName { get; set; } = string.Empty;

        [StringLength(32, ErrorMessage = "Ad en fazla 32 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;
    }

    public abstract class WorkTypeEditorViewModelBase
    {
        public decimal? Id { get; set; }

        public bool IsInvalid { get; set; }
        public bool AutomaticForward { get; set; }
        public string ReturnUrl { get; set; } = "/WorkType/List";

        public List<WorkTypeLanguageOptionViewModel> Languages { get; set; } = new();
        public List<WorkTypeLocalizedNameViewModel> Names { get; set; } = new();
    }

    public class WorkTypeDetailViewModel : WorkTypeEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class WorkTypeEditViewModel : WorkTypeEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class WorkTypeCreateViewModel : WorkTypeEditorViewModelBase
    {
    }
}
