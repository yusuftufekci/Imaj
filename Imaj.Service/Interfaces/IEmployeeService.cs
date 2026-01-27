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
    }
}
