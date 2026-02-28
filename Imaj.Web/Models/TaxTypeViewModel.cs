using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Imaj.Web.Models
{
    public class TaxTypeFilterModel
    {
        public string? Code { get; set; }
        public bool? IsInvalid { get; set; }
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class TaxTypeIndexViewModel
    {
        public TaxTypeFilterModel Filter { get; set; } = new();
        public string CreateCode { get; set; } = string.Empty;
    }

    public class TaxTypeListItemViewModel
    {
        public decimal Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public short TaxPercentage { get; set; }
        public bool IsInvalid { get; set; }
    }

    public class TaxTypeListViewModel
    {
        public List<TaxTypeListItemViewModel> Items { get; set; } = new();
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
        public int TotalCount { get; set; }
        public TaxTypeFilterModel Filter { get; set; } = new();
        public string ReturnUrl { get; set; } = "/TaxType/List";
    }

    public class TaxTypeLanguageOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class TaxTypeLocalizedNameViewModel
    {
        [Range(1, 999, ErrorMessage = "Dil secimi zorunludur.")]
        public decimal LanguageId { get; set; }

        public string LanguageName { get; set; } = string.Empty;

        [StringLength(32, ErrorMessage = "Ad en fazla 32 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(32, ErrorMessage = "Fatura son-eki en fazla 32 karakter olabilir.")]
        public string InvoLinePostfix { get; set; } = string.Empty;
    }

    public abstract class TaxTypeEditorViewModelBase
    {
        public decimal? Id { get; set; }

        [Required(ErrorMessage = "Kod zorunludur.")]
        [StringLength(8, ErrorMessage = "Kod en fazla 8 karakter olabilir.")]
        public string Code { get; set; } = string.Empty;

        [Range(0, 100, ErrorMessage = "Vergi orani 0 ile 100 arasinda olmalidir.")]
        public short TaxPercentage { get; set; }

        public bool IsInvalid { get; set; }
        public bool AutomaticForward { get; set; }
        public string ReturnUrl { get; set; } = "/TaxType/List";

        public List<TaxTypeLanguageOptionViewModel> Languages { get; set; } = new();
        public List<TaxTypeLocalizedNameViewModel> Names { get; set; } = new();
    }

    public class TaxTypeDetailViewModel : TaxTypeEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class TaxTypeEditViewModel : TaxTypeEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class TaxTypeCreateViewModel : TaxTypeEditorViewModelBase
    {
    }
}
