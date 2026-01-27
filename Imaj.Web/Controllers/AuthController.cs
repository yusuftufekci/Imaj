using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Service.Results;
using Imaj.Web.Controllers.Base;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Imaj.Web.Controllers
{
    /// <summary>
    /// Kimlik doğrulama (Authentication) işlemleri için controller.
    /// </summary>
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger) : base(logger)
        {
            _authService = authService;
        }


        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            if (!result.IsSuccess)
            {
                return Json(result);
            }

            if (result.Data == null)
            {
                 return Json(ServiceResult<object>.Fail("Bir hata oluştu."));
            }

            var principal = _authService.CreatePrincipal(result.Data);
            
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

            return Json(ServiceResult<object>.Success(default!));
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
