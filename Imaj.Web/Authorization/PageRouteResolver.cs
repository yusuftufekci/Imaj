using System;
using System.Collections.Generic;
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

        private static readonly Dictionary<string, string> ControllerPageMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Home"] = "Home.asp",
            ["Culture"] = "Home.asp",
            ["Customer"] = "CustomerQry.asp",
            ["Invoice"] = "InvoiceQry.asp",
            ["Job"] = "JobQry.asp",
            ["OvertimeReport"] = "JobWorkReport.asp",
            ["ProductReport"] = "JobProdReport.asp",
            ["Product"] = "JobQry.asp",
            ["Employee"] = "JobQry.asp"
        };

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

            if (string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase))
            {
                return new PageRouteMatch
                {
                    IsMapped = false,
                    MatchStatus = "Bypass-Home",
                    Reason = "Home controller tüm authenticated kullanıcılar için açık."
                };
            }

            if (string.Equals(controller, "Culture", StringComparison.OrdinalIgnoreCase))
            {
                return new PageRouteMatch
                {
                    IsMapped = false,
                    MatchStatus = "Bypass-Culture",
                    Reason = "Culture controller tüm authenticated kullanıcılar için açık."
                };
            }

            if (!ControllerPageMap.TryGetValue(controller, out var aspPage))
            {
                return new PageRouteMatch
                {
                    IsMapped = false,
                    MatchStatus = "NoRouteMap",
                    Reason = $"Controller '{controller}' için ASP page map bulunamadı."
                };
            }

            var baseIntfMap = await GetBaseIntfMapAsync();
            if (!baseIntfMap.TryGetValue(aspPage, out var baseIntfId))
            {
                return new PageRouteMatch
                {
                    IsMapped = false,
                    MatchStatus = "BaseIntfMissing",
                    AspPage = aspPage,
                    Reason = $"BaseIntf kaydı bulunamadı: {aspPage}"
                };
            }

            return new PageRouteMatch
            {
                IsMapped = true,
                MatchStatus = "Mapped",
                AspPage = aspPage,
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
