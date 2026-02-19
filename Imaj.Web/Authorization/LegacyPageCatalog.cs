using System;
using System.Collections.Generic;
using System.Linq;

namespace Imaj.Web.Authorization
{
    public static class LegacyPageCatalog
    {
        private static readonly IReadOnlyList<LegacyPageDefinition> AllPages = new List<LegacyPageDefinition>
        {
            new()
            {
                Key = "Home",
                Label = "Ana Sayfa",
                AspPage = "Home.asp",
                Url = "/",
                Controller = "Home",
                Action = "Index",
                IncludeInMenu = true,
                IsImplemented = true,
                AlwaysAllowAuthenticated = true,
                BypassMatchStatus = "Bypass-Home",
                BypassReason = "Home controller tum authenticated kullanicilar icin acik."
            },
            new()
            {
                Key = "Culture",
                Label = "Culture",
                AspPage = "Home.asp",
                Url = "/Culture/SetLanguage",
                Controller = "Culture",
                Action = "SetLanguage",
                IncludeInMenu = false,
                IsImplemented = true,
                AlwaysAllowAuthenticated = true,
                BypassMatchStatus = "Bypass-Culture",
                BypassReason = "Culture controller tum authenticated kullanicilar icin acik."
            },

            // Implemented menu items (current MVC coverage)
            new() { Key = "User", Label = "Kullanici", AspPage = "UserQry.asp", Url = "/User", Controller = "User", Action = "Index", IncludeInMenu = true, IsImplemented = true },
            new() { Key = "Customer", Label = "Musteri", AspPage = "CustomerQry.asp", Url = "/Customer", Controller = "Customer", Action = "Index", IncludeInMenu = true, IsImplemented = true },
            new() { Key = "Job", Label = "Is", AspPage = "JobQry.asp", Url = "/Job", Controller = "Job", Action = "Index", IncludeInMenu = true, IsImplemented = true },
            new() { Key = "Invoice", Label = "Fatura", AspPage = "InvoiceQry.asp", Url = "/Invoice", Controller = "Invoice", Action = "Index", IncludeInMenu = true, IsImplemented = true },
            new() { Key = "OvertimeReport", Label = "Mesai Raporu", AspPage = "JobWorkReport.asp", Url = "/OvertimeReport", Controller = "OvertimeReport", Action = "Index", IncludeInMenu = true, IsImplemented = true },
            new() { Key = "ProductReport", Label = "Urun Raporu", AspPage = "JobProdReport.asp", Url = "/ProductReport", Controller = "ProductReport", Action = "Index", IncludeInMenu = true, IsImplemented = true },
            new() { Key = "ResoCat", Label = "Kaynak Kategorisi", AspPage = "ResoCatQry.asp", Url = "/ResoCat", Controller = "ResoCat", Action = "Index", IncludeInMenu = true, IsImplemented = true },
            new() { Key = "Function", Label = "Fonksiyon", AspPage = "FunctionQry.asp", Url = "/Function", Controller = "Function", Action = "Index", IncludeInMenu = true, IsImplemented = true },
            new() { Key = "Resource", Label = "Kaynak", AspPage = "ResourceQry.asp", Url = "/Resource", Controller = "Resource", Action = "Index", IncludeInMenu = true, IsImplemented = true },
            new() { Key = "Reason", Label = "Gerekce", AspPage = "ReasonQry.asp", Url = "/Reason", Controller = "Reason", Action = "Index", IncludeInMenu = true, IsImplemented = true },

            // Temporary API aliases to preserve current behavior until dedicated pages are implemented.
            new() { Key = "ProductApiAlias", Label = "Product API Alias", AspPage = "JobQry.asp", Url = string.Empty, Controller = "Product", Action = "Search", IncludeInMenu = false, IsImplemented = true },
            new() { Key = "EmployeeApiAlias", Label = "Employee API Alias", AspPage = "JobQry.asp", Url = string.Empty, Controller = "Employee", Action = "Search", IncludeInMenu = false, IsImplemented = true },

            // Planned legacy menu items (not implemented yet)
            new() { Key = "ReserveCrossTab", Label = "Takvim", AspPage = "ReserveCrossTab.asp", Url = "/ReserveCalendar", IncludeInMenu = true, IsImplemented = false, PlannedController = "ReserveCalendar", PlannedAction = "Index" },
            new() { Key = "Absence", Label = "Mazeret", AspPage = "AbsenceQry.asp", Url = "/Absence", Controller = "Absence", Action = "Index", IncludeInMenu = true, IsImplemented = true },
            new() { Key = "Reserve", Label = "Reservasyon", AspPage = "ReserveQry.asp", Url = "/Reserve", IncludeInMenu = true, IsImplemented = false, PlannedController = "Reserve", PlannedAction = "Index" },
            new() { Key = "WorkType", Label = "Gorev Tipi", AspPage = "WorkTypeQry.asp", Url = "/WorkType", Controller = "WorkType", Action = "Index", IncludeInMenu = true, IsImplemented = true },
            new() { Key = "TimeType", Label = "Mesai Tipi", AspPage = "TimeTypeQry.asp", Url = "/TimeType", Controller = "TimeType", Action = "Index", IncludeInMenu = true, IsImplemented = true },
            new() { Key = "Employee", Label = "Calisan", AspPage = "EmployeeQry.asp", Url = "/Employee", Controller = "EmployeePage", Action = "Index", IncludeInMenu = true, IsImplemented = true },
            new() { Key = "TaxType", Label = "Vergi Tipi", AspPage = "TaxTypeQry.asp", Url = "/TaxType", IncludeInMenu = true, IsImplemented = false, PlannedController = "TaxType", PlannedAction = "Index" },
            new() { Key = "ProdCat", Label = "Urun Kategorisi", AspPage = "ProdCatQry.asp", Url = "/ProdCat", IncludeInMenu = true, IsImplemented = false, PlannedController = "ProdCat", PlannedAction = "Index" },
            new() { Key = "ProdGrp", Label = "Urun Grubu", AspPage = "ProdGrpQry.asp", Url = "/ProdGrp", IncludeInMenu = true, IsImplemented = false, PlannedController = "ProdGrp", PlannedAction = "Index" },
            new() { Key = "Product", Label = "Urun", AspPage = "ProductQry.asp", Url = "/Product", IncludeInMenu = true, IsImplemented = false, PlannedController = "Product", PlannedAction = "Index" },
            new() { Key = "JobEntry", Label = "Is Girisi", AspPage = "JobEntryQry.asp", Url = "/JobEntry", IncludeInMenu = true, IsImplemented = false, PlannedController = "JobEntry", PlannedAction = "Index" }
        };

        private static readonly IReadOnlyDictionary<string, LegacyPageDefinition> ControllerRouteMap = AllPages
            .Where(x => x.IsImplemented && !string.IsNullOrWhiteSpace(x.Controller))
            .GroupBy(x => x.Controller!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        private static readonly IReadOnlyList<LegacyPageDefinition> ImplementedMenuPages = AllPages
            .Where(x => x.IncludeInMenu && x.IsImplemented)
            .ToList();

        private static readonly IReadOnlySet<string> AlwaysOpenAspPages = new HashSet<string>(
            AllPages
                .Where(x => x.AlwaysAllowAuthenticated)
                .Select(x => x.AspPage),
            StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<LegacyPageDefinition> Pages => AllPages;

        public static IReadOnlyList<LegacyPageDefinition> GetImplementedMenuPages()
        {
            return ImplementedMenuPages;
        }

        public static bool TryGetControllerRoute(string controller, out LegacyPageDefinition route)
        {
            return ControllerRouteMap.TryGetValue(controller, out route!);
        }

        public static bool IsAlwaysOpenAspPage(string aspPage)
        {
            return AlwaysOpenAspPages.Contains(aspPage);
        }
    }

    public class LegacyPageDefinition
    {
        public string Key { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public string AspPage { get; init; } = string.Empty;
        public string Url { get; init; } = string.Empty;
        public string? Controller { get; init; }
        public string Action { get; init; } = "Index";
        public bool IncludeInMenu { get; init; }
        public bool IsImplemented { get; init; }
        public bool AlwaysAllowAuthenticated { get; init; }
        public string? BypassMatchStatus { get; init; }
        public string? BypassReason { get; init; }
        public string? PlannedController { get; init; }
        public string? PlannedAction { get; init; }
    }
}
