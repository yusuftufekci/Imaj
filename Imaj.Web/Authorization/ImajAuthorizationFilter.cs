using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using Imaj.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Imaj.Web.Authorization
{
    public class ImajAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly IPermissionService _permissionService;
        private readonly IPageRouteResolver _pageRouteResolver;
        private readonly ILogger<ImajAuthorizationFilter> _logger;

        public ImajAuthorizationFilter(
            IPermissionService permissionService,
            IPageRouteResolver pageRouteResolver,
            ILogger<ImajAuthorizationFilter> logger)
        {
            _permissionService = permissionService;
            _pageRouteResolver = pageRouteResolver;
            _logger = logger;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
            {
                return;
            }

            if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
            {
                return;
            }

            if (context.ActionDescriptor is not ControllerActionDescriptor actionDescriptor)
            {
                return;
            }

            var controller = actionDescriptor.ControllerName;
            var action = actionDescriptor.ActionName;

            var routeMatch = await _pageRouteResolver.ResolveAsync(controller, action);

            context.HttpContext.Items["ImajRouteGuard.MatchStatus"] = routeMatch.MatchStatus;
            context.HttpContext.Items["ImajRouteGuard.AspPage"] = routeMatch.AspPage ?? string.Empty;

            if (string.Equals(routeMatch.MatchStatus, "Bypass-SecurityEndpoint", StringComparison.OrdinalIgnoreCase)
                || string.Equals(routeMatch.MatchStatus, "Bypass-Home", StringComparison.OrdinalIgnoreCase)
                || string.Equals(routeMatch.MatchStatus, "Bypass-Culture", StringComparison.OrdinalIgnoreCase)
                || string.Equals(routeMatch.MatchStatus, "Bypass-PasswordChange", StringComparison.OrdinalIgnoreCase)
                || string.Equals(routeMatch.MatchStatus, "Bypass-EmployeeLookupApi", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!routeMatch.IsMapped || !routeMatch.BaseIntfId.HasValue)
            {
                _logger.LogWarning(
                    "Route guard deny: map yok. Controller={Controller}, Action={Action}, MatchStatus={MatchStatus}, Reason={Reason}",
                    controller,
                    action,
                    routeMatch.MatchStatus,
                    routeMatch.Reason);

                context.Result = new ForbidResult();
                return;
            }

            var userIdClaim = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!decimal.TryParse(userIdClaim, NumberStyles.Number, CultureInfo.InvariantCulture, out var userId))
            {
                _logger.LogWarning(
                    "Route guard deny: NameIdentifier claim parse edilemedi. Controller={Controller}, Action={Action}, Claim={Claim}",
                    controller,
                    action,
                    userIdClaim ?? "NULL");

                context.Result = new ForbidResult();
                return;
            }

            var permissionResult = await _permissionService.GetOrBuildPermissionSetAsync(userId);
            if (!permissionResult.IsSuccess || permissionResult.Data == null)
            {
                _logger.LogWarning(
                    "Route guard deny: permission snapshot alınamadı. UserID={UserId}, Controller={Controller}, Action={Action}, Message={Message}",
                    userId,
                    controller,
                    action,
                    permissionResult.Message ?? "NULL");

                context.Result = new ForbidResult();
                return;
            }

            var snapshot = permissionResult.Data;
            if (snapshot.IsDenied)
            {
                _logger.LogWarning(
                    "Route guard deny: snapshot denied. UserID={UserId}, Controller={Controller}, Action={Action}, Reason={Reason}",
                    userId,
                    controller,
                    action,
                    snapshot.DenyReason ?? "NULL");

                context.Result = new ForbidResult();
                return;
            }

            var canAccess = snapshot.CanAccessPage(routeMatch.BaseIntfId.Value);
            if (!canAccess)
            {
                _logger.LogWarning(
                    "Route guard deny: page not allowed. UserID={UserId}, Controller={Controller}, Action={Action}, AspPage={AspPage}, BaseIntfID={BaseIntfId}",
                    userId,
                    controller,
                    action,
                    routeMatch.AspPage ?? "NULL",
                    routeMatch.BaseIntfId.Value);

                context.Result = new ForbidResult();
                return;
            }

            _logger.LogDebug(
                "Route guard allow. UserID={UserId}, Controller={Controller}, Action={Action}, AspPage={AspPage}, BaseIntfID={BaseIntfId}",
                userId,
                controller,
                action,
                routeMatch.AspPage ?? "NULL",
                routeMatch.BaseIntfId.Value);
        }
    }
}
