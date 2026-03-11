using Imaj.Service.Results;
using Imaj.Web;
using Imaj.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Globalization;

namespace Imaj.Web.Controllers.Base
{
    /// <summary>
    /// Tüm controller'lar için ortak base sınıf.
    /// Ortak metodlar ve helper'ları içerir.
    /// </summary>
    public abstract class BaseController : Controller
    {
        protected readonly ILogger _logger;
        private readonly IStringLocalizer<SharedResource>? _localizer;

        protected BaseController(ILogger logger)
        {
            _logger = logger;
        }

        protected BaseController(ILogger logger, IStringLocalizer<SharedResource> localizer)
        {
            _logger = logger;
            _localizer = localizer;
        }

        protected string L(string key)
        {
            if (_localizer != null)
            {
                return _localizer[key].Value;
            }

            if (key == "GenericError")
            {
                return CultureInfo.CurrentUICulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase)
                    ? "An error occurred."
                    : "Bir hata oluştu.";
            }

            return key;
        }

        /// <summary>
        /// ServiceResult'ı işleyerek uygun IActionResult döndürür.
        /// </summary>
        /// <typeparam name="T">Result data tipi</typeparam>
        /// <param name="result">İşlenecek ServiceResult</param>
        /// <param name="onSuccess">Başarılı durumda çalışacak fonksiyon</param>
        /// <returns>IActionResult</returns>
        protected IActionResult HandleResult<T>(ServiceResult<T> result, Func<T, IActionResult> onSuccess)
        {
            if (result.IsSuccess && result.Data != null)
            {
                return onSuccess(result.Data);
            }

            _logger.LogWarning("Operation failed: {Message}", result.Message);
            ShowError(result.Message ?? L("GenericError"));
            return RedirectToAction("Index");
        }

        /// <summary>
        /// ServiceResult'ı işleyerek uygun IActionResult döndürür (data olmadan).
        /// </summary>
        /// <param name="result">İşlenecek ServiceResult</param>
        /// <param name="onSuccess">Başarılı durumda çalışacak fonksiyon</param>
        /// <returns>IActionResult</returns>
        protected IActionResult HandleResult(ServiceResult result, Func<IActionResult> onSuccess)
        {
            if (result.IsSuccess)
            {
                return onSuccess();
            }

            _logger.LogWarning("Operation failed: {Message}", result.Message);
            ShowError(result.Message ?? L("GenericError"));
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Başarı mesajını TempData'ya ekler.
        /// </summary>
        /// <param name="message">Gösterilecek mesaj</param>
        protected void ShowSuccess(string message)
        {
            TempData["SuccessMessage"] = ControllerMessageLocalizationExtensions.LocalizeUiMessage(this, WebUtility.HtmlDecode(message));
        }

        /// <summary>
        /// Hata mesajını TempData'ya ekler.
        /// </summary>
        /// <param name="message">Gösterilecek mesaj</param>
        protected void ShowError(string message)
        {
            TempData["ErrorMessage"] = ControllerMessageLocalizationExtensions.LocalizeUiMessage(this, WebUtility.HtmlDecode(message));
        }
    }
}
