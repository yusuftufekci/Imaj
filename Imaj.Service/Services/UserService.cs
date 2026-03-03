using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Service.DTOs;
using Imaj.Service.DTOs.Security;
using Imaj.Service.Interfaces;
using Imaj.Service.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imaj.Service.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentPermissionContext _currentPermissionContext;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUnitOfWork unitOfWork,
            ICurrentPermissionContext currentPermissionContext,
            IPermissionService permissionService,
            ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentPermissionContext = currentPermissionContext;
            _permissionService = permissionService;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResultDto<UserListItemDto>>> GetUsersAsync(UserFilterDto filter)
        {
            var first = filter.First.HasValue && filter.First.Value > 0
                ? filter.First.Value
                : (int?)null;
            var page = filter.Page > 0 ? filter.Page : 1;
            var pageSize = filter.PageSize > 0 ? filter.PageSize : 16;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<PagedResultDto<UserListItemDto>>.Success(EmptyPage<UserListItemDto>(page, pageSize));
            }

            var scopedUsers = ApplyUserScope(_unitOfWork.Repository<User>().Query(), snapshot!);

            var query = from user in scopedUsers
                        join language in _unitOfWork.Repository<Language>().Query() on user.LanguageID equals language.Id into languageGroup
                        from language in languageGroup.DefaultIfEmpty()
                        join company in _unitOfWork.Repository<Company>().Query() on user.CompanyID equals (decimal?)company.Id into companyGroup
                        from company in companyGroup.DefaultIfEmpty()
                        select new UserListItemDto
                        {
                            Id = user.Id,
                            Code = user.Code,
                            Name = user.Name,
                            LanguageId = user.LanguageID,
                            LanguageName = language != null ? language.Name : string.Empty,
                            AllEmployee = user.AllEmployee,
                            Invisible = user.Invisible,
                            CompanyId = user.CompanyID,
                            CompanyName = company != null ? company.Name : string.Empty
                        };

            if (!string.IsNullOrWhiteSpace(filter.Code))
            {
                var code = filter.Code.Trim();
                query = query.Where(x => x.Code.Contains(code));
            }

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                var name = filter.Name.Trim();
                query = query.Where(x => x.Name.Contains(name));
            }

            if (filter.IsInvalid.HasValue)
            {
                query = query.Where(x => x.Invisible == filter.IsInvalid.Value);
            }

            if (first.HasValue)
            {
                var firstScope = query
                    .OrderBy(x => x.Code)
                    .Take(first.Value);

                var firstTotalCount = await firstScope.CountAsync();
                var firstPageItems = await firstScope
                    .OrderBy(x => x.Code)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return ServiceResult<PagedResultDto<UserListItemDto>>.Success(new PagedResultDto<UserListItemDto>
                {
                    Items = firstPageItems,
                    TotalCount = firstTotalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(x => x.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResultDto<UserListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return ServiceResult<PagedResultDto<UserListItemDto>>.Success(result);
        }

        public async Task<ServiceResult<UserDetailDto>> GetUserDetailAsync(string userCodeOrId)
        {
            if (string.IsNullOrWhiteSpace(userCodeOrId))
            {
                return ServiceResult<UserDetailDto>.Fail("Kullanici kodu veya ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<UserDetailDto>.NotFound("Kullanici bulunamadi.");
            }

            var scopedUsers = ApplyUserScope(_unitOfWork.Repository<User>().Query(), snapshot!);
            var userLookup = from user in scopedUsers
                             join language in _unitOfWork.Repository<Language>().Query() on user.LanguageID equals language.Id into languageGroup
                             from language in languageGroup.DefaultIfEmpty()
                             join company in _unitOfWork.Repository<Company>().Query() on user.CompanyID equals (decimal?)company.Id into companyGroup
                             from company in companyGroup.DefaultIfEmpty()
                             select new
                             {
                                 user.Id,
                                 user.Code,
                                 user.Name,
                                 user.LanguageID,
                                 LanguageName = language != null ? language.Name : string.Empty,
                                 user.CompanyID,
                                 CompanyName = company != null ? company.Name : string.Empty,
                                 user.AllEmployee,
                                 user.Invisible
                             };

            var trimmedKey = userCodeOrId.Trim();
            if (decimal.TryParse(trimmedKey, NumberStyles.Number, CultureInfo.InvariantCulture, out var userId))
            {
                userLookup = userLookup.Where(x => x.Id == userId);
            }
            else
            {
                userLookup = userLookup.Where(x => x.Code == trimmedKey);
            }

            var row = await userLookup.SingleOrDefaultAsync();
            if (row == null)
            {
                return ServiceResult<UserDetailDto>.NotFound("Kullanici bulunamadi.");
            }

            var roles = await (from userRole in _unitOfWork.Repository<UserRole>().Query()
                               join role in _unitOfWork.Repository<Role>().Query() on userRole.RoleID equals role.Id
                               where userRole.UserID == row.Id && userRole.Deleted == 0
                               orderby role.Name
                               select new UserRoleAssignmentDto
                               {
                                   RoleId = role.Id,
                                   Name = role.Name,
                                   Invisible = role.Invisible,
                                   Global = role.Global,
                                   AllMenu = role.AllMenu
                               })
                .ToListAsync();

            var functions = await GetUserFunctionsAsync(row.Id, row.LanguageID, snapshot!);
            var employees = await GetUserEmployeesAsync(row.Id, snapshot!);

            var detail = new UserDetailDto
            {
                Id = row.Id,
                Code = row.Code,
                Name = row.Name,
                LanguageId = row.LanguageID,
                LanguageName = row.LanguageName,
                CompanyId = row.CompanyID,
                CompanyName = row.CompanyName,
                AllEmployee = row.AllEmployee,
                Invisible = row.Invisible,
                Roles = roles,
                Functions = functions,
                Employees = employees
            };

            return ServiceResult<UserDetailDto>.Success(detail);
        }

        public async Task<ServiceResult<List<UserLanguageDto>>> GetLanguagesAsync()
        {
            var items = await _unitOfWork.Repository<Language>()
                .Query()
                .OrderBy(x => x.Sequence)
                .ThenBy(x => x.Id)
                .Select(x => new UserLanguageDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            return ServiceResult<List<UserLanguageDto>>.Success(items);
        }

        public async Task<ServiceResult<UserCompanyContextDto>> GetCompanyContextAsync()
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<UserCompanyContextDto>.Fail("Sirket kapsam bilgisi alinamadi.");
            }

            var context = new UserCompanyContextDto
            {
                CompanyId = snapshot!.CompanyId,
                CompanyName = "Sistem Geneli"
            };

            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound && snapshot.CompanyId.HasValue)
            {
                var companyName = await _unitOfWork.Repository<Company>()
                    .Query()
                    .Where(x => x.Id == snapshot.CompanyId.Value)
                    .Select(x => x.Name)
                    .SingleOrDefaultAsync();

                context.CompanyName = string.IsNullOrWhiteSpace(companyName)
                    ? snapshot.CompanyId.Value.ToString(CultureInfo.InvariantCulture)
                    : companyName;
            }

            return ServiceResult<UserCompanyContextDto>.Success(context);
        }

        public async Task<ServiceResult<PagedResultDto<RoleLookupItemDto>>> SearchRolesAsync(RoleLookupFilterDto filter)
        {
            var page = filter.Page > 0 ? filter.Page : 1;
            var pageSize = filter.PageSize > 0 ? filter.PageSize : 16;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<PagedResultDto<RoleLookupItemDto>>.Success(EmptyPage<RoleLookupItemDto>(page, pageSize));
            }

            var query = _unitOfWork.Repository<Role>()
                .Query();

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                var name = filter.Name.Trim();
                query = query.Where(x => x.Name.Contains(name));
            }

            if (filter.IsInvalid.HasValue)
            {
                query = query.Where(x => x.Invisible == filter.IsInvalid.Value);
            }

            if (filter.ExcludeIds.Count > 0)
            {
                query = query.Where(x => !filter.ExcludeIds.Contains(x.Id));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new RoleLookupItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Invisible = x.Invisible,
                    Global = x.Global,
                    AllMenu = x.AllMenu
                })
                .ToListAsync();

            var result = new PagedResultDto<RoleLookupItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return ServiceResult<PagedResultDto<RoleLookupItemDto>>.Success(result);
        }

        public async Task<ServiceResult<PagedResultDto<FunctionLookupItemDto>>> SearchFunctionsAsync(FunctionLookupFilterDto filter)
        {
            var page = filter.Page > 0 ? filter.Page : 1;
            var pageSize = filter.PageSize > 0 ? filter.PageSize : 16;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot) || snapshot!.AllowedFunctionIds.Count == 0)
            {
                return ServiceResult<PagedResultDto<FunctionLookupItemDto>>.Success(EmptyPage<FunctionLookupItemDto>(page, pageSize));
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var functionQuery = _unitOfWork.Repository<Function>()
                .Query()
                .Where(x => snapshot.AllowedFunctionIds.Contains(x.Id));

            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                if (!snapshot.CompanyId.HasValue)
                {
                    return ServiceResult<PagedResultDto<FunctionLookupItemDto>>.Success(EmptyPage<FunctionLookupItemDto>(page, pageSize));
                }

                functionQuery = functionQuery.Where(x => x.CompanyID == snapshot.CompanyId.Value);
            }

            if (filter.IsInvalid.HasValue)
            {
                functionQuery = functionQuery.Where(x => x.Invisible == filter.IsInvalid.Value);
            }

            if (filter.ExcludeIds.Count > 0)
            {
                functionQuery = functionQuery.Where(x => !filter.ExcludeIds.Contains(x.Id));
            }

            var translatedQuery =
                from function in functionQuery
                join preferred in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == languageId)
                    on function.Id equals preferred.FunctionID into preferredGroup
                from preferred in preferredGroup.DefaultIfEmpty()
                join fallback in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on function.Id equals fallback.FunctionID into fallbackGroup
                from fallback in fallbackGroup.DefaultIfEmpty()
                select new FunctionLookupItemDto
                {
                    Id = function.Id,
                    Name = preferred != null
                        ? preferred.Name
                        : (fallback != null ? fallback.Name : string.Empty),
                    Invisible = function.Invisible
                };

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                var name = filter.Name.Trim();
                translatedQuery = translatedQuery.Where(x => x.Name.Contains(name));
            }

            var totalCount = await translatedQuery.CountAsync();
            var items = await translatedQuery
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                item.Name = item.Id.ToString(CultureInfo.InvariantCulture);
            }

            var result = new PagedResultDto<FunctionLookupItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return ServiceResult<PagedResultDto<FunctionLookupItemDto>>.Success(result);
        }

        public async Task<ServiceResult> CreateUserAsync(UserUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Kullanici bilgisi bos olamaz.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Kullanici kaydi icin yetki kapsami bulunamadi.");
            }

            var activeSnapshot = snapshot!;
            var normalizedCode = NormalizeCode(input.Code);
            var normalizedName = (input.Name ?? string.Empty).Trim();
            var normalizedPassword = NormalizePassword(input.Password, normalizedCode);
            var normalizedRoleIds = NormalizePositiveIds(input.RoleIds);
            var normalizedFunctionIds = NormalizePositiveIds(input.FunctionIds);
            var normalizedEmployeeIds = input.AllEmployee
                ? new List<decimal>()
                : NormalizePositiveIds(input.EmployeeIds);

            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                return ServiceResult.Fail("Kullanici kodu zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return ServiceResult.Fail("Kullanici adi zorunludur.");
            }

            if (normalizedCode.Length > 16)
            {
                return ServiceResult.Fail("Kullanici kodu en fazla 16 karakter olabilir.");
            }

            if (normalizedName.Length > 48)
            {
                return ServiceResult.Fail("Kullanici adi en fazla 48 karakter olabilir.");
            }

            if (string.IsNullOrWhiteSpace(normalizedPassword))
            {
                return ServiceResult.Fail("Sifre bos olamaz.");
            }

            if (normalizedPassword.Length > 32)
            {
                return ServiceResult.Fail("Sifre en fazla 32 karakter olabilir.");
            }

            if (normalizedRoleIds.Count == 0)
            {
                return ServiceResult.Fail("En az bir rol secilmelidir.");
            }

            if (input.LanguageId <= 0)
            {
                return ServiceResult.Fail("Dil secimi zorunludur.");
            }

            var targetCompanyId = ResolveTargetCompanyIdForCreate(input.CompanyId, activeSnapshot);
            if (activeSnapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound && !targetCompanyId.HasValue)
            {
                return ServiceResult.Fail("Company-bound kapsamda sirket bilgisi zorunludur.");
            }

            var userRepo = _unitOfWork.Repository<User>();
            var existsSameCode = await userRepo.Query().AnyAsync(x => x.Code == normalizedCode);
            if (existsSameCode)
            {
                return ServiceResult.Fail($"'{normalizedCode}' kodlu kullanici zaten mevcut.");
            }

            var languageExists = await _unitOfWork.Repository<Language>()
                .Query()
                .AnyAsync(x => x.Id == input.LanguageId);
            if (!languageExists)
            {
                return ServiceResult.Fail("Secilen dil kaydi bulunamadi.");
            }

            if (targetCompanyId.HasValue)
            {
                var companyExists = await _unitOfWork.Repository<Company>()
                    .Query()
                    .AnyAsync(x => x.Id == targetCompanyId.Value);
                if (!companyExists)
                {
                    return ServiceResult.Fail("Secilen sirket kaydi bulunamadi.");
                }
            }

            var roleRows = await _unitOfWork.Repository<Role>()
                .Query()
                .Where(x => normalizedRoleIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Global })
                .ToListAsync();

            if (roleRows.Count != normalizedRoleIds.Count)
            {
                return ServiceResult.Fail("Secili rollerden en az biri gecersiz.");
            }

            if (!targetCompanyId.HasValue && !roleRows.Any(x => !x.Global))
            {
                return ServiceResult.Fail("CompanyID NULL kullanici icin en az bir sistem rolu (Role.Global=0) zorunludur.");
            }

            var functionValidation = await ValidateFunctionAssignmentsAsync(
                normalizedFunctionIds,
                targetCompanyId,
                activeSnapshot);
            if (!functionValidation.IsSuccess)
            {
                return ServiceResult.Fail(functionValidation.Message ?? "Fonksiyon atamalari dogrulanamadi.");
            }

            var employeeValidation = await ValidateEmployeeAssignmentsAsync(
                normalizedEmployeeIds,
                targetCompanyId,
                activeSnapshot,
                input.AllEmployee);
            if (!employeeValidation.IsSuccess)
            {
                return ServiceResult.Fail(employeeValidation.Message ?? "Calisan atamalari dogrulanamadi.");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var nextUserId = (await userRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;

                var user = new User
                {
                    Id = nextUserId,
                    LanguageID = input.LanguageId,
                    CompanyID = targetCompanyId,
                    Code = normalizedCode,
                    Name = normalizedName,
                    Password = normalizedPassword,
                    AllEmployee = input.AllEmployee,
                    Invisible = input.Invisible,
                    SelectFlag = false,
                    Stamp = 1
                };

                await userRepo.AddAsync(user);
                await AddUserRoleMappingsAsync(nextUserId, normalizedRoleIds);
                await AddUserFunctionMappingsAsync(nextUserId, normalizedFunctionIds);
                await AddUserEmployeeMappingsAsync(nextUserId, employeeValidation.Data ?? new List<decimal>());

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                await _permissionService.InvalidateAsync(nextUserId);
                return ServiceResult.Success("Kullanici kaydedildi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Kullanici kayit hatasi. Code={Code}", normalizedCode);
                return ServiceResult.Fail("Kullanici kaydedilirken hata olustu.");
            }
        }

        public async Task<ServiceResult> UpdateUserAsync(UserUpsertDto input)
        {
            if (input == null || !input.Id.HasValue || input.Id.Value <= 0)
            {
                return ServiceResult.Fail("Guncellenecek kullanici bulunamadi.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Kullanici guncelleme icin yetki kapsami bulunamadi.");
            }

            var activeSnapshot = snapshot!;
            var normalizedCode = NormalizeCode(input.Code);
            var normalizedName = (input.Name ?? string.Empty).Trim();
            var normalizedRoleIds = NormalizePositiveIds(input.RoleIds);
            var normalizedFunctionIds = NormalizePositiveIds(input.FunctionIds);
            var normalizedEmployeeIds = input.AllEmployee
                ? new List<decimal>()
                : NormalizePositiveIds(input.EmployeeIds);

            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                return ServiceResult.Fail("Kullanici kodu zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return ServiceResult.Fail("Kullanici adi zorunludur.");
            }

            if (normalizedCode.Length > 16)
            {
                return ServiceResult.Fail("Kullanici kodu en fazla 16 karakter olabilir.");
            }

            if (normalizedName.Length > 48)
            {
                return ServiceResult.Fail("Kullanici adi en fazla 48 karakter olabilir.");
            }

            if (normalizedRoleIds.Count == 0)
            {
                return ServiceResult.Fail("En az bir rol secilmelidir.");
            }

            if (input.LanguageId <= 0)
            {
                return ServiceResult.Fail("Dil secimi zorunludur.");
            }

            var userRepo = _unitOfWork.Repository<User>();
            var user = await ApplyUserScope(userRepo.Query(), activeSnapshot)
                .SingleOrDefaultAsync(x => x.Id == input.Id.Value);

            if (user == null)
            {
                return ServiceResult.NotFound("Kullanici bulunamadi.");
            }

            var targetCompanyId = ResolveTargetCompanyIdForUpdate(input.CompanyId, user.CompanyID, activeSnapshot);
            if (activeSnapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound && !targetCompanyId.HasValue)
            {
                return ServiceResult.Fail("Company-bound kapsamda sirket bilgisi zorunludur.");
            }

            if (targetCompanyId.HasValue)
            {
                var companyExists = await _unitOfWork.Repository<Company>()
                    .Query()
                    .AnyAsync(x => x.Id == targetCompanyId.Value);
                if (!companyExists)
                {
                    return ServiceResult.Fail("Secilen sirket kaydi bulunamadi.");
                }
            }

            var languageExists = await _unitOfWork.Repository<Language>()
                .Query()
                .AnyAsync(x => x.Id == input.LanguageId);
            if (!languageExists)
            {
                return ServiceResult.Fail("Secilen dil kaydi bulunamadi.");
            }

            var existsSameCode = await userRepo.Query()
                .AnyAsync(x => x.Id != user.Id && x.Code == normalizedCode);
            if (existsSameCode)
            {
                return ServiceResult.Fail($"'{normalizedCode}' kodlu kullanici zaten mevcut.");
            }

            var roleRows = await _unitOfWork.Repository<Role>()
                .Query()
                .Where(x => normalizedRoleIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Global })
                .ToListAsync();
            if (roleRows.Count != normalizedRoleIds.Count)
            {
                return ServiceResult.Fail("Secili rollerden en az biri gecersiz.");
            }

            if (!targetCompanyId.HasValue && !roleRows.Any(x => !x.Global))
            {
                return ServiceResult.Fail("CompanyID NULL kullanici icin en az bir sistem rolu (Role.Global=0) zorunludur.");
            }

            var functionValidation = await ValidateFunctionAssignmentsAsync(
                normalizedFunctionIds,
                targetCompanyId,
                activeSnapshot);
            if (!functionValidation.IsSuccess)
            {
                return ServiceResult.Fail(functionValidation.Message ?? "Fonksiyon atamalari dogrulanamadi.");
            }

            var employeeValidation = await ValidateEmployeeAssignmentsAsync(
                normalizedEmployeeIds,
                targetCompanyId,
                activeSnapshot,
                input.AllEmployee);
            if (!employeeValidation.IsSuccess)
            {
                return ServiceResult.Fail(employeeValidation.Message ?? "Calisan atamalari dogrulanamadi.");
            }

            var normalizedPassword = NormalizePassword(input.Password, user.Password);
            if (string.IsNullOrWhiteSpace(normalizedPassword))
            {
                return ServiceResult.Fail("Sifre bos olamaz.");
            }

            if (normalizedPassword.Length > 32)
            {
                return ServiceResult.Fail("Sifre en fazla 32 karakter olabilir.");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                user.Code = normalizedCode;
                user.Name = normalizedName;
                user.Password = normalizedPassword;
                user.LanguageID = input.LanguageId;
                user.CompanyID = targetCompanyId;
                user.AllEmployee = input.AllEmployee;
                user.Invisible = input.Invisible;
                user.SelectFlag = false;
                user.Stamp = 1;
                userRepo.Update(user);

                await ReplaceUserRoleMappingsAsync(user.Id, normalizedRoleIds);
                await ReplaceUserFunctionMappingsAsync(user.Id, normalizedFunctionIds);
                await ReplaceUserEmployeeMappingsAsync(user.Id, employeeValidation.Data ?? new List<decimal>());

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                await _permissionService.InvalidateAsync(user.Id);
                return ServiceResult.Success("Kullanici guncellendi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Kullanici guncelleme hatasi. UserID={UserId}", user.Id);
                return ServiceResult.Fail("Kullanici guncellenirken hata olustu.");
            }
        }

        public async Task<ServiceResult> ChangeCurrentUserPasswordAsync(ChangeCurrentUserPasswordDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Sifre degistirme bilgisi bos olamaz.");
            }

            if (!_currentPermissionContext.TryGetCurrentUserId(out var currentUserId))
            {
                return ServiceResult.Fail("Kullanici oturumu bulunamadi.");
            }

            var currentPassword = input.CurrentPassword ?? string.Empty;
            var newPassword = input.NewPassword ?? string.Empty;

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                return ServiceResult.Fail("Mevcut sifre zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                return ServiceResult.Fail("Yeni sifre zorunludur.");
            }

            if (newPassword.Length > 32)
            {
                return ServiceResult.Fail("Sifre en fazla 32 karakter olabilir.");
            }

            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.Query().SingleOrDefaultAsync(x => x.Id == currentUserId);
            if (user == null)
            {
                return ServiceResult.NotFound("Kullanici bulunamadi.");
            }

            if (!string.Equals(user.Password, currentPassword, StringComparison.Ordinal))
            {
                return ServiceResult.Fail("Mevcut sifre hatali.");
            }

            if (string.Equals(user.Password, newPassword, StringComparison.Ordinal))
            {
                return ServiceResult.Fail("Yeni sifre mevcut sifre ile ayni olamaz.");
            }

            try
            {
                user.Password = newPassword;
                user.Stamp = 1;
                userRepo.Update(user);

                await _unitOfWork.CommitAsync();
                await _permissionService.InvalidateAsync(user.Id);

                return ServiceResult.Success("Sifre basariyla degistirildi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sifre degistirme hatasi. UserID={UserId}", currentUserId);
                return ServiceResult.Fail("Sifre degistirilirken hata olustu.");
            }
        }

        private async Task<ServiceResult<List<decimal>>> ValidateFunctionAssignmentsAsync(
            IReadOnlyCollection<decimal> functionIds,
            decimal? targetCompanyId,
            PermissionSnapshotDto snapshot)
        {
            if (functionIds.Count == 0)
            {
                return ServiceResult<List<decimal>>.Success(new List<decimal>());
            }

            if (snapshot.AllowedFunctionIds.Count == 0)
            {
                return ServiceResult<List<decimal>>.Fail("Bu kullanici kapsaminda fonksiyon atama yetkisi bulunmuyor.");
            }

            if (functionIds.Any(id => !snapshot.AllowedFunctionIds.Contains(id)))
            {
                return ServiceResult<List<decimal>>.Fail("Kapsam disi fonksiyon secimi tespit edildi.");
            }

            var functionQuery = _unitOfWork.Repository<Function>()
                .Query()
                .Where(x => functionIds.Contains(x.Id));

            if (targetCompanyId.HasValue)
            {
                functionQuery = functionQuery.Where(x => x.CompanyID == targetCompanyId.Value);
            }

            var validIds = await functionQuery
                .Select(x => x.Id)
                .Distinct()
                .ToListAsync();

            if (validIds.Count != functionIds.Count)
            {
                return ServiceResult<List<decimal>>.Fail("Secilen fonksiyonlardan en az biri gecersiz veya sirket kapsami disinda.");
            }

            return ServiceResult<List<decimal>>.Success(validIds.OrderBy(x => x).ToList());
        }

        private async Task<ServiceResult<List<decimal>>> ValidateEmployeeAssignmentsAsync(
            IReadOnlyCollection<decimal> employeeIds,
            decimal? targetCompanyId,
            PermissionSnapshotDto snapshot,
            bool allEmployee)
        {
            if (allEmployee || employeeIds.Count == 0)
            {
                return ServiceResult<List<decimal>>.Success(new List<decimal>());
            }

            if (!snapshot.EmployeeScopeBypass)
            {
                if (snapshot.AllowedEmployeeIds.Count == 0)
                {
                    return ServiceResult<List<decimal>>.Fail("Bu kullanici kapsaminda calisan atama yetkisi bulunmuyor.");
                }

                if (employeeIds.Any(id => !snapshot.AllowedEmployeeIds.Contains(id)))
                {
                    return ServiceResult<List<decimal>>.Fail("Kapsam disi calisan secimi tespit edildi.");
                }
            }

            var employeeQuery = _unitOfWork.Repository<Employee>()
                .Query()
                .Where(x => employeeIds.Contains(x.Id));

            if (targetCompanyId.HasValue)
            {
                employeeQuery = employeeQuery.Where(x => x.CompanyID == targetCompanyId.Value);
            }

            var validIds = await employeeQuery
                .Select(x => x.Id)
                .Distinct()
                .ToListAsync();

            if (validIds.Count != employeeIds.Count)
            {
                return ServiceResult<List<decimal>>.Fail("Secilen calisanlardan en az biri gecersiz veya sirket kapsami disinda.");
            }

            return ServiceResult<List<decimal>>.Success(validIds.OrderBy(x => x).ToList());
        }

        private async Task ReplaceUserRoleMappingsAsync(decimal userId, IReadOnlyCollection<decimal> roleIds)
        {
            var userRoleRepo = _unitOfWork.Repository<UserRole>();
            var existingRows = await userRoleRepo.Query()
                .Where(x => x.UserID == userId && x.Deleted == 0)
                .ToListAsync();

            foreach (var row in existingRows)
            {
                row.Deleted = 1;
                row.SelectFlag = false;
                row.Stamp = 1;
                userRoleRepo.Update(row);
            }

            await AddUserRoleMappingsAsync(userId, roleIds);
        }

        private async Task ReplaceUserFunctionMappingsAsync(decimal userId, IReadOnlyCollection<decimal> functionIds)
        {
            var userFuncRepo = _unitOfWork.Repository<UserFunc>();
            var existingRows = await userFuncRepo.Query()
                .Where(x => x.UserID == userId && x.Deleted == 0)
                .ToListAsync();

            foreach (var row in existingRows)
            {
                row.Deleted = 1;
                row.SelectFlag = false;
                row.Stamp = 1;
                userFuncRepo.Update(row);
            }

            await AddUserFunctionMappingsAsync(userId, functionIds);
        }

        private async Task ReplaceUserEmployeeMappingsAsync(decimal userId, IReadOnlyCollection<decimal> employeeIds)
        {
            var userEmpRepo = _unitOfWork.Repository<UserEmp>();
            var existingRows = await userEmpRepo.Query()
                .Where(x => x.UserID == userId && x.Deleted == 0)
                .ToListAsync();

            foreach (var row in existingRows)
            {
                row.Deleted = 1;
                row.SelectFlag = false;
                row.Stamp = 1;
                userEmpRepo.Update(row);
            }

            await AddUserEmployeeMappingsAsync(userId, employeeIds);
        }

        private async Task AddUserRoleMappingsAsync(decimal userId, IReadOnlyCollection<decimal> roleIds)
        {
            if (roleIds.Count == 0)
            {
                return;
            }

            var userRoleRepo = _unitOfWork.Repository<UserRole>();
            var nextId = (await userRoleRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;

            foreach (var roleId in roleIds)
            {
                await userRoleRepo.AddAsync(new UserRole
                {
                    Id = nextId++,
                    UserID = userId,
                    RoleID = roleId,
                    Deleted = 0,
                    SelectFlag = false,
                    Stamp = 1
                });
            }
        }

        private async Task AddUserFunctionMappingsAsync(decimal userId, IReadOnlyCollection<decimal> functionIds)
        {
            if (functionIds.Count == 0)
            {
                return;
            }

            var userFuncRepo = _unitOfWork.Repository<UserFunc>();
            var nextId = (await userFuncRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;

            foreach (var functionId in functionIds)
            {
                await userFuncRepo.AddAsync(new UserFunc
                {
                    Id = nextId++,
                    UserID = userId,
                    FunctionID = functionId,
                    Deleted = 0,
                    SelectFlag = false,
                    Stamp = 1
                });
            }
        }

        private async Task AddUserEmployeeMappingsAsync(decimal userId, IReadOnlyCollection<decimal> employeeIds)
        {
            if (employeeIds.Count == 0)
            {
                return;
            }

            var userEmpRepo = _unitOfWork.Repository<UserEmp>();
            var nextId = (await userEmpRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;

            foreach (var employeeId in employeeIds)
            {
                await userEmpRepo.AddAsync(new UserEmp
                {
                    Id = nextId++,
                    UserID = userId,
                    EmployeeID = employeeId,
                    Deleted = 0,
                    SelectFlag = false,
                    Stamp = 1
                });
            }
        }

        private static string NormalizeCode(string? code)
        {
            return string.IsNullOrWhiteSpace(code)
                ? string.Empty
                : code.Trim().ToUpperInvariant();
        }

        private static string NormalizePassword(string? password, string fallback)
        {
            var trimmedPassword = password?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedPassword))
            {
                return trimmedPassword;
            }

            return fallback?.Trim() ?? string.Empty;
        }

        private static List<decimal> NormalizePositiveIds(IEnumerable<decimal>? ids)
        {
            return ids?
                .Where(x => x > 0)
                .Distinct()
                .OrderBy(x => x)
                .ToList()
                ?? new List<decimal>();
        }

        private static decimal? ResolveTargetCompanyIdForCreate(decimal? requestedCompanyId, PermissionSnapshotDto snapshot)
        {
            return snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound
                ? snapshot.CompanyId
                : requestedCompanyId;
        }

        private static decimal? ResolveTargetCompanyIdForUpdate(decimal? requestedCompanyId, decimal? existingCompanyId, PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                return snapshot.CompanyId;
            }

            return requestedCompanyId ?? existingCompanyId;
        }

        private async Task<List<UserFunctionAssignmentDto>> GetUserFunctionsAsync(decimal userId, decimal userLanguageId, PermissionSnapshotDto snapshot)
        {
            var functionIds = await _unitOfWork.Repository<UserFunc>()
                .Query()
                .Where(x => x.UserID == userId && x.Deleted == 0)
                .Select(x => x.FunctionID)
                .Distinct()
                .ToListAsync();

            if (functionIds.Count == 0)
            {
                return new List<UserFunctionAssignmentDto>();
            }

            var scopedFunctionQuery = _unitOfWork.Repository<Function>()
                .Query()
                .Where(x => functionIds.Contains(x.Id));

            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                if (!snapshot.CompanyId.HasValue)
                {
                    return new List<UserFunctionAssignmentDto>();
                }

                scopedFunctionQuery = scopedFunctionQuery.Where(x => x.CompanyID == snapshot.CompanyId.Value);
            }

            if (snapshot.AllowedFunctionIds.Count == 0)
            {
                return new List<UserFunctionAssignmentDto>();
            }

            scopedFunctionQuery = scopedFunctionQuery.Where(x => snapshot.AllowedFunctionIds.Contains(x.Id));

            var fallbackLanguageId = 1m;
            var functionRows = await (
                from function in scopedFunctionQuery
                join preferred in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == userLanguageId)
                    on function.Id equals preferred.FunctionID into preferredGroup
                from preferred in preferredGroup.DefaultIfEmpty()
                join fallback in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on function.Id equals fallback.FunctionID into fallbackGroup
                from fallback in fallbackGroup.DefaultIfEmpty()
                select new UserFunctionAssignmentDto
                {
                    FunctionId = function.Id,
                    Name = preferred != null
                        ? preferred.Name
                        : (fallback != null ? fallback.Name : string.Empty),
                    Invisible = function.Invisible
                })
                .OrderBy(x => x.Name)
                .ToListAsync();

            foreach (var item in functionRows.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                item.Name = item.FunctionId.ToString(CultureInfo.InvariantCulture);
            }

            return functionRows;
        }

        private async Task<List<UserEmployeeAssignmentDto>> GetUserEmployeesAsync(decimal userId, PermissionSnapshotDto snapshot)
        {
            var scopedUserEmp = _unitOfWork.Repository<UserEmp>()
                .Query()
                .Where(x => x.UserID == userId && x.Deleted == 0);

            if (!snapshot.EmployeeScopeBypass)
            {
                if (snapshot.AllowedEmployeeIds.Count == 0)
                {
                    return new List<UserEmployeeAssignmentDto>();
                }

                scopedUserEmp = scopedUserEmp.Where(x => snapshot.AllowedEmployeeIds.Contains(x.EmployeeID));
            }

            var query = from userEmp in scopedUserEmp
                        join employee in _unitOfWork.Repository<Employee>().Query() on userEmp.EmployeeID equals employee.Id
                        select new UserEmployeeAssignmentDto
                        {
                            EmployeeId = employee.Id,
                            Code = employee.Code,
                            Name = employee.Name,
                            Invisible = employee.Invisible
                        };

            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound && snapshot.CompanyId.HasValue)
            {
                query = from userEmp in scopedUserEmp
                        join employee in _unitOfWork.Repository<Employee>().Query() on userEmp.EmployeeID equals employee.Id
                        where employee.CompanyID == snapshot.CompanyId.Value
                        select new UserEmployeeAssignmentDto
                        {
                            EmployeeId = employee.Id,
                            Code = employee.Code,
                            Name = employee.Name,
                            Invisible = employee.Invisible
                        };
            }

            return await query
                .OrderBy(x => x.Code)
                .ToListAsync();
        }

        private static IQueryable<User> ApplyUserScope(IQueryable<User> query, PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                if (!snapshot.CompanyId.HasValue)
                {
                    return query.Where(_ => false);
                }

                query = query.Where(x => x.CompanyID == snapshot.CompanyId.Value);
            }

            return query;
        }

        private static bool IsScopeDenied(PermissionSnapshotDto? snapshot)
        {
            return snapshot == null
                || snapshot.IsDenied
                || snapshot.CompanyScopeMode == CompanyScopeMode.Deny;
        }

        private static PagedResultDto<T> EmptyPage<T>(int page, int pageSize)
        {
            return new PagedResultDto<T>
            {
                Items = new List<T>(),
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            };
        }

        private static decimal ResolveUiLanguageId()
        {
            var culture = CultureInfo.CurrentUICulture.Name;
            return culture.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? 2m : 1m;
        }
    }
}
