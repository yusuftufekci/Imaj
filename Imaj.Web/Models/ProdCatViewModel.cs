using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Imaj.Web.Models
{
    public class ProdCatFilterModel
    {
        public bool? IsInvalid { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class ProdCatIndexViewModel
    {
        public ProdCatFilterModel Filter { get; set; } = new();
    }

    public class ProdCatListItemViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string TaxName { get; set; } = string.Empty;
        public short Sequence { get; set; }
        public bool IsInvalid { get; set; }
    }

    public class ProdCatListViewModel
    {
        public List<ProdCatListItemViewModel> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
        public int TotalCount { get; set; }
        public ProdCatFilterModel Filter { get; set; } = new();
        public string ReturnUrl { get; set; } = "/ProdCat/List";
    }

    public class ProdCatLanguageOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ProdCatTaxTypeOptionViewModel
    {
        public decimal Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class ProdCatLocalizedNameViewModel
    {
        [Range(1, 999, ErrorMessage = "Dil secimi zorunludur.")]
        public decimal LanguageId { get; set; }

        public string LanguageName { get; set; } = string.Empty;

        [StringLength(32, ErrorMessage = "Ad en fazla 32 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;
    }

    public abstract class ProdCatEditorViewModelBase
    {
        public decimal? Id { get; set; }

        [Range(1, 999999, ErrorMessage = "Vergi tipi secimi zorunludur.")]
        public decimal TaxTypeId { get; set; }

        [Range(0, 32767, ErrorMessage = "Sira no 0 ile 32767 arasinda olmalidir.")]
        public short Sequence { get; set; }

        public bool IsInvalid { get; set; }
        public bool AutomaticForward { get; set; }
        public string ReturnUrl { get; set; } = "/ProdCat/List";

        public List<ProdCatLanguageOptionViewModel> Languages { get; set; } = new();
        public List<ProdCatTaxTypeOptionViewModel> TaxTypeOptions { get; set; } = new();
        public List<ProdCatLocalizedNameViewModel> Names { get; set; } = new();
    }

    public class ProdCatDetailViewModel : ProdCatEditorViewModelBase
    {
        public string TaxCode { get; set; } = string.Empty;
        public string TaxName { get; set; } = string.Empty;
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class ProdCatEditViewModel : ProdCatEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class ProdCatCreateViewModel : ProdCatEditorViewModelBase
    {
    }
}
