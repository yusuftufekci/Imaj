using Imaj.Web.Services.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Imaj.Web.Extensions
{
    public static class ControllerMessageLocalizationExtensions
    {
        public static string LocalizeUiMessage(this ControllerBase controller, string? message, string? fallback = null)
        {
            var localizer = controller.HttpContext.RequestServices.GetService(typeof(IUiMessageLocalizer)) as IUiMessageLocalizer;
            var localizedMessage = localizer?.Localize(message);

            if (!string.IsNullOrWhiteSpace(localizedMessage))
            {
                return localizedMessage!;
            }

            return fallback ?? string.Empty;
        }
    }
}
