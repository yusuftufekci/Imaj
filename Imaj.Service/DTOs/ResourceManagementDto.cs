using System.Collections.Generic;

namespace Imaj.Service.DTOs
{
    public class ResourceFilterDto
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

    public class ResourceListItemDto
    {
        public decimal Id { get; set; }
        public int Sequence { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal FunctionId { get; set; }
        public string FunctionName { get; set; } = string.Empty;
        public decimal ResoCatId { get; set; }
        public string ResoCatName { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class ResourceLocalizedNameDto
    {
        public decimal LanguageId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class ResourceFunctionOptionDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ResourceResoCatOptionDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ResourceLanguageDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ResourceDetailDto
    {
        public decimal Id { get; set; }
        public decimal CompanyId { get; set; }
        public decimal FunctionId { get; set; }
        public string FunctionName { get; set; } = string.Empty;
        public decimal ResoCatId { get; set; }
        public string ResoCatName { get; set; } = string.Empty;
        public int Sequence { get; set; }
        public string Code { get; set; } = string.Empty;
        public bool Invisible { get; set; }
        public List<ResourceLocalizedNameDto> Names { get; set; } = new();
    }

    public class ResourceLocalizedNameInputDto
    {
        public decimal LanguageId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ResourceUpsertDto
    {
        public decimal? Id { get; set; }
        public decimal FunctionId { get; set; }
        public decimal ResoCatId { get; set; }
        public int Sequence { get; set; }
        public string Code { get; set; } = string.Empty;
        public bool Invisible { get; set; }
        public List<ResourceLocalizedNameInputDto> Names { get; set; } = new();
    }
}
