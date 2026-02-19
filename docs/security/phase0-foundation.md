# Phase 0 Security Foundation (Completed)

This phase prepares parity-safe infrastructure before adding new MVC pages.

## Scope

- Centralize legacy ASP page matrix in one source: `Imaj.Web/Authorization/LegacyPageCatalog.cs`
- Feed both route guard and menu guard from the same catalog
- Keep `default deny` behavior for unmapped controllers/pages
- Keep only implemented pages visible in current menu
- Track planned legacy pages in catalog without exposing broken links

## Current Route Guard Sources

- `PageRouteResolver` now resolves controller -> ASP page via `LegacyPageCatalog`
- Bypass endpoints are catalog-driven:
  - `Home` (`Bypass-Home`)
  - `Culture` (`Bypass-Culture`)

## Current Menu Guard Sources

- `PermissionViewService` now builds visible menu from implemented catalog entries
- Menu still permission-filtered by snapshot (`HasAllMenu` or explicit allowed pages)
- Always-open ASP pages are resolved from catalog

## Planned (Not Yet Implemented) Legacy Menu Pages

The following are already registered in catalog as planned entries (`IsImplemented=false`):

- `UserQry.asp`
- `ResoCatQry.asp`
- `FunctionQry.asp`
- `ResourceQry.asp`
- `ReserveCrossTab.asp`
- `ReasonQry.asp`
- `AbsenceQry.asp`
- `ReserveQry.asp`
- `WorkTypeQry.asp`
- `TimeTypeQry.asp`
- `EmployeeQry.asp`
- `TaxTypeQry.asp`
- `ProdCatQry.asp`
- `ProdGrpQry.asp`
- `ProductQry.asp`
- `JobEntryQry.asp`

## Acceptance for Phase 0

- Route/menu mapping duplication removed
- No permission widening introduced
- Existing implemented pages remain accessible by same permissions
- Non-implemented pages remain non-routable/non-visible
