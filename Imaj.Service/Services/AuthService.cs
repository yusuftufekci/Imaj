using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Imaj.Service.DTOs;
using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Service.Interfaces;
using Imaj.Service.Results;
using Imaj.Service.Helpers;

namespace Imaj.Service.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(claimsIdentity);
        }

        public async Task<ServiceResult<UserDto>> LoginAsync(LoginDto loginDto)
        {
            var user = await _unitOfWork.Repository<User>().SingleOrDefaultAsync(x => x.Username == loginDto.Username);
            
            if (user == null)
                return ServiceResult<UserDto>.Fail("Kullanıcı bulunamadı.");

            if (string.IsNullOrEmpty(loginDto.Password))
                return ServiceResult<UserDto>.Fail("Şifre boş olamaz.");

            var hashedPassword = HashPassword(loginDto.Password);
            
            // Case insensitive check for legacy systems sometimes
            if (string.Equals(user.PasswordHash, hashedPassword, StringComparison.OrdinalIgnoreCase))
            {
                var userDto = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role
                };
                return ServiceResult<UserDto>.Success(userDto);
            }

            return ServiceResult<UserDto>.Fail("Kullanıcı adı veya şifre hatalı.");
        }
    }
}
