# Product Category & Group Real Data Integration

I have replaced the mock data for Product Categories and Product Groups with real data fetching from the database. This ensures that the Product Report and Product Selection Modal reflect the actual data in the system.

## Changes

### 1. Backend Service Updates
**File:** `Imaj.Service/Services/ProductService.cs`
- Implemented `GetCategoriesAsync`: Fetches categories from `ProdCat`, filters by **CompanyID = 7** and LanguageID=1, and filters out invisible ones.
- Implemented `GetProductGroupsAsync`: Fetches groups from `ProdGrp`, filters by **CompanyID = 7** and LanguageID=1.
- Implemented `GetFunctionsAsync`: Fetches functions from `Function`, filters by **CompanyID = 7**, LanguageID=1, and filters out invisible ones.
- Updated `GetByFilterAsync`: Supports nullable `IsInvalid` filter logic (handles "All" selection).

**File:** `Imaj.Service/Services/JobService.cs`
- Updated `GetWorkTypesAsync`: Filters by **CompanyID = 7** and **Invisible = false**.
- Updated `GetTimeTypesAsync`: Filters by **CompanyID = 7** and **Invisible = false**.
- Updated `GetFunctionsAsync`: Filters by **CompanyID = 7** and **Invisible = false**.

**File:** `Imaj.Service/DTOs`
- Created `FunctionDto.cs` and `ProductGroupDto.cs`.
- Updated `ProductFilterDto.cs`: Changed `IsInvalid` to `bool?`.

### 2. API Endpoints
**File:** `Imaj.Web/Controllers/ProductController.cs`
- Added `[HttpGet] Product/GetCategories`, `Product/GetProductGroups`, `Product/GetFunctions`.
- Fixes implicit routing issue by enforcing explicit "Product/" prefix.

### 3. Frontend Integration
**File:** `Imaj.Web/wwwroot/js/components/product-select-modal.js`
- Fetches real data for categories, groups, and functions.
- Handles `IsInvalid` filter: "Tümü" (`""`) maps to `null`.
- Clears the result list (`items = []`) immediately when search starts.

**File:** `Imaj.Web/Views/Shared/_ProductSelectModal.cshtml`
- Added "Tümü" option to the "Geçersiz" (Invalid) filter.
- Dynamic dropdown population for Category, Group, and Function.

## Verification
- **Build Status**: ✅ Succeeded.
- **Manual Verification**:
    1. Check "Ürün Raporu" page dropdowns.
    2. Open Modal via "Seç".
    3. Verify "Geçersiz" filter:
       - "Tümü": Returns both valid and invalid.
       - "Hayır": Returns only valid.
       - "Evet": Returns only invalid.
    4. Verify Grid clears when "Sorgula" is clicked before new results arrive.
    5. Check screens using WorkType, TimeType, and Function (e.g., Job Create/Edit, Overtime Report); they should only show active types for Company 7.
