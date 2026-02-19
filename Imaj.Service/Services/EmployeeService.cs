using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Service.Results;
using Microsoft.EntityFrameworkCore;

namespace Imaj.Service.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentPermissionContext _currentPermissionContext;

        public EmployeeService(IUnitOfWork unitOfWork, ICurrentPermissionContext currentPermissionContext)
        {
            _unitOfWork = unitOfWork;
            _currentPermissionContext = currentPermissionContext;
        }

        public async Task<ServiceResult<PagedResultDto<EmployeeDto>>> GetEmployeesAsync(EmployeeFilterDto filter)
        {
            var query = _unitOfWork.Repository<Employee>().Query();
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();

            if (snapshot == null || snapshot.IsDenied || snapshot.CompanyScopeMode == DTOs.Security.CompanyScopeMode.Deny)
            {
                return ServiceResult<PagedResultDto<EmployeeDto>>.Success(new PagedResultDto<EmployeeDto>
                {
                    Items = new List<EmployeeDto>(),
                    TotalCount = 0,
                    Page = filter.Page,
                    PageSize = filter.PageSize
                });
            }

            if (snapshot.CompanyScopeMode == DTOs.Security.CompanyScopeMode.CompanyBound && snapshot.CompanyId.HasValue)
            {
                query = query.Where(x => x.CompanyID == snapshot.CompanyId.Value);
            }

            if (!snapshot.EmployeeScopeBypass)
            {
                if (snapshot.AllowedEmployeeIds.Count == 0)
                {
                    return ServiceResult<PagedResultDto<EmployeeDto>>.Success(new PagedResultDto<EmployeeDto>
                    {
                        Items = new List<EmployeeDto>(),
                        TotalCount = 0,
                        Page = filter.Page,
                        PageSize = filter.PageSize
                    });
                }

                query = query.Where(x => snapshot.AllowedEmployeeIds.Contains(x.Id));
            }

            if (snapshot.AllowedFunctionIds.Count == 0)
            {
                return ServiceResult<PagedResultDto<EmployeeDto>>.Success(new PagedResultDto<EmployeeDto>
                {
                    Items = new List<EmployeeDto>(),
                    TotalCount = 0,
                    Page = filter.Page,
                    PageSize = filter.PageSize
                });
            }

            query = query.Where(x => x.EmpFuncs.Any(ef => ef.Deleted == 0 && snapshot.AllowedFunctionIds.Contains(ef.FunctionID)));

            // Status Filter: 0=All, 1=Valid (Invisible=false), 2=Invalid (Invisible=true)
            if (filter.Status == 1)
            {
                query = query.Where(x => !x.Invisible);
            }
            else if (filter.Status == 2)
            {
                query = query.Where(x => x.Invisible);
            }

            // Code Filter
            if (!string.IsNullOrWhiteSpace(filter.Code))
            {
                query = query.Where(x => x.Code.Contains(filter.Code));
            }

            // Name Filter
            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                query = query.Where(x => x.Name.Contains(filter.Name));
            }

            // Function Filter
            if (filter.FunctionID.HasValue && filter.FunctionID.Value > 0)
            {
                query = query.Where(x => x.EmpFuncs.Any(ef => ef.FunctionID == filter.FunctionID.Value));
            }

            // Total Count
            var totalCount = await query.CountAsync();

            // Paging
            var items = await query
                .OrderBy(x => x.Code) // Default sorting by Code
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new EmployeeDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Invisible = x.Invisible
                })
                .ToListAsync();

            return ServiceResult<PagedResultDto<EmployeeDto>>.Success(new PagedResultDto<EmployeeDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ServiceResult<List<FunctionDto>>> GetFunctionsAsync()
        {
            // TODO: LanguageID parametrik olmalı (User'ın diline göre)
            var languageId = 1; 
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();

            if (snapshot == null || snapshot.IsDenied || snapshot.AllowedFunctionIds.Count == 0)
            {
                return ServiceResult<List<FunctionDto>>.Success(new List<FunctionDto>());
            }

            var functions = await _unitOfWork.Repository<XFunction>().Query()
                .Where(x => x.LanguageID == languageId && snapshot.AllowedFunctionIds.Contains(x.FunctionID))
                .OrderBy(x => x.Name)
                .Select(x => new FunctionDto
                {
                    Id = x.FunctionID,
                    Name = x.Name
                })
                .ToListAsync();

            return ServiceResult<List<FunctionDto>>.Success(functions);
        }
    }
}
