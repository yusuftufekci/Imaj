using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Imaj.Web.Models
{
    public class ProdGrpFilterModel
    {
        public bool? IsInvalid { get; set; }
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class ProdGrpIndexViewModel
    {
        public ProdGrpFilterModel Filter { get; set; } = new();
    }

    public class ProdGrpListItemViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsInvalid { get; set; }
    }

    public class ProdGrpListViewModel
    {
        public List<ProdGrpListItemViewModel> Items { get; set; } = new();
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
        public int TotalCount { get; set; }
        public ProdGrpFilterModel Filter { get; set; } = new();
        public string ReturnUrl { get; set; } = "/ProdGrp/List";
    }

    public class ProdGrpLanguageOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ProdGrpLocalizedNameViewModel
    {
        [Range(1, 999, ErrorMessage = "Dil secimi zorunludur.")]
        public decimal LanguageId { get; set; }

        public string LanguageName { get; set; } = string.Empty;

        [StringLength(32, ErrorMessage = "Ad en fazla 32 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;
    }

    public abstract class ProdGrpEditorViewModelBase
    {
        public decimal? Id { get; set; }
        public bool IsInvalid { get; set; }
        public bool AutomaticForward { get; set; }
        public string ReturnUrl { get; set; } = "/ProdGrp/List";

        public List<ProdGrpLanguageOptionViewModel> Languages { get; set; } = new();
        public List<ProdGrpLocalizedNameViewModel> Names { get; set; } = new();
    }

    public class ProdGrpDetailViewModel : ProdGrpEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class ProdGrpEditViewModel : ProdGrpEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class ProdGrpCreateViewModel : ProdGrpEditorViewModelBase
    {
    }
}
