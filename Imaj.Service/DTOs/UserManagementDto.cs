using System.Collections.Generic;

namespace Imaj.Service.DTOs
{
    public class UserFilterDto
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public bool? IsInvalid { get; set; }
        public int? First { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class UserListItemDto
    {
        public decimal Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal LanguageId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public bool AllEmployee { get; set; }
        public bool Invisible { get; set; }
        public decimal? CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
    }

    public class UserRoleAssignmentDto
    {
        public decimal RoleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
        public bool Global { get; set; }
        public bool AllMenu { get; set; }
    }

    public class UserFunctionAssignmentDto
    {
        public decimal FunctionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class UserEmployeeAssignmentDto
    {
        public decimal EmployeeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class UserDetailDto
    {
        public decimal Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal LanguageId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public decimal? CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public bool AllEmployee { get; set; }
        public bool Invisible { get; set; }

        public List<UserRoleAssignmentDto> Roles { get; set; } = new();
        public List<UserFunctionAssignmentDto> Functions { get; set; } = new();
        public List<UserEmployeeAssignmentDto> Employees { get; set; } = new();
    }

    public class UserLanguageDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class UserCompanyContextDto
    {
        public decimal? CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
    }

    public class RoleLookupFilterDto
    {
        public string? Name { get; set; }
        public bool? IsInvalid { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
        public IReadOnlyCollection<decimal> ExcludeIds { get; set; } = new List<decimal>();
    }

    public class RoleLookupItemDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
        public bool Global { get; set; }
        public bool AllMenu { get; set; }
    }

    public class FunctionLookupFilterDto
    {
        public string? Name { get; set; }
        public bool? IsInvalid { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
        public IReadOnlyCollection<decimal> ExcludeIds { get; set; } = new List<decimal>();
    }

    public class FunctionLookupItemDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class UserUpsertDto
    {
        public decimal? Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public decimal LanguageId { get; set; }
        public decimal? CompanyId { get; set; }
        public bool AllEmployee { get; set; }
        public bool Invisible { get; set; }
        public List<decimal> RoleIds { get; set; } = new();
        public List<decimal> FunctionIds { get; set; } = new();
        public List<decimal> EmployeeIds { get; set; } = new();
    }
}
