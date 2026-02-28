using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Imaj.Web.Models
{
    public class ResoCatFilterModel
    {
        public string? Name { get; set; }
        public bool? IsInvalid { get; set; }
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class ResoCatIndexViewModel
    {
        public ResoCatFilterModel Filter { get; set; } = new();
    }

    public class ResoCatListItemViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsInvalid { get; set; }
    }

    public class ResoCatListViewModel
    {
        public List<ResoCatListItemViewModel> Items { get; set; } = new();
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
        public int TotalCount { get; set; }
        public ResoCatFilterModel Filter { get; set; } = new();
        public string ReturnUrl { get; set; } = "/ResoCat/List";
    }

    public class ResoCatLanguageOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ResoCatLocalizedNameViewModel
    {
        [Range(1, 999, ErrorMessage = "Dil secimi zorunludur.")]
        public decimal LanguageId { get; set; }

        public string LanguageName { get; set; } = string.Empty;

        [StringLength(32, ErrorMessage = "Ad en fazla 32 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;
    }

    public abstract class ResoCatEditorViewModelBase
    {
        public decimal? Id { get; set; }
        public bool IsInvalid { get; set; }
        public bool AutomaticForward { get; set; }
        public string ReturnUrl { get; set; } = "/ResoCat/List";

        public List<ResoCatLanguageOptionViewModel> Languages { get; set; } = new();
        public List<ResoCatLocalizedNameViewModel> Names { get; set; } = new();
    }

    public class ResoCatDetailViewModel : ResoCatEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class ResoCatEditViewModel : ResoCatEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class ResoCatCreateViewModel : ResoCatEditorViewModelBase
    {
    }
}
