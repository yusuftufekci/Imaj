using System.Collections.Generic;

namespace Imaj.Service.DTOs
{
    public class EmployeeLookupOptionDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class EmployeeFunctionAssignmentDto
    {
        public decimal FunctionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool WorkAmountUpdate { get; set; }
    }

    public class EmployeeWorkTypeAssignmentDto
    {
        public decimal WorkTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public class EmployeeTimeTypeAssignmentDto
    {
        public decimal TimeTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public class EmployeeDetailDto
    {
        public decimal Id { get; set; }
        public decimal CompanyId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
        public List<EmployeeFunctionAssignmentDto> Functions { get; set; } = new();
        public List<EmployeeWorkTypeAssignmentDto> WorkTypes { get; set; } = new();
        public List<EmployeeTimeTypeAssignmentDto> TimeTypes { get; set; } = new();
    }

    public class EmployeeFunctionAssignmentInputDto
    {
        public decimal FunctionId { get; set; }
        public bool WorkAmountUpdate { get; set; }
    }

    public class EmployeeWorkTypeAssignmentInputDto
    {
        public decimal WorkTypeId { get; set; }
        public bool IsDefault { get; set; }
    }

    public class EmployeeTimeTypeAssignmentInputDto
    {
        public decimal TimeTypeId { get; set; }
        public bool IsDefault { get; set; }
    }

    public class EmployeeUpsertDto
    {
        public decimal? Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Invisible { get; set; }
        public List<EmployeeFunctionAssignmentInputDto> Functions { get; set; } = new();
        public List<EmployeeWorkTypeAssignmentInputDto> WorkTypes { get; set; } = new();
        public List<EmployeeTimeTypeAssignmentInputDto> TimeTypes { get; set; } = new();
    }
}
