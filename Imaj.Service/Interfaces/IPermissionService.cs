using System.Threading.Tasks;
using Imaj.Service.DTOs.Security;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface IPermissionService
    {
        Task<ServiceResult<PermissionSnapshotDto>> GetOrBuildPermissionSetAsync(decimal userId, bool forceRefresh = false);
        Task InvalidateAsync(decimal userId);
    }
}
