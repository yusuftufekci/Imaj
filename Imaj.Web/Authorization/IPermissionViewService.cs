using System.Collections.Generic;
using System.Threading.Tasks;
using Imaj.Service.DTOs.Security;
using Imaj.Web.Models.Authorization;

namespace Imaj.Web.Authorization
{
    public interface IPermissionViewService
    {
        Task<PermissionSnapshotDto?> GetSnapshotAsync();
        Task<bool> CanAccessAspPageAsync(string aspPage);
        Task<bool> CanExecuteMethodAsync(decimal baseMethId, bool write = false);
        Task<bool> CanReadPropAsync(decimal basePropId);
        Task<bool> CanWritePropAsync(decimal basePropId);
        Task<IReadOnlyList<VisibleMenuItem>> GetVisibleMenuItemsAsync();
    }
}
