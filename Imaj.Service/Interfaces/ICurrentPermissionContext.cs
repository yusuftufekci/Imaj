using System.Threading.Tasks;
using Imaj.Service.DTOs.Security;

namespace Imaj.Service.Interfaces
{
    public interface ICurrentPermissionContext
    {
        bool TryGetCurrentUserId(out decimal userId);
        Task<PermissionSnapshotDto?> GetSnapshotAsync(bool forceRefresh = false);
    }
}
