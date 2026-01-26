using Imaj.Service.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Imaj.Web.Controllers.Base
{
    /// <summary>
    /// Tüm controller'lar için ortak base sınıf.
    /// Ortak metodlar ve helper'ları içerir.
    /// </summary>
    public abstract class BaseController : Controller
    {
        protected readonly ILogger _logger;

        protected BaseController(ILogger logger)
        {
            _logger = logger;
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
            ShowError(result.Message ?? "Bir hata oluştu.");
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
            ShowError(result.Message ?? "Bir hata oluştu.");
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Başarı mesajını TempData'ya ekler.
        /// </summary>
        /// <param name="message">Gösterilecek mesaj</param>
        protected void ShowSuccess(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        /// <summary>
        /// Hata mesajını TempData'ya ekler.
        /// </summary>
        /// <param name="message">Gösterilecek mesaj</param>
        protected void ShowError(string message)
        {
            TempData["ErrorMessage"] = message;
        }
    }
}
