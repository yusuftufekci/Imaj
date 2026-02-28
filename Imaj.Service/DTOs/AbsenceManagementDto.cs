using System;
using System.Collections.Generic;

namespace Imaj.Service.DTOs
{
    public class AbsenceFilterDto
    {
        public decimal? FunctionId { get; set; }
        public decimal? ReasonId { get; set; }
        public string? Name { get; set; }
        public string? Contact { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public DateTime? EndDateFrom { get; set; }
        public DateTime? EndDateTo { get; set; }
        public decimal? StateId { get; set; }
        public bool? Evaluated { get; set; }
        public List<decimal> ResourceIds { get; set; } = new();
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class AbsenceListItemDto
    {
        public decimal Id { get; set; }
        public decimal FunctionId { get; set; }
        public string FunctionName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal? ReasonId { get; set; }
        public string ReasonName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal StateId { get; set; }
        public string StateName { get; set; } = string.Empty;
        public bool Evaluated { get; set; }
    }

    public class AbsenceFunctionOptionDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class AbsenceReasonOptionDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class AbsenceStateOptionDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class AbsenceResourceItemDto
    {
        public decimal ResourceId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal FunctionId { get; set; }
        public string FunctionName { get; set; } = string.Empty;
        public decimal ResoCatId { get; set; }
        public string ResoCatName { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class AbsenceResourceLookupFilterDto
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public decimal? FunctionId { get; set; }
        public bool? IsInvalid { get; set; }
        public List<decimal> ExcludeIds { get; set; } = new();
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class AbsenceDetailDto
    {
        public decimal Id { get; set; }
        public decimal CompanyId { get; set; }
        public decimal FunctionId { get; set; }
        public string FunctionName { get; set; } = string.Empty;
        public decimal? ReasonId { get; set; }
        public string ReasonName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal StateId { get; set; }
        public string StateName { get; set; } = string.Empty;
        public bool Evaluated { get; set; }
        public string Notes { get; set; } = string.Empty;
        public List<AbsenceResourceItemDto> Resources { get; set; } = new();
    }

    public class AbsenceCreateDto
    {
        public decimal FunctionId { get; set; }
        public decimal ReasonId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool Evaluated { get; set; }
        public string Notes { get; set; } = string.Empty;
        public List<decimal> ResourceIds { get; set; } = new();
    }
}
