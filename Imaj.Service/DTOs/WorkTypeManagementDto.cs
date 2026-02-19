using System.Collections.Generic;

namespace Imaj.Service.DTOs
{
    public class WorkTypeFilterDto
    {
        public bool? IsInvalid { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class WorkTypeListItemDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class WorkTypeLanguageDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class WorkTypeLocalizedNameDto
    {
        public decimal LanguageId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class WorkTypeDetailDto
    {
        public decimal Id { get; set; }
        public decimal CompanyId { get; set; }
        public bool Invisible { get; set; }
        public List<WorkTypeLocalizedNameDto> Names { get; set; } = new();
    }

    public class WorkTypeLocalizedNameInputDto
    {
        public decimal LanguageId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class WorkTypeUpsertDto
    {
        public decimal? Id { get; set; }
        public bool Invisible { get; set; }
        public List<WorkTypeLocalizedNameInputDto> Names { get; set; } = new();
    }
}
