using System.Collections.Generic;

namespace Imaj.Service.DTOs
{
    public class ProductPageFilterDto
    {
        public string? Code { get; set; }
        public decimal? ProductCategoryId { get; set; }
        public decimal? ProductGroupId { get; set; }
        public decimal? FunctionId { get; set; }
        public bool? IsInvalid { get; set; }
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class ProductPageListItemDto
    {
        public decimal Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal ProductCategoryId { get; set; }
        public string ProductCategoryName { get; set; } = string.Empty;
        public decimal ProductGroupId { get; set; }
        public string ProductGroupName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool Invisible { get; set; }
    }

    public class ProductPageLanguageDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ProductPageLocalizedNameDto
    {
        public decimal LanguageId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class ProductPageLocalizedNameInputDto
    {
        public decimal LanguageId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ProductPageCategoryOptionDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class ProductPageGroupOptionDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class ProductPageFunctionOptionDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class ProductPageFunctionLookupFilterDto
    {
        public string? Name { get; set; }
        public bool? IsInvalid { get; set; }
        public List<decimal> ExcludeIds { get; set; } = new();
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ProductPageDetailDto
    {
        public decimal Id { get; set; }
        public decimal CompanyId { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal ProductCategoryId { get; set; }
        public string ProductCategoryName { get; set; } = string.Empty;
        public decimal ProductGroupId { get; set; }
        public string ProductGroupName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool Invisible { get; set; }
        public List<ProductPageLocalizedNameDto> Names { get; set; } = new();
        public List<ProductPageFunctionOptionDto> Functions { get; set; } = new();
    }

    public class ProductPageUpsertDto
    {
        public decimal? Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal ProductCategoryId { get; set; }
        public decimal ProductGroupId { get; set; }
        public decimal Price { get; set; }
        public bool Invisible { get; set; }
        public List<ProductPageLocalizedNameInputDto> Names { get; set; } = new();
        public List<decimal> FunctionIds { get; set; } = new();
    }
}
