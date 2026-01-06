using System.Net;
using System.Text.Json;
using Imaj.Service.Results;

namespace Imaj.Web.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Something went wrong: {ex}");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Check if request expects JSON (API/AJAX)
            bool isJsonRequest = context.Request.Headers["Accept"].ToString().Contains("application/json") ||
                                 context.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (isJsonRequest)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var result = ServiceResult<string>.Fail("Bir hata oluştu: " + exception.Message);
                var json = JsonSerializer.Serialize(result);

                await context.Response.WriteAsync(json);
            }
            else
            {
                // Redirect to Error page for browser requests
                context.Response.Redirect("/Home/Error");
            }
        }
    }
}
