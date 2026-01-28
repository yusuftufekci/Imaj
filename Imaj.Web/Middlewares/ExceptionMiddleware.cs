using System.Net;
using System.Text.Json;
using Imaj.Service.Results;
using Microsoft.Extensions.Logging;

namespace Imaj.Web.Middlewares
{
    /// <summary>
    /// Global exception handling middleware.
    /// Tüm yakalanmamış hataları loglar ve uygun response döner.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                // Structured logging ile hata kaydet
                _logger.LogError(ex, "Beklenmeyen bir hata oluştu. Path: {Path}", httpContext.Request.Path);
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // AJAX/API isteği mi kontrol et
            bool isJsonRequest = context.Request.Headers["Accept"].ToString().Contains("application/json") ||
                                 context.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (isJsonRequest)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                // Güvenlik: Exception detaylarını client'a gösterme, sadece logla
                var result = ServiceResult<string>.Fail("Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.");
                var json = JsonSerializer.Serialize(result);

                await context.Response.WriteAsync(json);
            }
            else
            {
                // Browser istekleri için Error sayfasına yönlendir
                context.Response.Redirect("/Home/Error");
            }
        }
    }
}

