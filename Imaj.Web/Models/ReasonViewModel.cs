using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Imaj.Web.Models
{
    public class ReasonFilterModel
    {
        public string? Code { get; set; }
        public decimal? ReasonCatId { get; set; }
        public bool? IsInvalid { get; set; }
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class ReasonCatOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ReasonIndexViewModel
    {
        public ReasonFilterModel Filter { get; set; } = new();
        public List<ReasonCatOptionViewModel> ReasonCatOptions { get; set; } = new();

        [StringLength(8, ErrorMessage = "Kod en fazla 8 karakter olabilir.")]
        public string CreateCode { get; set; } = string.Empty;

        public decimal? CreateReasonCatId { get; set; }
    }

    public class ReasonListItemViewModel
    {
        public decimal Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ReasonCatName { get; set; } = string.Empty;
        public bool IsInvalid { get; set; }
    }

    public class ReasonListViewModel
    {
        public List<ReasonListItemViewModel> Items { get; set; } = new();
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
        public int TotalCount { get; set; }
        public ReasonFilterModel Filter { get; set; } = new();
        public string ReturnUrl { get; set; } = "/Reason/List";
    }

    public class ReasonLanguageOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ReasonLocalizedNameViewModel
    {
        [Range(1, 999, ErrorMessage = "Dil secimi zorunludur.")]
        public decimal LanguageId { get; set; }

        public string LanguageName { get; set; } = string.Empty;

        [StringLength(32, ErrorMessage = "Ad en fazla 32 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;
    }

    public abstract class ReasonEditorViewModelBase
    {
        public decimal? Id { get; set; }

        public decimal? ReasonCatId { get; set; }
        public string ReasonCatName { get; set; } = string.Empty;

        [StringLength(8, ErrorMessage = "Kod en fazla 8 karakter olabilir.")]
        public string Code { get; set; } = string.Empty;

        public bool IsInvalid { get; set; }
        public string ReturnUrl { get; set; } = "/Reason/List";

        public List<ReasonLanguageOptionViewModel> Languages { get; set; } = new();
        public List<ReasonCatOptionViewModel> ReasonCatOptions { get; set; } = new();
        public List<ReasonLocalizedNameViewModel> Names { get; set; } = new();
    }

    public class ReasonDetailViewModel : ReasonEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class ReasonEditViewModel : ReasonEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class ReasonCreateViewModel : ReasonEditorViewModelBase
    {
    }
}
