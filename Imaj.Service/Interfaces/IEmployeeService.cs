using System.Collections.Generic;
using System.Threading.Tasks;
using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface IEmployeeService
    {
        Task<ServiceResult<PagedResultDto<EmployeeDto>>> GetEmployeesAsync(EmployeeFilterDto filter);
        Task<ServiceResult<List<FunctionDto>>> GetFunctionsAsync();
        Task<ServiceResult<EmployeeDetailDto>> GetEmployeeDetailAsync(decimal id);
        Task<ServiceResult<List<EmployeeLookupOptionDto>>> GetEmployeeFunctionOptionsAsync();
        Task<ServiceResult<List<EmployeeLookupOptionDto>>> GetEmployeeWorkTypeOptionsAsync();
        Task<ServiceResult<List<EmployeeLookupOptionDto>>> GetEmployeeTimeTypeOptionsAsync();
        Task<ServiceResult> CreateEmployeeAsync(EmployeeUpsertDto input);
        Task<ServiceResult> UpdateEmployeeAsync(EmployeeUpsertDto input);
    }
}
