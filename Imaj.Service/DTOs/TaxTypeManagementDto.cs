using System.Collections.Generic;

namespace Imaj.Service.DTOs
{
    public class TaxTypeFilterDto
    {
        public string? Code { get; set; }
        public bool? IsInvalid { get; set; }
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class TaxTypeListItemDto
    {
        public decimal Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public short TaxPercentage { get; set; }
        public bool Invisible { get; set; }
    }

    public class TaxTypeLanguageDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class TaxTypeLocalizedNameDto
    {
        public decimal LanguageId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string InvoLinePostfix { get; set; } = string.Empty;
    }

    public class TaxTypeDetailDto
    {
        public decimal Id { get; set; }
        public decimal CompanyId { get; set; }
        public string Code { get; set; } = string.Empty;
        public short TaxPercentage { get; set; }
        public bool Invisible { get; set; }
        public List<TaxTypeLocalizedNameDto> Names { get; set; } = new();
    }

    public class TaxTypeLocalizedNameInputDto
    {
        public decimal LanguageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string InvoLinePostfix { get; set; } = string.Empty;
    }

    public class TaxTypeUpsertDto
    {
        public decimal? Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public short TaxPercentage { get; set; }
        public bool Invisible { get; set; }
        public List<TaxTypeLocalizedNameInputDto> Names { get; set; } = new();
    }
}
