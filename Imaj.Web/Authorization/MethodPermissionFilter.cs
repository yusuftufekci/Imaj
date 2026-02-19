using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Imaj.Web.Authorization
{
    public class MethodPermissionFilter : IAsyncActionFilter
    {
        private readonly decimal _baseMethId;
        private readonly bool _write;
        private readonly IPermissionViewService _permissionViewService;
        private readonly ILogger<MethodPermissionFilter> _logger;

        public MethodPermissionFilter(
            decimal baseMethId,
            bool write,
            IPermissionViewService permissionViewService,
            ILogger<MethodPermissionFilter> logger)
        {
            _baseMethId = baseMethId;
            _write = write;
            _permissionViewService = permissionViewService;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var allowed = await _permissionViewService.CanExecuteMethodAsync(_baseMethId, _write);
            if (!allowed)
            {
                _logger.LogWarning(
                    "Method guard deny. Controller={Controller}, Action={Action}, BaseMethID={BaseMethId}, Write={Write}",
                    context.RouteData.Values["controller"]?.ToString() ?? "NULL",
                    context.RouteData.Values["action"]?.ToString() ?? "NULL",
                    _baseMethId,
                    _write);

                context.Result = new ForbidResult();
                return;
            }

            await next();
        }
    }
}
