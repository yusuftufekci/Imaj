using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Service.Constants;
using Imaj.Service.DTOs;
using Imaj.Service.Helpers;
using Imaj.Service.Interfaces;
using Imaj.Service.Results;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Imaj.Service.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPermissionService _permissionService;

        public AuthService(IUnitOfWork unitOfWork, IPermissionService permissionService)
        {
            _unitOfWork = unitOfWork;
            _permissionService = permissionService;
        }

        public string HashPassword(string password)
        {
            return PasswordHelper.HashPassword(password);
        }

        public ClaimsPrincipal CreatePrincipal(UserDto user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim(CustomClaimTypes.UserCode, user.Username ?? string.Empty),
                new Claim(CustomClaimTypes.AllEmployee, user.AllEmployee.ToString())
            };

            if (user.CompanyId.HasValue)
            {
                claims.Add(new Claim(CustomClaimTypes.CompanyId, user.CompanyId.Value.ToString(CultureInfo.InvariantCulture)));
            }

            var uniqueRoles = user.Roles
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (uniqueRoles.Count == 0 && !string.IsNullOrWhiteSpace(user.Role))
            {
                uniqueRoles.Add(user.Role);
            }

            foreach (var role in uniqueRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(claimsIdentity);
        }

        public async Task<ServiceResult<UserDto>> LoginAsync(LoginDto loginDto)
        {
            if (loginDto == null)
            {
                return ServiceResult<UserDto>.Fail("Giriş bilgisi boş olamaz.");
            }

            var username = loginDto.Username?.Trim();
            var password = loginDto.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username))
            {
                return ServiceResult<UserDto>.Fail("Kullanıcı kodu boş olamaz.");
            }

            if (string.IsNullOrEmpty(password))
            {
                return ServiceResult<UserDto>.Fail("Şifre boş olamaz.");
            }

            var user = await _unitOfWork.Repository<User>()
                .Query()
                .SingleOrDefaultAsync(x => x.Code == username);

            if (user == null)
            {
                return ServiceResult<UserDto>.Fail("Kullanıcı bulunamadı.");
            }

            if (user.Invisible)
            {
                return ServiceResult<UserDto>.Fail("Kullanıcı pasif olduğu için giriş yapamaz.");
            }

            // Legacy parity: password düz metin karşılaştırılır.
            if (!string.Equals(user.Password, password, StringComparison.Ordinal))
            {
                return ServiceResult<UserDto>.Fail("Kullanıcı adı veya şifre hatalı.");
            }

            var activeRoles = await (
                from userRole in _unitOfWork.Repository<UserRole>().Query()
                join role in _unitOfWork.Repository<Role>().Query() on userRole.RoleID equals role.Id
                where userRole.UserID == user.Id && userRole.Deleted == 0
                select new { role.Name, role.Global })
                .ToListAsync();

            if (activeRoles.Count == 0)
            {
                return ServiceResult<UserDto>.Fail("Aktif rol kaydı olmadığı için giriş yapılamaz.");
            }

            var hasSystemRole = activeRoles.Any(x => !x.Global);
            if (!user.CompanyID.HasValue && !hasSystemRole)
            {
                return ServiceResult<UserDto>.Fail("CompanyID NULL kullanıcı için sistem rolü zorunludur.");
            }

            var permissionResult = await _permissionService.GetOrBuildPermissionSetAsync(user.Id, forceRefresh: true);
            if (!permissionResult.IsSuccess || permissionResult.Data == null)
            {
                return ServiceResult<UserDto>.Fail(permissionResult.Message ?? "Yetki snapshot oluşturulamadı.");
            }

            if (permissionResult.Data.IsDenied)
            {
                return ServiceResult<UserDto>.Fail(permissionResult.Data.DenyReason ?? "Yetki politikası nedeniyle giriş reddedildi.");
            }

            var roleNames = activeRoles
                .Select(x => x.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Code,
                Email = string.Empty,
                FullName = user.Name,
                Role = roleNames.FirstOrDefault() ?? "User",
                Roles = roleNames,
                CompanyId = user.CompanyID,
                AllEmployee = user.AllEmployee
            };

            return ServiceResult<UserDto>.Success(userDto);
        }
    }
}
