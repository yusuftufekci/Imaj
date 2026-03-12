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
    public class EmployeeService : IEmployeeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentPermissionContext _currentPermissionContext;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(
            IUnitOfWork unitOfWork,
            ICurrentPermissionContext currentPermissionContext,
            ILogger<EmployeeService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentPermissionContext = currentPermissionContext;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResultDto<EmployeeDto>>> GetEmployeesAsync(EmployeeFilterDto filter)
        {
            var normalizedFilter = filter ?? new EmployeeFilterDto();
            var page = normalizedFilter.Page > 0 ? normalizedFilter.Page : 1;
            var pageSize = normalizedFilter.PageSize > 0 ? normalizedFilter.PageSize : 10;

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<PagedResultDto<EmployeeDto>>.Success(EmptyPage<EmployeeDto>(page, pageSize));
            }

            var query = ApplyEmployeeScope(_unitOfWork.Repository<Employee>().Query(), snapshot!, requireFunctionScope: true);

            if (normalizedFilter.Status == 1)
            {
                query = query.Where(x => !x.Invisible);
            }
            else if (normalizedFilter.Status == 2)
            {
                query = query.Where(x => x.Invisible);
            }

            if (!string.IsNullOrWhiteSpace(normalizedFilter.Code))
            {
                query = query.Where(x => x.Code.Contains(normalizedFilter.Code));
            }

            if (!string.IsNullOrWhiteSpace(normalizedFilter.Name))
            {
                query = query.Where(x => x.Name.Contains(normalizedFilter.Name));
            }

            if (normalizedFilter.FunctionID.HasValue && normalizedFilter.FunctionID.Value > 0)
            {
                var functionId = normalizedFilter.FunctionID.Value;
                query = query.Where(x => x.EmpFuncs.Any(ef => ef.Deleted == 0 && ef.FunctionID == functionId));
            }

            var first = normalizedFilter.First.HasValue && normalizedFilter.First.Value > 0 ? normalizedFilter.First.Value : (int?)null;
            IQueryable<Employee> scopedQuery = query
                .OrderBy(x => x.Code)
                .ThenBy(x => x.Id);

            if (first.HasValue)
            {
                scopedQuery = scopedQuery.Take(first.Value);
            }

            var totalCount = await scopedQuery.CountAsync();
            var rows = await scopedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    CompanyId = x.CompanyID,
                    Invisible = x.Invisible
                })
                .ToListAsync();

            var items = rows
                .Select(x => new EmployeeDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Invisible = x.Invisible
                })
                .ToList();

            if (items.Count > 0)
            {
                var employeeIds = items.Select(x => x.Id).ToList();
                var languageId = ResolveUiLanguageId();
                var fallbackLanguageId = 1m;

                var workTypeSelections = await (
                    from empWork in _unitOfWork.Repository<EmpWork>().Query()
                    join workType in _unitOfWork.Repository<WorkType>().Query()
                        on empWork.WorkTypeID equals workType.Id
                    join preferredName in _unitOfWork.Repository<XWorkType>().Query().Where(x => x.LanguageID == languageId)
                        on workType.Id equals preferredName.WorkTypeID into preferredNameGroup
                    from preferredName in preferredNameGroup.DefaultIfEmpty()
                    join fallbackName in _unitOfWork.Repository<XWorkType>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                        on workType.Id equals fallbackName.WorkTypeID into fallbackNameGroup
                    from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                    where employeeIds.Contains(empWork.EmployeeID)
                          && empWork.Deleted == 0
                    select new
                    {
                        empWork.EmployeeID,
                        empWork.Default,
                        WorkTypeId = workType.Id,
                        WorkTypeName = preferredName != null
                            ? preferredName.Name
                            : (fallbackName != null ? fallbackName.Name : string.Empty)
                    })
                    .ToListAsync();

                var workTypeMap = workTypeSelections
                    .GroupBy(x => x.EmployeeID)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .OrderByDescending(x => x.Default)
                            .ThenBy(x => string.IsNullOrWhiteSpace(x.WorkTypeName) ? 1 : 0)
                            .ThenBy(x => x.WorkTypeName)
                            .ThenBy(x => x.WorkTypeId)
                            .First());

                foreach (var item in items)
                {
                    if (workTypeMap.TryGetValue(item.Id, out var workType))
                    {
                        item.DefaultWorkTypeId = workType.WorkTypeId;
                        item.DefaultWorkTypeName = string.IsNullOrWhiteSpace(workType.WorkTypeName)
                            ? workType.WorkTypeId.ToString(CultureInfo.InvariantCulture)
                            : workType.WorkTypeName;
                    }
                }
            }

            return ServiceResult<PagedResultDto<EmployeeDto>>.Success(new PagedResultDto<EmployeeDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult<List<FunctionDto>>> GetFunctionsAsync()
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<List<FunctionDto>>.Success(new List<FunctionDto>());
            }

            var options = await QueryFunctionOptionsAsync(snapshot!, requireCompanyId: false);
            var data = options
                .Select(x => new FunctionDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();

            return ServiceResult<List<FunctionDto>>.Success(data);
        }

        public async Task<ServiceResult<EmployeeDetailDto>> GetEmployeeDetailAsync(decimal id)
        {
            if (id <= 0)
            {
                return ServiceResult<EmployeeDetailDto>.Fail("Calisan ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<EmployeeDetailDto>.NotFound("Calisan bulunamadi.");
            }

            var scopedEmployees = ApplyEmployeeScope(_unitOfWork.Repository<Employee>().Query(), snapshot!, requireFunctionScope: true);
            var employee = await scopedEmployees
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.CompanyID,
                    x.Code,
                    x.Name,
                    x.Invisible
                })
                .SingleOrDefaultAsync();

            if (employee == null)
            {
                return ServiceResult<EmployeeDetailDto>.NotFound("Calisan bulunamadi.");
            }

            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var functionScopeQuery = ApplyFunctionScope(_unitOfWork.Repository<Function>().Query(), snapshot!, employee.CompanyID);
            var functionAssignments = await (
                from empFunc in _unitOfWork.Repository<EmpFunc>().Query()
                join function in functionScopeQuery
                    on empFunc.FunctionID equals function.Id
                join preferredName in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == languageId)
                    on function.Id equals preferredName.FunctionID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on function.Id equals fallbackName.FunctionID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                where empFunc.EmployeeID == employee.Id && empFunc.Deleted == 0
                orderby preferredName != null ? preferredName.Name : (fallbackName != null ? fallbackName.Name : string.Empty), function.Id
                select new EmployeeFunctionAssignmentDto
                {
                    FunctionId = function.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    WorkAmountUpdate = empFunc.WorkAmountUpdate
                })
                .ToListAsync();

            foreach (var assignment in functionAssignments.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                assignment.Name = assignment.FunctionId.ToString(CultureInfo.InvariantCulture);
            }

            var scopedWorkTypes = ApplyWorkTypeScope(_unitOfWork.Repository<WorkType>().Query(), snapshot!, employee.CompanyID);
            var workTypeAssignments = await (
                from empWork in _unitOfWork.Repository<EmpWork>().Query()
                join workType in scopedWorkTypes
                    on empWork.WorkTypeID equals workType.Id
                join preferredName in _unitOfWork.Repository<XWorkType>().Query().Where(x => x.LanguageID == languageId)
                    on workType.Id equals preferredName.WorkTypeID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XWorkType>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on workType.Id equals fallbackName.WorkTypeID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                where empWork.EmployeeID == employee.Id && empWork.Deleted == 0
                orderby preferredName != null ? preferredName.Name : (fallbackName != null ? fallbackName.Name : string.Empty), workType.Id
                select new EmployeeWorkTypeAssignmentDto
                {
                    WorkTypeId = workType.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    IsDefault = empWork.Default
                })
                .ToListAsync();

            foreach (var assignment in workTypeAssignments.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                assignment.Name = assignment.WorkTypeId.ToString(CultureInfo.InvariantCulture);
            }

            var scopedTimeTypes = ApplyTimeTypeScope(_unitOfWork.Repository<TimeType>().Query(), snapshot!, employee.CompanyID);
            var timeTypeAssignments = await (
                from empTime in _unitOfWork.Repository<EmpTime>().Query()
                join timeType in scopedTimeTypes
                    on empTime.TimeTypeID equals timeType.Id
                join preferredName in _unitOfWork.Repository<XTimeType>().Query().Where(x => x.LanguageID == languageId)
                    on timeType.Id equals preferredName.TimeTypeID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XTimeType>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on timeType.Id equals fallbackName.TimeTypeID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                where empTime.EmployeeID == employee.Id && empTime.Deleted == 0
                orderby preferredName != null ? preferredName.Name : (fallbackName != null ? fallbackName.Name : string.Empty), timeType.Id
                select new EmployeeTimeTypeAssignmentDto
                {
                    TimeTypeId = timeType.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    IsDefault = empTime.Default
                })
                .ToListAsync();

            foreach (var assignment in timeTypeAssignments.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                assignment.Name = assignment.TimeTypeId.ToString(CultureInfo.InvariantCulture);
            }

            return ServiceResult<EmployeeDetailDto>.Success(new EmployeeDetailDto
            {
                Id = employee.Id,
                CompanyId = employee.CompanyID,
                Code = employee.Code,
                Name = employee.Name,
                Invisible = employee.Invisible,
                Functions = functionAssignments,
                WorkTypes = workTypeAssignments,
                TimeTypes = timeTypeAssignments
            });
        }

        public async Task<ServiceResult<List<EmployeeLookupOptionDto>>> GetEmployeeFunctionOptionsAsync()
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<List<EmployeeLookupOptionDto>>.Success(new List<EmployeeLookupOptionDto>());
            }

            var options = await QueryFunctionOptionsAsync(snapshot!, requireCompanyId: true);
            return ServiceResult<List<EmployeeLookupOptionDto>>.Success(options);
        }

        public async Task<ServiceResult<List<EmployeeLookupOptionDto>>> GetEmployeeWorkTypeOptionsAsync()
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<List<EmployeeLookupOptionDto>>.Success(new List<EmployeeLookupOptionDto>());
            }

            var options = await QueryWorkTypeOptionsAsync(snapshot!, requireCompanyId: true);
            return ServiceResult<List<EmployeeLookupOptionDto>>.Success(options);
        }

        public async Task<ServiceResult<List<EmployeeLookupOptionDto>>> GetEmployeeTimeTypeOptionsAsync()
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult<List<EmployeeLookupOptionDto>>.Success(new List<EmployeeLookupOptionDto>());
            }

            var options = await QueryTimeTypeOptionsAsync(snapshot!, requireCompanyId: true);
            return ServiceResult<List<EmployeeLookupOptionDto>>.Success(options);
        }

        public async Task<ServiceResult> CreateEmployeeAsync(EmployeeUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Calisan bilgisi bos olamaz.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Calisan olusturma icin yetki kapsami bulunamadi.");
            }

            var normalizedCode = NormalizeCode(input.Code);
            var normalizedName = NormalizeName(input.Name);
            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                return ServiceResult.Fail("Kod zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return ServiceResult.Fail("Ad zorunludur.");
            }

            if (normalizedCode.Length > 8)
            {
                return ServiceResult.Fail("Kod en fazla 8 karakter olabilir.");
            }

            if (normalizedName.Length > 32)
            {
                return ServiceResult.Fail("Ad en fazla 32 karakter olabilir.");
            }

            var targetCompanyId = ResolveTargetCompanyId(snapshot!);
            if (!targetCompanyId.HasValue)
            {
                return ServiceResult.Fail("Company kapsam bilgisi olmadigi icin calisan olusturulamadi.");
            }

            var normalizedFunctions = NormalizeFunctionAssignments(input.Functions);
            var normalizedWorkTypes = NormalizeWorkTypeAssignments(input.WorkTypes);
            var normalizedTimeTypes = NormalizeTimeTypeAssignments(input.TimeTypes);

            var functionValidation = await ValidateFunctionAssignmentsAsync(normalizedFunctions, snapshot!, targetCompanyId.Value);
            if (!functionValidation.IsSuccess)
            {
                return functionValidation;
            }

            var workTypeValidation = await ValidateWorkTypeAssignmentsAsync(normalizedWorkTypes, snapshot!, targetCompanyId.Value);
            if (!workTypeValidation.IsSuccess)
            {
                return workTypeValidation;
            }

            var timeTypeValidation = await ValidateTimeTypeAssignmentsAsync(normalizedTimeTypes, snapshot!, targetCompanyId.Value);
            if (!timeTypeValidation.IsSuccess)
            {
                return timeTypeValidation;
            }

            var codeExists = await _unitOfWork.Repository<Employee>()
                .Query()
                .AnyAsync(x => x.CompanyID == targetCompanyId.Value && x.Code == normalizedCode);

            if (codeExists)
            {
                return ServiceResult.Fail("Ayni kod ile baska bir calisan kaydi mevcut.");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var employeeRepo = _unitOfWork.Repository<Employee>();
                var nextEmployeeId = (await employeeRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;

                await employeeRepo.AddAsync(new Employee
                {
                    Id = nextEmployeeId,
                    CompanyID = targetCompanyId.Value,
                    Code = normalizedCode,
                    Name = normalizedName,
                    Invisible = input.Invisible,
                    SelectFlag = false,
                    SelectQty = 0,
                    Stamp = 1
                });

                await AddEmpFuncMappingsAsync(nextEmployeeId, normalizedFunctions);
                await AddEmpWorkMappingsAsync(nextEmployeeId, normalizedWorkTypes);
                await AddEmpTimeMappingsAsync(nextEmployeeId, normalizedTimeTypes);

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Calisan kaydedildi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Calisan kaydedilirken hata olustu.");
                return ServiceResult.Fail("Calisan kaydedilemedi.");
            }
        }

        public async Task<ServiceResult> UpdateEmployeeAsync(EmployeeUpsertDto input)
        {
            if (input == null)
            {
                return ServiceResult.Fail("Calisan bilgisi bos olamaz.");
            }

            if (!input.Id.HasValue || input.Id.Value <= 0)
            {
                return ServiceResult.Fail("Calisan ID zorunludur.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Calisan guncelleme icin yetki kapsami bulunamadi.");
            }

            var normalizedCode = NormalizeCode(input.Code);
            var normalizedName = NormalizeName(input.Name);
            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                return ServiceResult.Fail("Kod zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return ServiceResult.Fail("Ad zorunludur.");
            }

            if (normalizedCode.Length > 8)
            {
                return ServiceResult.Fail("Kod en fazla 8 karakter olabilir.");
            }

            if (normalizedName.Length > 32)
            {
                return ServiceResult.Fail("Ad en fazla 32 karakter olabilir.");
            }

            var normalizedFunctions = NormalizeFunctionAssignments(input.Functions);
            var normalizedWorkTypes = NormalizeWorkTypeAssignments(input.WorkTypes);
            var normalizedTimeTypes = NormalizeTimeTypeAssignments(input.TimeTypes);

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var employeeRepo = _unitOfWork.Repository<Employee>();
                var employee = await ApplyEmployeeScope(employeeRepo.Query(), snapshot!, requireFunctionScope: true)
                    .SingleOrDefaultAsync(x => x.Id == input.Id.Value);

                if (employee == null)
                {
                    return ServiceResult.NotFound("Calisan bulunamadi.");
                }

                var functionValidation = await ValidateFunctionAssignmentsAsync(normalizedFunctions, snapshot!, employee.CompanyID);
                if (!functionValidation.IsSuccess)
                {
                    return functionValidation;
                }

                var workTypeValidation = await ValidateWorkTypeAssignmentsAsync(normalizedWorkTypes, snapshot!, employee.CompanyID);
                if (!workTypeValidation.IsSuccess)
                {
                    return workTypeValidation;
                }

                var timeTypeValidation = await ValidateTimeTypeAssignmentsAsync(normalizedTimeTypes, snapshot!, employee.CompanyID);
                if (!timeTypeValidation.IsSuccess)
                {
                    return timeTypeValidation;
                }

                var duplicateCodeExists = await employeeRepo.Query()
                    .AnyAsync(x => x.CompanyID == employee.CompanyID && x.Code == normalizedCode && x.Id != employee.Id);

                if (duplicateCodeExists)
                {
                    return ServiceResult.Fail("Ayni kod ile baska bir calisan kaydi mevcut.");
                }

                employee.Code = normalizedCode;
                employee.Name = normalizedName;
                employee.Invisible = input.Invisible;
                employee.Stamp = 1;
                employeeRepo.Update(employee);

                await SoftDeleteEmpFuncMappingsAsync(employee.Id);
                await SoftDeleteEmpWorkMappingsAsync(employee.Id);
                await SoftDeleteEmpTimeMappingsAsync(employee.Id);

                await AddEmpFuncMappingsAsync(employee.Id, normalizedFunctions);
                await AddEmpWorkMappingsAsync(employee.Id, normalizedWorkTypes);
                await AddEmpTimeMappingsAsync(employee.Id, normalizedTimeTypes);

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Calisan guncellendi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Calisan guncellenirken hata olustu. EmployeeID={EmployeeId}", input.Id.Value);
                return ServiceResult.Fail("Calisan guncellenemedi.");
            }
        }

        private async Task<List<EmployeeLookupOptionDto>> QueryFunctionOptionsAsync(PermissionSnapshotDto snapshot, bool requireCompanyId)
        {
            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var functionQuery = ApplyFunctionScope(_unitOfWork.Repository<Function>().Query(), snapshot, companyId: null, requireCompanyId: requireCompanyId);

            var options = await (
                from function in functionQuery
                join preferredName in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == languageId)
                    on function.Id equals preferredName.FunctionID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XFunction>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on function.Id equals fallbackName.FunctionID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                select new EmployeeLookupOptionDto
                {
                    Id = function.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    Invisible = function.Invisible
                })
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToListAsync();

            foreach (var option in options.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                option.Name = option.Id.ToString(CultureInfo.InvariantCulture);
            }

            return options;
        }

        private async Task<List<EmployeeLookupOptionDto>> QueryWorkTypeOptionsAsync(PermissionSnapshotDto snapshot, bool requireCompanyId)
        {
            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var workTypeQuery = ApplyWorkTypeScope(_unitOfWork.Repository<WorkType>().Query(), snapshot, companyId: null, requireCompanyId: requireCompanyId);

            var options = await (
                from workType in workTypeQuery
                join preferredName in _unitOfWork.Repository<XWorkType>().Query().Where(x => x.LanguageID == languageId)
                    on workType.Id equals preferredName.WorkTypeID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XWorkType>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on workType.Id equals fallbackName.WorkTypeID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                select new EmployeeLookupOptionDto
                {
                    Id = workType.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    Invisible = workType.Invisible
                })
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToListAsync();

            foreach (var option in options.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                option.Name = option.Id.ToString(CultureInfo.InvariantCulture);
            }

            return options;
        }

        private async Task<List<EmployeeLookupOptionDto>> QueryTimeTypeOptionsAsync(PermissionSnapshotDto snapshot, bool requireCompanyId)
        {
            var languageId = ResolveUiLanguageId();
            var fallbackLanguageId = 1m;

            var timeTypeQuery = ApplyTimeTypeScope(_unitOfWork.Repository<TimeType>().Query(), snapshot, companyId: null, requireCompanyId: requireCompanyId);

            var options = await (
                from timeType in timeTypeQuery
                join preferredName in _unitOfWork.Repository<XTimeType>().Query().Where(x => x.LanguageID == languageId)
                    on timeType.Id equals preferredName.TimeTypeID into preferredNameGroup
                from preferredName in preferredNameGroup.DefaultIfEmpty()
                join fallbackName in _unitOfWork.Repository<XTimeType>().Query().Where(x => x.LanguageID == fallbackLanguageId)
                    on timeType.Id equals fallbackName.TimeTypeID into fallbackNameGroup
                from fallbackName in fallbackNameGroup.DefaultIfEmpty()
                select new EmployeeLookupOptionDto
                {
                    Id = timeType.Id,
                    Name = preferredName != null
                        ? preferredName.Name
                        : (fallbackName != null ? fallbackName.Name : string.Empty),
                    Invisible = timeType.Invisible
                })
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToListAsync();

            foreach (var option in options.Where(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                option.Name = option.Id.ToString(CultureInfo.InvariantCulture);
            }

            return options;
        }

        private async Task<ServiceResult> ValidateFunctionAssignmentsAsync(
            IReadOnlyCollection<EmployeeFunctionAssignmentInputDto> assignments,
            PermissionSnapshotDto snapshot,
            decimal companyId)
        {
            if (assignments.Count == 0)
            {
                return ServiceResult.Success();
            }

            var functionIds = assignments.Select(x => x.FunctionId).Distinct().ToList();
            var existingFunctionIds = await ApplyFunctionScope(
                    _unitOfWork.Repository<Function>().Query(),
                    snapshot,
                    companyId,
                    requireCompanyId: true)
                .Where(x => functionIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            if (existingFunctionIds.Count != functionIds.Count)
            {
                return ServiceResult.Fail("Secilen fonksiyonlardan en az biri yetki kapsaminda degil.");
            }

            return ServiceResult.Success();
        }

        private async Task<ServiceResult> ValidateWorkTypeAssignmentsAsync(
            IReadOnlyCollection<EmployeeWorkTypeAssignmentInputDto> assignments,
            PermissionSnapshotDto snapshot,
            decimal companyId)
        {
            if (assignments.Count == 0)
            {
                return ServiceResult.Success();
            }

            var workTypeIds = assignments.Select(x => x.WorkTypeId).Distinct().ToList();
            var existingWorkTypeIds = await ApplyWorkTypeScope(
                    _unitOfWork.Repository<WorkType>().Query(),
                    snapshot,
                    companyId,
                    requireCompanyId: true)
                .Where(x => workTypeIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            if (existingWorkTypeIds.Count != workTypeIds.Count)
            {
                return ServiceResult.Fail("Secilen gorev tiplerinden en az biri company kapsaminda degil.");
            }

            return ServiceResult.Success();
        }

        private async Task<ServiceResult> ValidateTimeTypeAssignmentsAsync(
            IReadOnlyCollection<EmployeeTimeTypeAssignmentInputDto> assignments,
            PermissionSnapshotDto snapshot,
            decimal companyId)
        {
            if (assignments.Count == 0)
            {
                return ServiceResult.Success();
            }

            var timeTypeIds = assignments.Select(x => x.TimeTypeId).Distinct().ToList();
            var existingTimeTypeIds = await ApplyTimeTypeScope(
                    _unitOfWork.Repository<TimeType>().Query(),
                    snapshot,
                    companyId,
                    requireCompanyId: true)
                .Where(x => timeTypeIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            if (existingTimeTypeIds.Count != timeTypeIds.Count)
            {
                return ServiceResult.Fail("Secilen mesai tiplerinden en az biri company kapsaminda degil.");
            }

            return ServiceResult.Success();
        }

        private async Task AddEmpFuncMappingsAsync(decimal employeeId, IReadOnlyCollection<EmployeeFunctionAssignmentInputDto> assignments)
        {
            if (assignments.Count == 0)
            {
                return;
            }

            var repository = _unitOfWork.Repository<EmpFunc>();
            var nextId = (await repository.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;

            foreach (var assignment in assignments)
            {
                await repository.AddAsync(new EmpFunc
                {
                    Id = nextId++,
                    EmployeeID = employeeId,
                    FunctionID = assignment.FunctionId,
                    WorkAmountUpdate = assignment.WorkAmountUpdate,
                    Deleted = 0,
                    SelectFlag = false,
                    Stamp = 1
                });
            }
        }

        private async Task AddEmpWorkMappingsAsync(decimal employeeId, IReadOnlyCollection<EmployeeWorkTypeAssignmentInputDto> assignments)
        {
            if (assignments.Count == 0)
            {
                return;
            }

            var repository = _unitOfWork.Repository<EmpWork>();
            var nextId = (await repository.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;

            foreach (var assignment in assignments)
            {
                await repository.AddAsync(new EmpWork
                {
                    Id = nextId++,
                    EmployeeID = employeeId,
                    WorkTypeID = assignment.WorkTypeId,
                    Default = assignment.IsDefault,
                    Deleted = 0,
                    SelectFlag = false,
                    Stamp = 1
                });
            }
        }

        private async Task AddEmpTimeMappingsAsync(decimal employeeId, IReadOnlyCollection<EmployeeTimeTypeAssignmentInputDto> assignments)
        {
            if (assignments.Count == 0)
            {
                return;
            }

            var repository = _unitOfWork.Repository<EmpTime>();
            var nextId = (await repository.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;

            foreach (var assignment in assignments)
            {
                await repository.AddAsync(new EmpTime
                {
                    Id = nextId++,
                    EmployeeID = employeeId,
                    TimeTypeID = assignment.TimeTypeId,
                    Default = assignment.IsDefault,
                    Deleted = 0,
                    SelectFlag = false,
                    Stamp = 1
                });
            }
        }

        private async Task SoftDeleteEmpFuncMappingsAsync(decimal employeeId)
        {
            var repository = _unitOfWork.Repository<EmpFunc>();
            var rows = await repository.Query()
                .Where(x => x.EmployeeID == employeeId && x.Deleted == 0)
                .ToListAsync();

            foreach (var row in rows)
            {
                row.Deleted = 1;
                row.SelectFlag = false;
                row.Stamp = 1;
                repository.Update(row);
            }
        }

        private async Task SoftDeleteEmpWorkMappingsAsync(decimal employeeId)
        {
            var repository = _unitOfWork.Repository<EmpWork>();
            var rows = await repository.Query()
                .Where(x => x.EmployeeID == employeeId && x.Deleted == 0)
                .ToListAsync();

            foreach (var row in rows)
            {
                row.Deleted = 1;
                row.SelectFlag = false;
                row.Stamp = 1;
                repository.Update(row);
            }
        }

        private async Task SoftDeleteEmpTimeMappingsAsync(decimal employeeId)
        {
            var repository = _unitOfWork.Repository<EmpTime>();
            var rows = await repository.Query()
                .Where(x => x.EmployeeID == employeeId && x.Deleted == 0)
                .ToListAsync();

            foreach (var row in rows)
            {
                row.Deleted = 1;
                row.SelectFlag = false;
                row.Stamp = 1;
                repository.Update(row);
            }
        }

        private static List<EmployeeFunctionAssignmentInputDto> NormalizeFunctionAssignments(IEnumerable<EmployeeFunctionAssignmentInputDto>? input)
        {
            return input?
                .Where(x => x.FunctionId > 0)
                .GroupBy(x => x.FunctionId)
                .Select(x => new EmployeeFunctionAssignmentInputDto
                {
                    FunctionId = x.Key,
                    WorkAmountUpdate = x.Any(y => y.WorkAmountUpdate)
                })
                .OrderBy(x => x.FunctionId)
                .ToList()
                ?? new List<EmployeeFunctionAssignmentInputDto>();
        }

        private static List<EmployeeWorkTypeAssignmentInputDto> NormalizeWorkTypeAssignments(IEnumerable<EmployeeWorkTypeAssignmentInputDto>? input)
        {
            return input?
                .Where(x => x.WorkTypeId > 0)
                .GroupBy(x => x.WorkTypeId)
                .Select(x => new EmployeeWorkTypeAssignmentInputDto
                {
                    WorkTypeId = x.Key,
                    IsDefault = x.Any(y => y.IsDefault)
                })
                .OrderBy(x => x.WorkTypeId)
                .ToList()
                ?? new List<EmployeeWorkTypeAssignmentInputDto>();
        }

        private static List<EmployeeTimeTypeAssignmentInputDto> NormalizeTimeTypeAssignments(IEnumerable<EmployeeTimeTypeAssignmentInputDto>? input)
        {
            return input?
                .Where(x => x.TimeTypeId > 0)
                .GroupBy(x => x.TimeTypeId)
                .Select(x => new EmployeeTimeTypeAssignmentInputDto
                {
                    TimeTypeId = x.Key,
                    IsDefault = x.Any(y => y.IsDefault)
                })
                .OrderBy(x => x.TimeTypeId)
                .ToList()
                ?? new List<EmployeeTimeTypeAssignmentInputDto>();
        }

        private static string NormalizeCode(string? code)
        {
            return (code ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string NormalizeName(string? name)
        {
            return (name ?? string.Empty).Trim();
        }

        private static decimal? ResolveTargetCompanyId(PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound)
            {
                return snapshot.CompanyId;
            }

            return snapshot.CompanyId;
        }

        private static IQueryable<Employee> ApplyEmployeeScope(
            IQueryable<Employee> query,
            PermissionSnapshotDto snapshot,
            bool requireFunctionScope)
        {
            query = ApplyEmployeeCompanyScope(query, snapshot, companyId: null, requireCompanyId: false);

            if (!snapshot.EmployeeScopeBypass)
            {
                if (snapshot.AllowedEmployeeIds.Count == 0)
                {
                    return query.Where(_ => false);
                }

                query = query.Where(x => snapshot.AllowedEmployeeIds.Contains(x.Id));
            }

            if (requireFunctionScope)
            {
                if (snapshot.AllowedFunctionIds.Count == 0)
                {
                    return query.Where(_ => false);
                }

                query = query.Where(x => x.EmpFuncs.Any(ef => ef.Deleted == 0 && snapshot.AllowedFunctionIds.Contains(ef.FunctionID)));
            }

            return query;
        }

        private static IQueryable<Employee> ApplyEmployeeCompanyScope(
            IQueryable<Employee> query,
            PermissionSnapshotDto snapshot,
            decimal? companyId,
            bool requireCompanyId)
        {
            var targetCompanyId = companyId ?? snapshot.CompanyId;

            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound || requireCompanyId)
            {
                if (!targetCompanyId.HasValue)
                {
                    return query.Where(_ => false);
                }

                query = query.Where(x => x.CompanyID == targetCompanyId.Value);
            }

            return query;
        }

        private static IQueryable<Function> ApplyFunctionScope(
            IQueryable<Function> query,
            PermissionSnapshotDto snapshot,
            decimal? companyId,
            bool requireCompanyId = false)
        {
            var targetCompanyId = companyId ?? snapshot.CompanyId;

            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound || requireCompanyId)
            {
                if (!targetCompanyId.HasValue)
                {
                    return query.Where(_ => false);
                }

                query = query.Where(x => x.CompanyID == targetCompanyId.Value);
            }

            if (snapshot.AllowedFunctionIds.Count == 0)
            {
                return query.Where(_ => false);
            }

            return query.Where(x => snapshot.AllowedFunctionIds.Contains(x.Id));
        }

        private static IQueryable<WorkType> ApplyWorkTypeScope(
            IQueryable<WorkType> query,
            PermissionSnapshotDto snapshot,
            decimal? companyId,
            bool requireCompanyId = false)
        {
            var targetCompanyId = companyId ?? snapshot.CompanyId;

            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound || requireCompanyId)
            {
                if (!targetCompanyId.HasValue)
                {
                    return query.Where(_ => false);
                }

                query = query.Where(x => x.CompanyID == targetCompanyId.Value);
            }

            return query;
        }

        private static IQueryable<TimeType> ApplyTimeTypeScope(
            IQueryable<TimeType> query,
            PermissionSnapshotDto snapshot,
            decimal? companyId,
            bool requireCompanyId = false)
        {
            var targetCompanyId = companyId ?? snapshot.CompanyId;

            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound || requireCompanyId)
            {
                if (!targetCompanyId.HasValue)
                {
                    return query.Where(_ => false);
                }

                query = query.Where(x => x.CompanyID == targetCompanyId.Value);
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
