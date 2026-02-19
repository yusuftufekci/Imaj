using System.Collections.Generic;

namespace Imaj.Service.DTOs
{
    public class ProdGrpFilterDto
    {
        public bool? IsInvalid { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class ProdGrpListItemDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class ProdGrpLanguageDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ProdGrpLocalizedNameDto
    {
        public decimal LanguageId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class ProdGrpDetailDto
    {
        public decimal Id { get; set; }
        public decimal CompanyId { get; set; }
        public bool Invisible { get; set; }
        public List<ProdGrpLocalizedNameDto> Names { get; set; } = new();
    }

    public class ProdGrpLocalizedNameInputDto
    {
        public decimal LanguageId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ProdGrpUpsertDto
    {
        public decimal? Id { get; set; }
        public bool Invisible { get; set; }
        public List<ProdGrpLocalizedNameInputDto> Names { get; set; } = new();
    }
}
