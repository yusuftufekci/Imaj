using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Imaj.Service.DTOs.Security;
using Imaj.Service.Interfaces;
using Imaj.Web.Models.Authorization;
using Microsoft.Extensions.Logging;

namespace Imaj.Web.Authorization
{
    public class PermissionViewService : IPermissionViewService
    {
        private const string HomeAspPage = "Home.asp";

        private static readonly IReadOnlyList<VisibleMenuItem> MenuDefinitions = new List<VisibleMenuItem>
        {
            new() { Key = "Home", Label = "Ana Sayfa", Url = "/", AspPage = HomeAspPage },
            new() { Key = "Customer", Label = "Müşteri", Url = "/Customer", AspPage = "CustomerQry.asp" },
            new() { Key = "Job", Label = "İş", Url = "/Job", AspPage = "JobQry.asp" },
            new() { Key = "Invoice", Label = "Fatura", Url = "/Invoice", AspPage = "InvoiceQry.asp" },
            new() { Key = "OvertimeReport", Label = "Mesai Raporu", Url = "/OvertimeReport", AspPage = "JobWorkReport.asp" },
            new() { Key = "ProductReport", Label = "Ürün Raporu", Url = "/ProductReport", AspPage = "JobProdReport.asp" }
        };

        private readonly ICurrentPermissionContext _currentPermissionContext;
        private readonly ILogger<PermissionViewService> _logger;

        public PermissionViewService(
            ICurrentPermissionContext currentPermissionContext,
            ILogger<PermissionViewService> logger)
        {
            _currentPermissionContext = currentPermissionContext;
            _logger = logger;
        }

        public Task<PermissionSnapshotDto?> GetSnapshotAsync()
        {
            return _currentPermissionContext.GetSnapshotAsync();
        }

        public async Task<bool> CanAccessAspPageAsync(string aspPage)
        {
            if (IsAlwaysOpenAspPage(aspPage))
            {
                return true;
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (snapshot == null || snapshot.IsDenied)
            {
                return false;
            }

            if (snapshot.HasAllMenu)
            {
                return true;
            }

            return snapshot.AllowedPages.Values.Any(x => string.Equals(x, aspPage, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> CanExecuteMethodAsync(decimal baseMethId, bool write = false)
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (snapshot == null || snapshot.IsDenied)
            {
                return false;
            }

            return snapshot.CanExecuteMethod(baseMethId, write);
        }

        public async Task<bool> CanReadPropAsync(decimal basePropId)
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (snapshot == null || snapshot.IsDenied)
            {
                return false;
            }

            return snapshot.CanReadProperty(basePropId);
        }

        public async Task<bool> CanWritePropAsync(decimal basePropId)
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (snapshot == null || snapshot.IsDenied)
            {
                return false;
            }

            return snapshot.CanWriteProperty(basePropId);
        }

        public async Task<IReadOnlyList<VisibleMenuItem>> GetVisibleMenuItemsAsync()
        {
            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (snapshot == null || snapshot.IsDenied)
            {
                return MenuDefinitions.Where(x => IsAlwaysOpenAspPage(x.AspPage)).ToList();
            }

            if (snapshot.HasAllMenu)
            {
                return MenuDefinitions;
            }

            var allowedPages = snapshot.AllowedPages.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var visible = MenuDefinitions
                .Where(x => IsAlwaysOpenAspPage(x.AspPage) || allowedPages.Contains(x.AspPage))
                .ToList();

            _logger.LogDebug("Menu guard computed. UserID={UserId}, VisibleMenuCount={Count}", snapshot.UserId, visible.Count);
            return visible;
        }

        private static bool IsAlwaysOpenAspPage(string aspPage)
        {
            return string.Equals(aspPage, HomeAspPage, StringComparison.OrdinalIgnoreCase);
        }
    }
}
