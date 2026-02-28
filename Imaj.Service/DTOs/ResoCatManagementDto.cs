using System.Collections.Generic;

namespace Imaj.Service.DTOs
{
    public class ResoCatFilterDto
    {
        public string? Name { get; set; }
        public bool? IsInvalid { get; set; }
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class ResoCatListItemDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class ResoCatLocalizedNameDto
    {
        public decimal LanguageId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class ResoCatDetailDto
    {
        public decimal Id { get; set; }
        public decimal CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public bool Invisible { get; set; }
        public List<ResoCatLocalizedNameDto> Names { get; set; } = new();
    }

    public class ResoCatLanguageDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ResoCatLocalizedNameInputDto
    {
        public decimal LanguageId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ResoCatUpsertDto
    {
        public decimal? Id { get; set; }
        public bool Invisible { get; set; }
        public List<ResoCatLocalizedNameInputDto> Names { get; set; } = new();
    }
}
