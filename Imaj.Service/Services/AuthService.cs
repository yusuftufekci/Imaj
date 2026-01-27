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
            // Yeni yapıda Username yerine Code kullanılıyor
            var user = await _unitOfWork.Repository<User>().SingleOrDefaultAsync(x => x.Code == loginDto.Username);
            
            if (user == null)
                return ServiceResult<UserDto>.Fail("Kullanıcı bulunamadı.");

            if (string.IsNullOrEmpty(loginDto.Password))
                return ServiceResult<UserDto>.Fail("Şifre boş olamaz.");

            var hashedPassword = HashPassword(loginDto.Password);
            
            // Yeni yapıda PasswordHash yerine Password kullanılıyor
            // Not: Mevcut sistemde password hashlenmiş mi yoksa düz metin mi saklanıyor?
            // Şimdilik kodun hash beklentisine uyuyoruz. Eğer db'de düz ise burası değişmeli.
            if (string.Equals(user.Password, hashedPassword, StringComparison.OrdinalIgnoreCase))
            {
                var userDto = new UserDto
                {
                    Id = user.Id,
                    Username = user.Code, // Code -> Username
                    Email = "", // Email bilgisi User entity'de yok
                    FullName = user.Name, // Name -> FullName
                    Role = "User" // Role bilgisi UserRole tablosundan çekilmeli, şimdilik sabit
                };
                return ServiceResult<UserDto>.Success(userDto);
            }

            return ServiceResult<UserDto>.Fail("Kullanıcı adı veya şifre hatalı.");
        }
    }
}
