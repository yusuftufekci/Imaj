using System.Collections.Generic;

namespace Imaj.Service.DTOs
{
    public class ProdCatFilterDto
    {
        public bool? IsInvalid { get; set; }
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class ProdCatListItemDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string TaxName { get; set; } = string.Empty;
        public short Sequence { get; set; }
        public bool Invisible { get; set; }
    }

    public class ProdCatLanguageDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ProdCatTaxTypeOptionDto
    {
        public decimal Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class ProdCatLocalizedNameDto
    {
        public decimal LanguageId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class ProdCatDetailDto
    {
        public decimal Id { get; set; }
        public decimal CompanyId { get; set; }
        public decimal TaxTypeId { get; set; }
        public string TaxCode { get; set; } = string.Empty;
        public string TaxName { get; set; } = string.Empty;
        public short Sequence { get; set; }
        public bool Invisible { get; set; }
        public List<ProdCatLocalizedNameDto> Names { get; set; } = new();
    }

    public class ProdCatLocalizedNameInputDto
    {
        public decimal LanguageId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ProdCatUpsertDto
    {
        public decimal? Id { get; set; }
        public decimal TaxTypeId { get; set; }
        public short Sequence { get; set; }
        public bool Invisible { get; set; }
        public List<ProdCatLocalizedNameInputDto> Names { get; set; } = new();
    }
}
