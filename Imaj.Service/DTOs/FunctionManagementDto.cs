using System.Collections.Generic;

namespace Imaj.Service.DTOs
{
    public class FunctionFilterDto
    {
        public bool? Reservable { get; set; }
        public decimal? IntervalId { get; set; }
        public bool? IsInvalid { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class FunctionListItemDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Reservable { get; set; }
        public decimal? IntervalId { get; set; }
        public string IntervalName { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class FunctionLocalizedNameDto
    {
        public decimal LanguageId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class FunctionLanguageDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class FunctionIntervalOptionDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class FunctionProductLookupFilterDto
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public bool? IsInvalid { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public IReadOnlyCollection<decimal> ExcludeIds { get; set; } = new List<decimal>();
    }

    public class FunctionProductLookupItemDto
    {
        public decimal Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class FunctionResoCatLookupFilterDto
    {
        public string? Name { get; set; }
        public bool? IsInvalid { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public IReadOnlyCollection<decimal> ExcludeIds { get; set; } = new List<decimal>();
    }

    public class FunctionResoCatLookupItemDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class FunctionProductAssignmentDto
    {
        public decimal ProductId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class FunctionRuleResoCatDto
    {
        public decimal ResoCatId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class FunctionRuleDto
    {
        public decimal? RuleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public short MinValue { get; set; }
        public short MaxValue { get; set; }
        public List<FunctionRuleResoCatDto> ResoCats { get; set; } = new();
    }

    public class FunctionDetailDto
    {
        public decimal Id { get; set; }
        public decimal CompanyId { get; set; }
        public bool Reservable { get; set; }
        public bool WorkMandatory { get; set; }
        public bool ProdMandatory { get; set; }
        public bool Invisible { get; set; }
        public decimal? IntervalId { get; set; }
        public string IntervalName { get; set; } = string.Empty;

        public List<FunctionLocalizedNameDto> Names { get; set; } = new();
        public List<FunctionProductAssignmentDto> Products { get; set; } = new();
        public List<FunctionRuleDto> Rules { get; set; } = new();
    }

    public class FunctionLocalizedNameInputDto
    {
        public decimal LanguageId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class FunctionRuleInputDto
    {
        public string Name { get; set; } = string.Empty;
        public short MinValue { get; set; }
        public short MaxValue { get; set; }
        public List<decimal> ResoCatIds { get; set; } = new();
    }

    public class FunctionUpsertDto
    {
        public decimal? Id { get; set; }
        public bool Reservable { get; set; }
        public bool WorkMandatory { get; set; }
        public bool ProdMandatory { get; set; }
        public bool Invisible { get; set; }
        public decimal? IntervalId { get; set; }

        public List<FunctionLocalizedNameInputDto> Names { get; set; } = new();
        public List<decimal> ProductIds { get; set; } = new();
        public List<FunctionRuleInputDto> Rules { get; set; } = new();
    }
}
