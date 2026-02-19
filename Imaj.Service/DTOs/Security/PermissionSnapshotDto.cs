using System;
using System.Collections.Generic;

namespace Imaj.Service.DTOs.Security
{
    public enum CompanyScopeMode
    {
        CompanyBound = 0,
        SystemWide = 1,
        Deny = 2
    }

    public class ContainerPermissionDto
    {
        public decimal BaseContId { get; set; }
        public string BaseContName { get; set; } = string.Empty;
        public bool AllIntf { get; set; }
        public bool AllMethRead { get; set; }
        public bool AllMethWrite { get; set; }
        public bool AllPropRead { get; set; }
        public bool AllPropWrite { get; set; }
        public List<decimal> SourceRoleContIds { get; set; } = new();
        public List<decimal> SourceRoleIds { get; set; } = new();
    }

    public class MethodPermissionDto
    {
        public decimal BaseMethId { get; set; }
        public decimal BaseContId { get; set; }
        public string BaseMethName { get; set; } = string.Empty;
        public bool ReadOnly { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    public class PropertyPermissionDto
    {
        public decimal BasePropId { get; set; }
        public decimal BaseContId { get; set; }
        public string BasePropName { get; set; } = string.Empty;
        public bool ReadOnly { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    public class PermissionSnapshotDto
    {
        public decimal UserId { get; set; }
        public string UserCode { get; set; } = string.Empty;
        public decimal? CompanyId { get; set; }
        public CompanyScopeMode CompanyScopeMode { get; set; }
        public bool AllEmployee { get; set; }
        public bool HasAllMenu { get; set; }
        public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public bool IsDenied { get; set; }
        public string? DenyReason { get; set; }

        public List<decimal> ActiveRoleIds { get; set; } = new();
        public List<string> ActiveRoleNames { get; set; } = new();

        public Dictionary<decimal, string> AllowedPages { get; set; } = new();
        public Dictionary<decimal, ContainerPermissionDto> Containers { get; set; } = new();
        public Dictionary<decimal, MethodPermissionDto> Methods { get; set; } = new();
        public Dictionary<decimal, PropertyPermissionDto> Properties { get; set; } = new();

        public bool EmployeeScopeBypass { get; set; }
        public List<decimal> AllowedEmployeeIds { get; set; } = new();
        public List<decimal> AllowedFunctionIds { get; set; } = new();

        public string LegacyUserMenu { get; set; } = string.Empty;

        public List<PermissionDecisionTraceDto> DecisionTrace { get; set; } = new();

        public bool CanAccessPage(decimal baseIntfId)
        {
            return HasAllMenu || AllowedPages.ContainsKey(baseIntfId);
        }

        public bool CanExecuteMethod(decimal baseMethId, bool write)
        {
            if (!Methods.TryGetValue(baseMethId, out var permission))
            {
                return false;
            }

            return write ? permission.CanWrite : permission.CanRead;
        }

        public bool CanReadProperty(decimal basePropId)
        {
            return Properties.TryGetValue(basePropId, out var permission) && permission.CanRead;
        }

        public bool CanWriteProperty(decimal basePropId)
        {
            return Properties.TryGetValue(basePropId, out var permission) && permission.CanWrite;
        }
    }
}
