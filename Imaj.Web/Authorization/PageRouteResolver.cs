using System;
using System.Linq;
using System.Threading.Tasks;
using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Imaj.Web.Authorization
{
    public class PageRouteResolver : IPageRouteResolver
    {
        private const string BaseIntfMapCacheKey = "authz:route:baseintf-map:v1";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _memoryCache;

        public PageRouteResolver(IUnitOfWork unitOfWork, IMemoryCache memoryCache)
        {
            _unitOfWork = unitOfWork;
            _memoryCache = memoryCache;
        }

        public async Task<PageRouteMatch> ResolveAsync(string controller, string action)
        {
            if (string.Equals(controller, "Auth", StringComparison.OrdinalIgnoreCase))
            {
                return new PageRouteMatch
                {
                    IsMapped = false,
                    MatchStatus = "Bypass-SecurityEndpoint",
                    Reason = "Auth controller route guard dışında."
                };
            }

            if (string.Equals(controller, "User", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(action, "ChangePassword", StringComparison.OrdinalIgnoreCase))
            {
                return new PageRouteMatch
                {
                    IsMapped = false,
                    MatchStatus = "Bypass-PasswordChange",
                    AspPage = "Password.asp",
                    Reason = "Password change sayfasi method yetkisi ile korunuyor."
                };
            }

            if (string.Equals(controller, "Employee", StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(action, "Search", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(action, "GetFunctions", StringComparison.OrdinalIgnoreCase)))
            {
                return new PageRouteMatch
                {
                    IsMapped = false,
                    MatchStatus = "Bypass-EmployeeLookupApi",
                    AspPage = "EmployeeLookupApi",
                    Reason = "Employee lookup API ortak kullanim icin data-scope ile korunuyor."
                };
            }

            if (!LegacyPageCatalog.TryGetControllerRoute(controller, out var route))
            {
                return new PageRouteMatch
                {
                    IsMapped = false,
                    MatchStatus = "NoRouteMap",
                    Reason = $"Controller '{controller}' için ASP page map bulunamadı."
                };
            }

            if (route.AlwaysAllowAuthenticated)
            {
                return new PageRouteMatch
                {
                    IsMapped = false,
                    MatchStatus = route.BypassMatchStatus ?? "Bypass-AlwaysOpen",
                    AspPage = route.AspPage,
                    Reason = route.BypassReason ?? "Controller tum authenticated kullanicilar icin acik."
                };
            }

            var baseIntfMap = await GetBaseIntfMapAsync();
            if (!baseIntfMap.TryGetValue(route.AspPage, out var baseIntfId))
            {
                return new PageRouteMatch
                {
                    IsMapped = false,
                    MatchStatus = "BaseIntfMissing",
                    AspPage = route.AspPage,
                    Reason = $"BaseIntf kaydı bulunamadı: {route.AspPage}"
                };
            }

            return new PageRouteMatch
            {
                IsMapped = true,
                MatchStatus = "Mapped",
                AspPage = route.AspPage,
                BaseIntfId = baseIntfId
            };
        }

        private async Task<IReadOnlyDictionary<string, decimal>> GetBaseIntfMapAsync()
        {
            if (_memoryCache.TryGetValue(BaseIntfMapCacheKey, out Dictionary<string, decimal>? cachedMap) && cachedMap != null)
            {
                return cachedMap;
            }

            var rows = await _unitOfWork.Repository<BaseIntf>()
                .Query()
                .Select(x => new { x.Id, x.Name })
                .ToListAsync();

            var map = rows
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First().Id, StringComparer.OrdinalIgnoreCase);

            _memoryCache.Set(BaseIntfMapCacheKey, map, TimeSpan.FromMinutes(30));
            return map;
        }
    }
}
