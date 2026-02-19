using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using Imaj.Service.DTOs.Security;
using Imaj.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Imaj.Service.Services
{
    public class CurrentPermissionContext : ICurrentPermissionContext
    {
        private const string HttpContextSnapshotKey = "Imaj.PermissionSnapshot.CurrentRequest";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<CurrentPermissionContext> _logger;

        public CurrentPermissionContext(
            IHttpContextAccessor httpContextAccessor,
            IPermissionService permissionService,
            ILogger<CurrentPermissionContext> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _permissionService = permissionService;
            _logger = logger;
        }

        public bool TryGetCurrentUserId(out decimal userId)
        {
            userId = 0;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return decimal.TryParse(userIdClaim, NumberStyles.Number, CultureInfo.InvariantCulture, out userId);
        }

        public async Task<PermissionSnapshotDto?> GetSnapshotAsync(bool forceRefresh = false)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            if (!TryGetCurrentUserId(out var userId))
            {
                return null;
            }

            if (!forceRefresh && httpContext.Items.TryGetValue(HttpContextSnapshotKey, out var existing) && existing is PermissionSnapshotDto existingSnapshot)
            {
                return existingSnapshot;
            }

            var permissionResult = await _permissionService.GetOrBuildPermissionSetAsync(userId, forceRefresh);
            if (!permissionResult.IsSuccess || permissionResult.Data == null)
            {
                _logger.LogWarning(
                    "CurrentPermissionContext snapshot alınamadı. UserID={UserId}, Message={Message}",
                    userId,
                    permissionResult.Message ?? "NULL");

                return null;
            }

            httpContext.Items[HttpContextSnapshotKey] = permissionResult.Data;
            return permissionResult.Data;
        }
    }
}
