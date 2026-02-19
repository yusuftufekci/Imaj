using System.Collections.Generic;

namespace Imaj.Service.DTOs
{
    public class ReasonFilterDto
    {
        public string? Code { get; set; }
        public decimal? ReasonCatId { get; set; }
        public bool? IsInvalid { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class ReasonListItemDto
    {
        public decimal Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal ReasonCatId { get; set; }
        public string ReasonCatName { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class ReasonLocalizedNameDto
    {
        public decimal LanguageId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class ReasonLanguageDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ReasonCatOptionDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ReasonDetailDto
    {
        public decimal Id { get; set; }
        public decimal CompanyId { get; set; }
        public decimal ReasonCatId { get; set; }
        public string ReasonCatName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool Invisible { get; set; }
        public List<ReasonLocalizedNameDto> Names { get; set; } = new();
    }

    public class ReasonLocalizedNameInputDto
    {
        public decimal LanguageId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ReasonUpsertDto
    {
        public decimal? Id { get; set; }
        public decimal ReasonCatId { get; set; }
        public string Code { get; set; } = string.Empty;
        public bool Invisible { get; set; }
        public List<ReasonLocalizedNameInputDto> Names { get; set; } = new();
    }
}
