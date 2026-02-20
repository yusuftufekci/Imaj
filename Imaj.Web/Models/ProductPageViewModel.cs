using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Imaj.Web.Models
{
    public class ProductPageFilterModel
    {
        public string? Code { get; set; }
        public decimal? ProductCategoryId { get; set; }
        public decimal? ProductGroupId { get; set; }
        public decimal? FunctionId { get; set; }
        public bool? IsInvalid { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class ProductPageIndexViewModel
    {
        public ProductPageFilterModel Filter { get; set; } = new();
        public List<ProductPageCategoryOptionViewModel> ProductCategoryOptions { get; set; } = new();
        public List<ProductPageGroupOptionViewModel> ProductGroupOptions { get; set; } = new();
        public List<ProductPageFunctionOptionViewModel> FunctionOptions { get; set; } = new();

        [StringLength(8, ErrorMessage = "Kod en fazla 8 karakter olabilir.")]
        public string CreateCode { get; set; } = string.Empty;
    }

    public class ProductPageCategoryOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsInvalid { get; set; }
    }

    public class ProductPageGroupOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsInvalid { get; set; }
    }

    public class ProductPageFunctionOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsInvalid { get; set; }
    }

    public class ProductPageListItemViewModel
    {
        public decimal Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ProductCategoryName { get; set; } = string.Empty;
        public string ProductGroupName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsInvalid { get; set; }
    }

    public class ProductPageListViewModel
    {
        public List<ProductPageListItemViewModel> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
        public int TotalCount { get; set; }
        public ProductPageFilterModel Filter { get; set; } = new();
        public string ReturnUrl { get; set; } = "/Product/List";
    }

    public class ProductPageLanguageOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ProductPageLocalizedNameViewModel
    {
        [Range(1, 999, ErrorMessage = "Dil secimi zorunludur.")]
        public decimal LanguageId { get; set; }

        public string LanguageName { get; set; } = string.Empty;

        [StringLength(32, ErrorMessage = "Ad en fazla 32 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;
    }

    public class ProductPageFunctionAssignmentViewModel
    {
        public decimal FunctionId { get; set; }
        public string FunctionName { get; set; } = string.Empty;
        public bool IsInvalid { get; set; }
    }

    public abstract class ProductPageEditorViewModelBase
    {
        public decimal? Id { get; set; }

        [Required(ErrorMessage = "Kod zorunludur.")]
        [StringLength(8, ErrorMessage = "Kod en fazla 8 karakter olabilir.")]
        public string Code { get; set; } = string.Empty;

        [Range(1, double.MaxValue, ErrorMessage = "Urun kategorisi secimi zorunludur.")]
        public decimal ProductCategoryId { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "Urun grubu secimi zorunludur.")]
        public decimal ProductGroupId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Fiyat sifirdan kucuk olamaz.")]
        public decimal Price { get; set; }

        public bool IsInvalid { get; set; }
        public bool AutomaticForward { get; set; }
        public string ReturnUrl { get; set; } = "/Product/List";

        public string ProductCategoryName { get; set; } = string.Empty;
        public string ProductGroupName { get; set; } = string.Empty;

        public List<ProductPageLanguageOptionViewModel> Languages { get; set; } = new();
        public List<ProductPageCategoryOptionViewModel> ProductCategoryOptions { get; set; } = new();
        public List<ProductPageGroupOptionViewModel> ProductGroupOptions { get; set; } = new();
        public List<ProductPageFunctionOptionViewModel> AvailableFunctions { get; set; } = new();

        public List<ProductPageLocalizedNameViewModel> Names { get; set; } = new();
        public List<ProductPageFunctionAssignmentViewModel> Functions { get; set; } = new();
    }

    public class ProductPageDetailViewModel : ProductPageEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class ProductPageEditViewModel : ProductPageEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class ProductPageCreateViewModel : ProductPageEditorViewModelBase
    {
    }

    public class ProductPageFunctionLookupFilterModel
    {
        public string? Name { get; set; }
        public bool? IsInvalid { get; set; }
        public string? ExcludeIds { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
