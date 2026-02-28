using System.Collections.Generic;

namespace Imaj.Service.DTOs
{
    public class TimeTypeFilterDto
    {
        public bool? IsInvalid { get; set; }
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class TimeTypeListItemDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class TimeTypeLanguageDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class TimeTypeLocalizedNameDto
    {
        public decimal LanguageId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class TimeTypeDetailDto
    {
        public decimal Id { get; set; }
        public decimal CompanyId { get; set; }
        public bool Invisible { get; set; }
        public List<TimeTypeLocalizedNameDto> Names { get; set; } = new();
    }

    public class TimeTypeLocalizedNameInputDto
    {
        public decimal LanguageId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class TimeTypeUpsertDto
    {
        public decimal? Id { get; set; }
        public bool Invisible { get; set; }
        public List<TimeTypeLocalizedNameInputDto> Names { get; set; } = new();
    }
}
