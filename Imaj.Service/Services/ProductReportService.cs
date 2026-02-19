using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Service.DTOs;
using Imaj.Service.DTOs.Security;
using Imaj.Service.Interfaces;
using Imaj.Service.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Imaj.Service.Services
{
    public class ProductReportService : BaseService, IProductReportService
    {
        private readonly ICurrentPermissionContext _currentPermissionContext;

        public ProductReportService(
            IUnitOfWork unitOfWork,
            ILogger<ProductReportService> logger,
            IConfiguration configuration,
            ICurrentPermissionContext currentPermissionContext)
            : base(unitOfWork, logger, configuration)
        {
            _currentPermissionContext = currentPermissionContext;
        }

        public async Task<ServiceResult<List<ProductReportRowDto>>> GetDetailedReportAsync(ProductReportFilterDto filter)
        {
            try
            {
                var snapshot = await _currentPermissionContext.GetSnapshotAsync();
                if (IsDataScopeDenied(snapshot))
                {
                    return ServiceResult<List<ProductReportRowDto>>.Success(new List<ProductReportRowDto>());
                }

                var activeSnapshot = snapshot!;
                var startDate = filter.StartDate.Date;
                var endDate = filter.EndDate.Date.AddDays(1);
                var languageId = filter.LanguageId > 0 ? filter.LanguageId : 1;
                var productGroupName = filter.ProductGroupName?.Trim();
                var productCode = filter.ProductCode?.Trim();
                var customerCode = filter.CustomerCode?.Trim();

                var query =
                    from jp in _unitOfWork.Repository<JobProd>().Query()
                    join j in _unitOfWork.Repository<Job>().Query() on jp.JobID equals j.Id
                    join p in _unitOfWork.Repository<Product>().Query() on jp.ProductID equals p.Id
                    join pg in _unitOfWork.Repository<ProdGrp>().Query() on p.ProdGrpID equals pg.Id
                    join xp in _unitOfWork.Repository<XProduct>().Query().Where(x => x.LanguageID == languageId)
                        on p.Id equals xp.ProductID into xpGroup
                    from xProduct in xpGroup.DefaultIfEmpty()
                    join xpg in _unitOfWork.Repository<XProdGrp>().Query().Where(x => x.LanguageID == languageId)
                        on pg.Id equals xpg.ProdGrpID into xpgGroup
                    from xProductGroup in xpgGroup.DefaultIfEmpty()
                    join c in _unitOfWork.Repository<Customer>().Query() on j.CustomerID equals c.Id into cGroup
                    from customer in cGroup.DefaultIfEmpty()
                    where jp.Deleted == 0
                          && j.StartDT >= startDate
                          && j.StartDT < endDate
                          && activeSnapshot.AllowedFunctionIds.Contains(j.FunctionID)
                          && (activeSnapshot.CompanyScopeMode != CompanyScopeMode.CompanyBound
                              || !activeSnapshot.CompanyId.HasValue
                              || j.CompanyID == activeSnapshot.CompanyId.Value)
                    select new ProductReportRowDto
                    {
                        ProductGroupName = xProductGroup != null ? xProductGroup.Name : string.Empty,
                        ProductCode = p.Code,
                        ProductName = xProduct != null ? xProduct.Name : string.Empty,
                        Reference = j.Reference,
                        JobDate = j.StartDT,
                        CustomerCode = customer != null ? customer.Code : string.Empty,
                        CustomerName = customer != null ? customer.Name : string.Empty,
                        JobName = j.Name,
                        Notes = jp.Notes,
                        Quantity = (decimal)jp.Quantity,
                        Amount = jp.NetAmount
                    };

                if (!string.IsNullOrWhiteSpace(productGroupName))
                {
                    query = query.Where(x => x.ProductGroupName == productGroupName);
                }

                if (!string.IsNullOrWhiteSpace(productCode))
                {
                    query = query.Where(x => x.ProductCode == productCode);
                }

                if (!string.IsNullOrWhiteSpace(customerCode))
                {
                    query = query.Where(x => x.CustomerCode == customerCode);
                }

                var items = await query
                    .OrderBy(x => x.ProductGroupName)
                    .ThenBy(x => x.ProductName)
                    .ThenBy(x => x.JobDate)
                    .ThenBy(x => x.Reference)
                    .ToListAsync();

                return ServiceResult<List<ProductReportRowDto>>.Success(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detaylı ürün raporu alınırken hata oluştu.");
                return ServiceResult<List<ProductReportRowDto>>.Fail("Detaylı ürün raporu alınırken bir hata oluştu.");
            }
        }

        private static bool IsDataScopeDenied(PermissionSnapshotDto? snapshot)
        {
            return snapshot == null
                   || snapshot.IsDenied
                   || snapshot.CompanyScopeMode == CompanyScopeMode.Deny
                   || snapshot.AllowedFunctionIds.Count == 0;
        }
    }
}
