using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Service.DTOs.Security;
using Imaj.Service.Interfaces;
using Imaj.Service.Options;
using Imaj.Service.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Imaj.Service.Services
{
    public class PermissionService : IPermissionService
    {
        private const int LegacyTimeoutMinutes = 45;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<PermissionService> _logger;
        private readonly TimeSpan _cacheTtl;

        public PermissionService(
            IUnitOfWork unitOfWork,
            IMemoryCache memoryCache,
            IOptions<AuthSettings> authSettings,
            ILogger<PermissionService> logger)
        {
            _unitOfWork = unitOfWork;
            _memoryCache = memoryCache;
            _logger = logger;

            var timeout = authSettings.Value.SessionTimeoutMinutes;
            if (timeout <= 0)
            {
                timeout = LegacyTimeoutMinutes;
            }

            _cacheTtl = TimeSpan.FromMinutes(timeout);
        }

        public async Task<ServiceResult<PermissionSnapshotDto>> GetOrBuildPermissionSetAsync(decimal userId, bool forceRefresh = false)
        {
            var cacheKey = BuildCacheKey(userId);
            if (forceRefresh)
            {
                _memoryCache.Remove(cacheKey);
            }

            if (!forceRefresh && _memoryCache.TryGetValue(cacheKey, out PermissionSnapshotDto? cachedSnapshot) && cachedSnapshot != null)
            {
                return ServiceResult<PermissionSnapshotDto>.Success(cachedSnapshot);
            }

            var builtResult = await BuildPermissionSetInternalAsync(userId);
            if (!builtResult.IsSuccess || builtResult.Data == null)
            {
                return builtResult;
            }

            _memoryCache.Set(cacheKey, builtResult.Data, _cacheTtl);
            return ServiceResult<PermissionSnapshotDto>.Success(builtResult.Data);
        }

        public Task InvalidateAsync(decimal userId)
        {
            _memoryCache.Remove(BuildCacheKey(userId));
            return Task.CompletedTask;
        }

        private async Task<ServiceResult<PermissionSnapshotDto>> BuildPermissionSetInternalAsync(decimal userId)
        {
            try
            {
                var user = await _unitOfWork.Repository<User>()
                    .Query()
                    .Where(x => x.Id == userId)
                    .Select(x => new
                    {
                        x.Id,
                        x.Code,
                        x.CompanyID,
                        x.AllEmployee,
                        x.Invisible
                    })
                    .SingleOrDefaultAsync();

                if (user == null)
                {
                    return ServiceResult<PermissionSnapshotDto>.Fail("Permission snapshot oluşturulamadı: kullanıcı bulunamadı.");
                }

                var snapshot = new PermissionSnapshotDto
                {
                    UserId = user.Id,
                    UserCode = user.Code,
                    CompanyId = user.CompanyID,
                    AllEmployee = user.AllEmployee,
                    GeneratedAtUtc = DateTimeOffset.UtcNow
                };

                AddTrace(snapshot, "USER_LOADED", "ALLOW", $"UserID={user.Id}, Code={user.Code}, CompanyID={FormatNullableDecimal(user.CompanyID)}");

                if (user.Invisible)
                {
                    snapshot.IsDenied = true;
                    snapshot.DenyReason = "Invisible kullanıcı login olamaz.";
                    snapshot.CompanyScopeMode = CompanyScopeMode.Deny;
                    AddTrace(snapshot, "USER_INVISIBLE", "DENY", "User.Invisible = 1");
                    return ServiceResult<PermissionSnapshotDto>.Success(snapshot);
                }

                var activeRoles = await (
                    from userRole in _unitOfWork.Repository<UserRole>().Query()
                    join role in _unitOfWork.Repository<Role>().Query() on userRole.RoleID equals role.Id
                    where userRole.UserID == userId && userRole.Deleted == 0
                    select new ActiveRoleRow
                    {
                        RoleId = role.Id,
                        RoleName = role.Name,
                        AllMenu = role.AllMenu,
                        AllMethRead = role.AllMethRead,
                        AllMethWrite = role.AllMethWrite,
                        AllPropRead = role.AllPropRead,
                        AllPropWrite = role.AllPropWrite,
                        Global = role.Global
                    })
                    .ToListAsync();

                if (activeRoles.Count == 0)
                {
                    snapshot.IsDenied = true;
                    snapshot.DenyReason = "Aktif rol kaydı yok.";
                    snapshot.CompanyScopeMode = CompanyScopeMode.Deny;
                    AddTrace(snapshot, "ACTIVE_ROLE_CHECK", "DENY", "UserRole(Deleted=0) sonucu boş.");
                    return ServiceResult<PermissionSnapshotDto>.Success(snapshot);
                }

                snapshot.ActiveRoleIds = activeRoles.Select(x => x.RoleId).Distinct().OrderBy(x => x).ToList();
                snapshot.ActiveRoleNames = activeRoles.Select(x => x.RoleName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

                AddTrace(
                    snapshot,
                    "ACTIVE_ROLE_CHECK",
                    "ALLOW",
                    $"RoleCount={snapshot.ActiveRoleIds.Count}, Roles={string.Join(',', snapshot.ActiveRoleNames)}");

                var hasSystemRole = activeRoles.Any(x => !x.Global);
                if (user.CompanyID.HasValue)
                {
                    snapshot.CompanyScopeMode = CompanyScopeMode.CompanyBound;
                    AddTrace(snapshot, "COMPANY_SCOPE", "ALLOW", $"CompanyBound: CompanyID={user.CompanyID.Value}");
                }
                else if (hasSystemRole)
                {
                    snapshot.CompanyScopeMode = CompanyScopeMode.SystemWide;
                    AddTrace(snapshot, "COMPANY_SCOPE", "ALLOW", "CompanyID NULL + system role(Global=0) => SystemWide.");
                }
                else
                {
                    snapshot.CompanyScopeMode = CompanyScopeMode.Deny;
                    snapshot.IsDenied = true;
                    snapshot.DenyReason = "CompanyID NULL kullanıcı, system rol olmadan yetkilendirilemez.";
                    AddTrace(snapshot, "COMPANY_SCOPE", "DENY", "CompanyID NULL ve system role yok.");
                    return ServiceResult<PermissionSnapshotDto>.Success(snapshot);
                }

                snapshot.HasAllMenu = activeRoles.Any(x => x.AllMenu);
                AddTrace(snapshot, "ROLE_GLOBAL_MENU_FLAG", snapshot.HasAllMenu ? "ALLOW" : "INFO", $"HasAllMenu={snapshot.HasAllMenu}");

                var roleIds = snapshot.ActiveRoleIds;

                var roleContRows = await _unitOfWork.Repository<RoleCont>()
                    .Query()
                    .Where(x => roleIds.Contains(x.RoleID) && x.Deleted == 0)
                    .Select(x => new RoleContRow
                    {
                        RoleContId = x.Id,
                        RoleId = x.RoleID,
                        BaseContId = x.BaseContID,
                        AllIntf = x.AllIntf,
                        AllMethRead = x.AllMethRead,
                        AllMethWrite = x.AllMethWrite,
                        AllPropRead = x.AllPropRead,
                        AllPropWrite = x.AllPropWrite
                    })
                    .ToListAsync();

                var baseContIds = roleContRows.Select(x => x.BaseContId).Distinct().ToList();
                var baseContNames = await _unitOfWork.Repository<BaseCont>()
                    .Query()
                    .Where(x => baseContIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.Name);

                foreach (var grouped in roleContRows.GroupBy(x => x.BaseContId))
                {
                    var containerPermission = new ContainerPermissionDto
                    {
                        BaseContId = grouped.Key,
                        BaseContName = baseContNames.TryGetValue(grouped.Key, out var baseContName)
                            ? baseContName
                            : grouped.Key.ToString(CultureInfo.InvariantCulture),
                        AllIntf = grouped.Any(x => x.AllIntf),
                        AllMethRead = grouped.Any(x => x.AllMethRead),
                        AllMethWrite = grouped.Any(x => x.AllMethWrite),
                        AllPropRead = grouped.Any(x => x.AllPropRead),
                        AllPropWrite = grouped.Any(x => x.AllPropWrite),
                        SourceRoleContIds = grouped.Select(x => x.RoleContId).Distinct().OrderBy(x => x).ToList(),
                        SourceRoleIds = grouped.Select(x => x.RoleId).Distinct().OrderBy(x => x).ToList()
                    };

                    snapshot.Containers[grouped.Key] = containerPermission;
                }

                AddTrace(snapshot, "ROLECONT_MERGE", "ALLOW", $"MergedContainerCount={snapshot.Containers.Count}");

                var baseIntfRepository = _unitOfWork.Repository<BaseIntf>();

                if (snapshot.HasAllMenu)
                {
                    snapshot.AllowedPages = await baseIntfRepository
                        .Query()
                        .ToDictionaryAsync(x => x.Id, x => x.Name);

                    AddTrace(snapshot, "PAGE_ACCESS", "ALLOW", $"AllMenu aktif, BaseIntfCount={snapshot.AllowedPages.Count}");
                }
                else
                {
                    var roleMenuPageIds = await _unitOfWork.Repository<RoleMenu>()
                        .Query()
                        .Where(x => roleIds.Contains(x.RoleID) && x.Deleted == 0)
                        .Select(x => x.BaseIntfID)
                        .Distinct()
                        .ToListAsync();

                    var roleMenuContainerIds = roleMenuPageIds.Count == 0
                        ? new HashSet<decimal>()
                        : (await baseIntfRepository
                            .Query()
                            .Where(x => roleMenuPageIds.Contains(x.Id))
                            .Select(x => x.BaseContID)
                            .Distinct()
                            .ToListAsync())
                        .ToHashSet();

                    var allIntfContainerIds = snapshot.Containers.Values
                        .Where(x => x.AllIntf && roleMenuContainerIds.Contains(x.BaseContId))
                        .Select(x => x.BaseContId)
                        .Distinct()
                        .ToList();

                    var allIntfPageIds = allIntfContainerIds.Count == 0
                        ? new List<decimal>()
                        : await baseIntfRepository
                            .Query()
                            .Where(x => allIntfContainerIds.Contains(x.BaseContID))
                            .Select(x => x.Id)
                            .ToListAsync();

                    var roleContIds = roleContRows.Select(x => x.RoleContId).Distinct().ToList();
                    var roleIntfPageIds = roleContIds.Count == 0
                        ? new List<decimal>()
                        : await _unitOfWork.Repository<RoleIntf>()
                            .Query()
                            .Where(x => roleContIds.Contains(x.RoleContID) && x.Deleted == 0)
                            .Select(x => x.BaseIntfID)
                            .Distinct()
                            .ToListAsync();

                    var allowedPageIds = roleMenuPageIds
                        .Concat(allIntfPageIds)
                        .Concat(roleIntfPageIds)
                        .Distinct()
                        .ToList();

                    snapshot.AllowedPages = allowedPageIds.Count == 0
                        ? new Dictionary<decimal, string>()
                        : await baseIntfRepository
                            .Query()
                            .Where(x => allowedPageIds.Contains(x.Id))
                            .ToDictionaryAsync(x => x.Id, x => x.Name);

                    AddTrace(
                        snapshot,
                        "PAGE_ACCESS",
                        snapshot.AllowedPages.Count > 0 ? "ALLOW" : "DENY",
                        $"RoleMenu={roleMenuPageIds.Count}, RoleMenuContainers={roleMenuContainerIds.Count}, AllIntfDerived={allIntfPageIds.Count}, RoleIntfExplicit={roleIntfPageIds.Count}, Final={snapshot.AllowedPages.Count}");
                }

                var orderedLegacyMenu = snapshot.AllowedPages.Values
                    .Select(x => x.ToUpperInvariant())
                    .OrderBy(x => x)
                    .ToList();

                snapshot.LegacyUserMenu = orderedLegacyMenu.Count == 0
                    ? ","
                    : $",{string.Join(',', orderedLegacyMenu)},";

                var methodContainerIds = snapshot.Containers.Keys.ToList();
                var roleContIdsForExplicit = roleContRows.Select(x => x.RoleContId).Distinct().ToList();
                var anyGlobalMethRead = activeRoles.Any(x => x.AllMethRead);
                var anyGlobalMethWrite = activeRoles.Any(x => x.AllMethWrite);
                var anyGlobalPropRead = activeRoles.Any(x => x.AllPropRead);
                var anyGlobalPropWrite = activeRoles.Any(x => x.AllPropWrite);

                var baseMethQuery = _unitOfWork.Repository<BaseMeth>().Query();
                if (methodContainerIds.Count > 0)
                {
                    baseMethQuery = baseMethQuery.Where(x => methodContainerIds.Contains(x.BaseContID));
                }
                else if (!anyGlobalMethRead && !anyGlobalMethWrite)
                {
                    baseMethQuery = baseMethQuery.Where(_ => false);
                }

                var baseMethods = await baseMethQuery
                    .Select(x => new BaseMethodRow
                    {
                        BaseMethId = x.Id,
                        BaseContId = x.BaseContID,
                        Name = x.Name,
                        ReadOnly = x.ReadOnly
                    })
                    .ToListAsync();

                var explicitMethodIds = roleContIdsForExplicit.Count == 0
                    ? new HashSet<decimal>()
                    : (await _unitOfWork.Repository<RoleMeth>()
                        .Query()
                        .Where(x => roleContIdsForExplicit.Contains(x.RoleContID) && x.Deleted == 0)
                        .Select(x => x.BaseMethID)
                        .Distinct()
                        .ToListAsync())
                    .ToHashSet();

                foreach (var method in baseMethods)
                {
                    snapshot.Containers.TryGetValue(method.BaseContId, out var containerPermission);
                    var explicitAllow = explicitMethodIds.Contains(method.BaseMethId);

                    var canRead = anyGlobalMethRead || (containerPermission?.AllMethRead ?? false) || explicitAllow;
                    var canWrite = (anyGlobalMethWrite || (containerPermission?.AllMethWrite ?? false) || explicitAllow) && !method.ReadOnly;

                    if (!canRead && !canWrite)
                    {
                        continue;
                    }

                    var sourceParts = new List<string>();
                    if (anyGlobalMethRead || anyGlobalMethWrite)
                    {
                        sourceParts.Add("GLOBAL");
                    }

                    if ((containerPermission?.AllMethRead ?? false) || (containerPermission?.AllMethWrite ?? false))
                    {
                        sourceParts.Add($"CONTAINER:{method.BaseContId}");
                    }

                    if (explicitAllow)
                    {
                        sourceParts.Add("EXPLICIT:RoleMeth");
                    }

                    if (method.ReadOnly)
                    {
                        sourceParts.Add("READONLY");
                    }

                    snapshot.Methods[method.BaseMethId] = new MethodPermissionDto
                    {
                        BaseMethId = method.BaseMethId,
                        BaseContId = method.BaseContId,
                        BaseMethName = method.Name,
                        ReadOnly = method.ReadOnly,
                        CanRead = canRead,
                        CanWrite = canWrite,
                        Source = string.Join('|', sourceParts)
                    };
                }

                AddTrace(snapshot, "METHOD_ACCESS", "ALLOW", $"MethodPermissionCount={snapshot.Methods.Count}, ExplicitRoleMeth={explicitMethodIds.Count}");

                var basePropQuery = _unitOfWork.Repository<BaseProp>().Query();
                if (methodContainerIds.Count > 0)
                {
                    basePropQuery = basePropQuery.Where(x => methodContainerIds.Contains(x.BaseContID));
                }
                else if (!anyGlobalPropRead && !anyGlobalPropWrite)
                {
                    basePropQuery = basePropQuery.Where(_ => false);
                }

                var baseProps = await basePropQuery
                    .Select(x => new BasePropertyRow
                    {
                        BasePropId = x.Id,
                        BaseContId = x.BaseContID,
                        Name = x.Name,
                        ReadOnly = x.ReadOnly
                    })
                    .ToListAsync();

                var explicitProps = roleContIdsForExplicit.Count == 0
                    ? new Dictionary<decimal, (bool CanRead, bool CanWrite)>()
                    : (await _unitOfWork.Repository<RoleProp>()
                        .Query()
                        .Where(x => roleContIdsForExplicit.Contains(x.RoleContID) && x.Deleted == 0)
                        .Select(x => new { x.BasePropID, x.Read, x.Write })
                        .ToListAsync())
                    .GroupBy(x => x.BasePropID)
                    .ToDictionary(
                        x => x.Key,
                        x => (CanRead: x.Any(y => y.Read), CanWrite: x.Any(y => y.Write)));

                foreach (var property in baseProps)
                {
                    snapshot.Containers.TryGetValue(property.BaseContId, out var containerPermission);
                    explicitProps.TryGetValue(property.BasePropId, out var explicitPermission);

                    var canRead = anyGlobalPropRead || (containerPermission?.AllPropRead ?? false) || explicitPermission.CanRead;
                    var canWrite = (anyGlobalPropWrite || (containerPermission?.AllPropWrite ?? false) || explicitPermission.CanWrite) && !property.ReadOnly;

                    if (!canRead && !canWrite)
                    {
                        continue;
                    }

                    var sourceParts = new List<string>();
                    if (anyGlobalPropRead || anyGlobalPropWrite)
                    {
                        sourceParts.Add("GLOBAL");
                    }

                    if ((containerPermission?.AllPropRead ?? false) || (containerPermission?.AllPropWrite ?? false))
                    {
                        sourceParts.Add($"CONTAINER:{property.BaseContId}");
                    }

                    if (explicitPermission.CanRead || explicitPermission.CanWrite)
                    {
                        sourceParts.Add("EXPLICIT:RoleProp");
                    }

                    if (property.ReadOnly)
                    {
                        sourceParts.Add("READONLY");
                    }

                    snapshot.Properties[property.BasePropId] = new PropertyPermissionDto
                    {
                        BasePropId = property.BasePropId,
                        BaseContId = property.BaseContId,
                        BasePropName = property.Name,
                        ReadOnly = property.ReadOnly,
                        CanRead = canRead,
                        CanWrite = canWrite,
                        Source = string.Join('|', sourceParts)
                    };
                }

                AddTrace(snapshot, "PROPERTY_ACCESS", "ALLOW", $"PropertyPermissionCount={snapshot.Properties.Count}, ExplicitRoleProp={explicitProps.Count}");

                if (user.AllEmployee)
                {
                    snapshot.EmployeeScopeBypass = true;
                    AddTrace(snapshot, "DATA_SCOPE_EMPLOYEE", "ALLOW", "AllEmployee=1 -> UserEmp bypass");
                }
                else
                {
                    snapshot.AllowedEmployeeIds = await _unitOfWork.Repository<UserEmp>()
                        .Query()
                        .Where(x => x.UserID == userId && x.Deleted == 0)
                        .Select(x => x.EmployeeID)
                        .Distinct()
                        .OrderBy(x => x)
                        .ToListAsync();

                    AddTrace(snapshot, "DATA_SCOPE_EMPLOYEE", snapshot.AllowedEmployeeIds.Count > 0 ? "ALLOW" : "DENY", $"UserEmpCount={snapshot.AllowedEmployeeIds.Count}");
                }

                snapshot.AllowedFunctionIds = await _unitOfWork.Repository<UserFunc>()
                    .Query()
                    .Where(x => x.UserID == userId && x.Deleted == 0)
                    .Select(x => x.FunctionID)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();

                AddTrace(
                    snapshot,
                    "DATA_SCOPE_FUNCTION",
                    snapshot.AllowedFunctionIds.Count > 0 ? "ALLOW" : "DENY",
                    $"UserFuncCount={snapshot.AllowedFunctionIds.Count}");

                AddTrace(
                    snapshot,
                    "SNAPSHOT_SUMMARY",
                    "INFO",
                    $"Pages={snapshot.AllowedPages.Count}, Containers={snapshot.Containers.Count}, Methods={snapshot.Methods.Count}, Props={snapshot.Properties.Count}");

                return ServiceResult<PermissionSnapshotDto>.Success(snapshot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Permission snapshot build failed for user {UserId}", userId);
                return ServiceResult<PermissionSnapshotDto>.ServerError("Permission snapshot oluşturulurken hata oluştu.");
            }
        }

        private static string BuildCacheKey(decimal userId)
        {
            return $"permission:v1:user:{userId.ToString(CultureInfo.InvariantCulture)}";
        }

        private static string FormatNullableDecimal(decimal? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "NULL";
        }

        private static void AddTrace(PermissionSnapshotDto snapshot, string rule, string outcome, string detail)
        {
            snapshot.DecisionTrace.Add(new PermissionDecisionTraceDto
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Rule = rule,
                Outcome = outcome,
                Detail = detail
            });
        }

        private sealed class ActiveRoleRow
        {
            public decimal RoleId { get; init; }
            public string RoleName { get; init; } = string.Empty;
            public bool AllMenu { get; init; }
            public bool AllMethRead { get; init; }
            public bool AllMethWrite { get; init; }
            public bool AllPropRead { get; init; }
            public bool AllPropWrite { get; init; }
            public bool Global { get; init; }
        }

        private sealed class RoleContRow
        {
            public decimal RoleContId { get; init; }
            public decimal RoleId { get; init; }
            public decimal BaseContId { get; init; }
            public bool AllIntf { get; init; }
            public bool AllMethRead { get; init; }
            public bool AllMethWrite { get; init; }
            public bool AllPropRead { get; init; }
            public bool AllPropWrite { get; init; }
        }

        private sealed class BaseMethodRow
        {
            public decimal BaseMethId { get; init; }
            public decimal BaseContId { get; init; }
            public string Name { get; init; } = string.Empty;
            public bool ReadOnly { get; init; }
        }

        private sealed class BasePropertyRow
        {
            public decimal BasePropId { get; init; }
            public decimal BaseContId { get; init; }
            public string Name { get; init; } = string.Empty;
            public bool ReadOnly { get; init; }
        }
    }
}
