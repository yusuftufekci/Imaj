using System.Threading.Tasks;
using System.Globalization;
using System.Security.Claims;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Service.Results;
using Imaj.Web;
using Imaj.Web.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Imaj.Web.Controllers
{
    /// <summary>
    /// Kimlik doğrulama (Authentication) işlemleri için controller.
    /// </summary>
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;
        private readonly IPermissionService _permissionService;

        public AuthController(
            IAuthService authService,
            IPermissionService permissionService,
            ILogger<AuthController> logger,
            IStringLocalizer<SharedResource> localizer) : base(logger, localizer)
        {
            _authService = authService;
            _permissionService = permissionService;
        }


        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            if (!result.IsSuccess)
            {
                return Json(result);
            }

            if (result.Data == null)
            {
                 return Json(ServiceResult<object>.Fail(L("GenericError")));
            }

            var principal = _authService.CreatePrincipal(result.Data);
            
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = loginDto.RememberMe
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

            return Json(ServiceResult<object>.Success(default!));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (decimal.TryParse(userIdClaim, NumberStyles.Number, CultureInfo.InvariantCulture, out var userId))
            {
                await _permissionService.InvalidateAsync(userId);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
