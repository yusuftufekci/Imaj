using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Imaj.Web.Models
{
    public class ResourceFilterModel
    {
        public string? Code { get; set; }
        public int? SequenceFrom { get; set; }
        public int? SequenceTo { get; set; }
        public decimal? FunctionId { get; set; }
        public decimal? ResoCatId { get; set; }
        public bool? IsInvalid { get; set; }
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class ResourceFunctionOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ResourceResoCatOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ResourceIndexViewModel
    {
        public ResourceFilterModel Filter { get; set; } = new();
        public List<ResourceFunctionOptionViewModel> FunctionOptions { get; set; } = new();
        public List<ResourceResoCatOptionViewModel> ResoCatOptions { get; set; } = new();

        public decimal? CreateFunctionId { get; set; }
        public decimal? CreateResoCatId { get; set; }

        [StringLength(8, ErrorMessage = "Kod en fazla 8 karakter olabilir.")]
        public string CreateCode { get; set; } = string.Empty;
    }

    public class ResourceListItemViewModel
    {
        public decimal Id { get; set; }
        public int Sequence { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
        public string ResoCatName { get; set; } = string.Empty;
        public bool IsInvalid { get; set; }
    }

    public class ResourceListViewModel
    {
        public List<ResourceListItemViewModel> Items { get; set; } = new();
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
        public int TotalCount { get; set; }
        public ResourceFilterModel Filter { get; set; } = new();
        public string ReturnUrl { get; set; } = "/Resource/List";
    }

    public class ResourceLanguageOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ResourceLocalizedNameViewModel
    {
        [Range(1, 999, ErrorMessage = "Dil secimi zorunludur.")]
        public decimal LanguageId { get; set; }

        public string LanguageName { get; set; } = string.Empty;

        [StringLength(32, ErrorMessage = "Ad en fazla 32 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;
    }

    public abstract class ResourceEditorViewModelBase
    {
        public decimal? Id { get; set; }
        public decimal? FunctionId { get; set; }
        public string FunctionName { get; set; } = string.Empty;
        public decimal? ResoCatId { get; set; }
        public string ResoCatName { get; set; } = string.Empty;

        [StringLength(8, ErrorMessage = "Kod en fazla 8 karakter olabilir.")]
        public string Code { get; set; } = string.Empty;

        [Range(0, 999999, ErrorMessage = "Sira no en az 0 olabilir.")]
        public int Sequence { get; set; }

        public bool IsInvalid { get; set; }
        public string ReturnUrl { get; set; } = "/Resource/List";

        public List<ResourceLanguageOptionViewModel> Languages { get; set; } = new();
        public List<ResourceFunctionOptionViewModel> FunctionOptions { get; set; } = new();
        public List<ResourceResoCatOptionViewModel> ResoCatOptions { get; set; } = new();
        public List<ResourceLocalizedNameViewModel> Names { get; set; } = new();
    }

    public class ResourceDetailViewModel : ResourceEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class ResourceEditViewModel : ResourceEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class ResourceCreateViewModel : ResourceEditorViewModelBase
    {
    }
}
