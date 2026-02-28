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
    public class InvoiceService : BaseService, IInvoiceService
    {
        private readonly ICurrentPermissionContext _currentPermissionContext;

        public InvoiceService(
            IUnitOfWork unitOfWork,
            ILogger<InvoiceService> logger,
            IConfiguration configuration,
            ICurrentPermissionContext currentPermissionContext)
            : base(unitOfWork, logger, configuration)
        {
            _currentPermissionContext = currentPermissionContext;
        }

        public async Task<ServiceResult<PagedResult<InvoiceDto>>> GetByFilterAsync(InvoiceFilterDto filter)
        {
            try
            {
                var snapshot = await _currentPermissionContext.GetSnapshotAsync();
                if (IsDataScopeDenied(snapshot))
                {
                    return ServiceResult<PagedResult<InvoiceDto>>.Success(new PagedResult<InvoiceDto>
                    {
                        Items = new List<InvoiceDto>(),
                        TotalCount = 0,
                        PageNumber = filter.Page > 0 ? filter.Page : 1,
                        PageSize = filter.PageSize > 0 ? filter.PageSize : 10
                    });
                }

                var activeSnapshot = snapshot!;
                var invoices = BuildScopedInvoiceQuery(activeSnapshot);
                var customers = _unitOfWork.Repository<Customer>().Query();
                var states = _unitOfWork.Repository<XState>().Query();

                var query = from inv in invoices
                            join jobCust in customers on inv.JobCustomerID equals jobCust.Id into jobCustGroup
                            from jobCustomer in jobCustGroup.DefaultIfEmpty()
                            join invoCust in customers on inv.InvoCustomerID equals invoCust.Id into invoCustGroup
                            from invoiceCustomer in invoCustGroup.DefaultIfEmpty()
                            join xstate in states.Where(s => s.LanguageID == CurrentLanguageId)
                                on inv.StateID equals xstate.StateID into stateGroup
                            from state in stateGroup.DefaultIfEmpty()
                            select new
                            {
                                Invoice = inv,
                                JobCustomer = jobCustomer,
                                InvoiceCustomer = invoiceCustomer,
                                StateName = state != null ? state.Name : null
                            };

                // Filters
                if (!string.IsNullOrWhiteSpace(filter.JobCustomerCode))
                {
                    var code = filter.JobCustomerCode.Trim();
                    query = query.Where(x => x.JobCustomer != null && x.JobCustomer.Code == code);
                }

                if (!string.IsNullOrWhiteSpace(filter.JobCustomerName))
                {
                    var name = filter.JobCustomerName.Trim();
                    query = query.Where(x => x.JobCustomer != null && x.JobCustomer.Name.Contains(name));
                }

                if (!string.IsNullOrWhiteSpace(filter.InvoiceCustomerCode))
                {
                    var code = filter.InvoiceCustomerCode.Trim();
                    query = query.Where(x => x.InvoiceCustomer != null && x.InvoiceCustomer.Code == code);
                }

                if (!string.IsNullOrWhiteSpace(filter.InvoiceCustomerName))
                {
                    var name = filter.InvoiceCustomerName.Trim();
                    query = query.Where(x => x.InvoiceCustomer != null && x.InvoiceCustomer.Name.Contains(name));
                }

                if (filter.ReferenceStart.HasValue)
                {
                    query = query.Where(x => x.Invoice.Reference >= filter.ReferenceStart.Value);
                }

                if (filter.ReferenceEnd.HasValue)
                {
                    query = query.Where(x => x.Invoice.Reference <= filter.ReferenceEnd.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.Name))
                {
                    var name = filter.Name.Trim();
                    query = query.Where(x => x.Invoice.Name.Contains(name));
                }

                if (!string.IsNullOrWhiteSpace(filter.RelatedPerson))
                {
                    var related = filter.RelatedPerson.Trim();
                    query = query.Where(x => x.Invoice.Contact.Contains(related));
                }

                if (filter.IssueDateStart.HasValue)
                {
                    query = query.Where(x => x.Invoice.IssueDate >= filter.IssueDateStart.Value);
                }

                if (filter.IssueDateEnd.HasValue)
                {
                    query = query.Where(x => x.Invoice.IssueDate <= filter.IssueDateEnd.Value);
                }

                if (filter.StateId.HasValue)
                {
                    query = query.Where(x => x.Invoice.StateID == filter.StateId.Value);
                }

                if (filter.Evaluated.HasValue)
                {
                    query = query.Where(x => x.Invoice.Evaluated == filter.Evaluated.Value);
                }

                var page = filter.Page > 0 ? filter.Page : 1;
                var pageSize = filter.PageSize > 0 ? filter.PageSize : 10;
                var first = filter.First.HasValue && filter.First.Value > 0 ? filter.First.Value : (int?)null;

                var orderedQuery = query
                    .OrderByDescending(x => x.Invoice.IssueDate)
                    .ThenByDescending(x => x.Invoice.Id);

                var scopedQuery = first.HasValue
                    ? orderedQuery.Take(first.Value)
                    : orderedQuery;

                var totalCount = await scopedQuery.CountAsync();
                var skip = (page - 1) * pageSize;

                var items = await scopedQuery
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(x => new InvoiceDto
                    {
                        Id = x.Invoice.Id,
                        Reference = x.Invoice.Reference,
                        JobCustomerCode = x.JobCustomer != null ? x.JobCustomer.Code : null,
                        JobCustomerName = x.JobCustomer != null ? x.JobCustomer.Name : null,
                        InvoiceCustomerCode = x.InvoiceCustomer != null ? x.InvoiceCustomer.Code : null,
                        InvoiceCustomerName = x.InvoiceCustomer != null ? x.InvoiceCustomer.Name : null,
                        Name = x.Invoice.Name,
                        IssueDate = x.Invoice.IssueDate,
                        GrossAmount = x.Invoice.GrossAmount,
                        StateName = x.StateName,
                        Evaluated = x.Invoice.Evaluated
                    })
                    .ToListAsync();

                var result = new PagedResult<InvoiceDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                };

                return ServiceResult<PagedResult<InvoiceDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invoice search failed");
                return ServiceResult<PagedResult<InvoiceDto>>.Fail("Fatura sorgulama sırasında hata oluştu.");
            }
        }

        public async Task<ServiceResult<List<InvoiceDetailDto>>> GetDetailsByReferencesAsync(List<int> references)
        {
            try
            {
                var snapshot = await _currentPermissionContext.GetSnapshotAsync();
                if (IsDataScopeDenied(snapshot))
                {
                    return ServiceResult<List<InvoiceDetailDto>>.Success(new List<InvoiceDetailDto>());
                }

                var refList = references?.Where(r => r > 0).Distinct().ToList() ?? new List<int>();
                if (refList.Count == 0)
                {
                    return ServiceResult<List<InvoiceDetailDto>>.Success(new List<InvoiceDetailDto>());
                }

                var activeSnapshot = snapshot!;
                var scopedJobsQuery = BuildScopedJobQuery(activeSnapshot);
                var scopedJobIds = scopedJobsQuery.Select(j => j.Id);
                var invoices = BuildScopedInvoiceQuery(activeSnapshot);
                var customers = _unitOfWork.Repository<Customer>().Query();
                var states = _unitOfWork.Repository<XState>().Query();

                var invoiceRows = await (from inv in invoices
                                         where refList.Contains(inv.Reference)
                                         join jobCust in customers on inv.JobCustomerID equals jobCust.Id into jobCustGroup
                                         from jobCustomer in jobCustGroup.DefaultIfEmpty()
                                         join invoCust in customers on inv.InvoCustomerID equals invoCust.Id into invoCustGroup
                                         from invoiceCustomer in invoCustGroup.DefaultIfEmpty()
                                         join xstate in states.Where(s => s.LanguageID == CurrentLanguageId)
                                             on inv.StateID equals xstate.StateID into stateGroup
                                         from state in stateGroup.DefaultIfEmpty()
                                         select new
                                         {
                                             Invoice = inv,
                                             JobCustomerName = jobCustomer != null ? jobCustomer.Name : null,
                                             InvoiceCustomerName = invoiceCustomer != null ? invoiceCustomer.Name : null,
                                             StateName = state != null ? state.Name : null
                                         })
                                         .ToListAsync();

                var invoiceIds = invoiceRows.Select(x => x.Invoice.Id).Distinct().ToList();

                var lines = await _unitOfWork.Repository<InvoLine>().Query()
                    .Where(l => invoiceIds.Contains(l.InvoiceID) && l.Deleted == 0)
                    .ToListAsync();

                var lineIds = lines.Select(l => l.Id).Distinct().ToList();
                var invoJobs = await _unitOfWork.Repository<InvoJob>().Query()
                    .Where(j => lineIds.Contains(j.InvoLineID) && j.Deleted == 0 && scopedJobIds.Contains(j.JobID))
                    .ToListAsync();
                List<Job> jobs;
                Dictionary<decimal, bool> jobSelectMap;
                Dictionary<decimal, List<InvoJob>> invoJobsByLineId;

                if (invoJobs.Any())
                {
                    var jobIdsFromRows = invoJobs.Select(r => r.JobID).Distinct().ToList();
                    jobs = await scopedJobsQuery
                        .Where(j => jobIdsFromRows.Contains(j.Id))
                        .ToListAsync();
                    jobSelectMap = invoJobs
                        .GroupBy(r => r.JobID)
                        .ToDictionary(g => g.Key, g => g.Any(x => x.SelectFlag));
                    invoJobsByLineId = invoJobs
                        .GroupBy(r => r.InvoLineID)
                        .ToDictionary(g => g.Key, g => g.ToList());
                }
                else
                {
                    jobs = await scopedJobsQuery
                        .Where(j => j.InvoLineID.HasValue && lineIds.Contains(j.InvoLineID.Value))
                        .ToListAsync();
                    jobSelectMap = jobs.ToDictionary(j => j.Id, j => j.SelectFlag);
                    invoJobsByLineId = new Dictionary<decimal, List<InvoJob>>();
                }

                var invoProdCats = await _unitOfWork.Repository<InvoProdCat>().Query()
                    .Where(ipc => lineIds.Contains(ipc.InvoLineID) && ipc.Deleted == 0)
                    .ToListAsync();

                var prodCatIds = invoProdCats.Select(ipc => ipc.ProdCatID).Distinct().ToList();
                var prodCats = await _unitOfWork.Repository<ProdCat>().Query()
                    .Where(pc => prodCatIds.Contains(pc.Id))
                    .ToListAsync();

                var xProdCats = await _unitOfWork.Repository<XProdCat>().Query()
                    .Where(x => prodCatIds.Contains(x.ProdCatID) && x.LanguageID == CurrentLanguageId)
                    .ToListAsync();

                var taxTypes = await _unitOfWork.Repository<TaxType>().Query().ToListAsync();
                var xTaxTypes = await _unitOfWork.Repository<XTaxType>().Query()
                    .Where(x => x.LanguageID == CurrentLanguageId)
                    .ToListAsync();

                var invoTaxes = await _unitOfWork.Repository<InvoTax>().Query()
                    .Where(t => invoiceIds.Contains(t.InvoiceID) && t.Deleted == 0)
                    .ToListAsync();

                var taxTypeById = taxTypes.ToDictionary(x => x.Id, x => x);
                var xTaxTypeById = xTaxTypes.GroupBy(x => x.TaxTypeID).ToDictionary(x => x.Key, x => x.First());
                var prodCatById = prodCats.ToDictionary(x => x.Id, x => x);
                var xProdCatById = xProdCats.GroupBy(x => x.ProdCatID).ToDictionary(x => x.Key, x => x.First());
                var invoTaxMap = invoTaxes
                    .GroupBy(t => t.InvoiceID)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var result = new List<InvoiceDetailDto>();

                foreach (var row in invoiceRows.OrderBy(x => x.Invoice.IssueDate).ThenBy(x => x.Invoice.Reference))
                {
                    var invoice = row.Invoice;
                    var invoiceLines = lines.Where(l => l.InvoiceID == invoice.Id).OrderBy(l => l.Sequence).ToList();
                    var invoiceLineIds = invoiceLines.Select(l => l.Id).ToList();
                    List<Job> invoiceJobs;
                    if (invoJobsByLineId.Count > 0)
                    {
                        var invoiceJobIdsFromMap = invoiceLineIds
                            .SelectMany(id => invoJobsByLineId.TryGetValue(id, out var jobRows) ? jobRows : Enumerable.Empty<InvoJob>())
                            .Select(r => r.JobID)
                            .Distinct()
                            .ToList();
                        invoiceJobs = jobs.Where(j => invoiceJobIdsFromMap.Contains(j.Id)).ToList();
                    }
                    else
                    {
                        invoiceJobs = jobs.Where(j => j.InvoLineID.HasValue && invoiceLineIds.Contains(j.InvoLineID.Value)).ToList();
                    }

                    var invoiceJobIds = invoiceJobs.Select(j => j.Id).ToList();

                    var lineDtos = invoiceLines.Select(l =>
                    {
                        var taxLabel = string.Empty;
                        if (l.TaxTypeID.HasValue && taxTypeById.TryGetValue(l.TaxTypeID.Value, out var taxType))
                        {
                            if (xTaxTypeById.TryGetValue(l.TaxTypeID.Value, out var xTax))
                            {
                                taxLabel = string.IsNullOrWhiteSpace(xTax.InvoLinePostfix) ? taxType.Code : xTax.InvoLinePostfix;
                            }
                            else
                            {
                                taxLabel = taxType.Code;
                            }
                        }

                        return new InvoiceLineDto
                        {
                            Selected = l.SelectFlag,
                            Sequence = l.Sequence,
                            Notes = l.Notes,
                            Quantity = l.Quantity,
                            Price = l.Price,
                            Amount = l.Amount,
                            TaxType = taxLabel,
                            TaxTypeId = l.TaxTypeID
                        };
                    }).ToList();

                    var jobDtos = invoiceJobs.Select(j =>
                    {
                        var selected = jobSelectMap.TryGetValue(j.Id, out var flag) ? flag : j.SelectFlag;
                        return new InvoiceJobDto
                        {
                            Selected = selected,
                            Reference = j.Reference,
                            Name = j.Name,
                            Amount = j.ProdSum
                        };
                    }).ToList();

                    var invoiceProdCats = invoProdCats
                        .Where(ipc => invoiceLineIds.Contains(ipc.InvoLineID))
                        .ToList();
                    var productCatSummary = invoiceProdCats
                        .GroupBy(x => x.ProdCatID)
                        .Select(g =>
                        {
                            var name = prodCatById.TryGetValue(g.Key, out var prodCat) ? prodCat.Id.ToString() : g.Key.ToString();
                            if (xProdCatById.TryGetValue(g.Key, out var xProd))
                            {
                                name = xProd.Name;
                            }

                            return new InvoiceProdCatSummaryDto
                            {
                                ProdCatId = g.Key,
                                Name = name,
                                SubTotal = g.Sum(x => x.GrossAmount),
                                NetTotal = g.Sum(x => x.NetAmount)
                            };
                        })
                        .OrderBy(x => x.Name)
                        .ToList();

                    var lineSubTotalsByTax = invoiceLines
                        .Where(l => l.TaxTypeID.HasValue)
                        .GroupBy(l => l.TaxTypeID!.Value)
                        .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

                    List<InvoiceTaxSummaryDto> taxSummaries;
                    var invoiceTaxRows = invoTaxMap.TryGetValue(invoice.Id, out var taxRows)
                        ? taxRows
                        : new List<InvoTax>();
                    if (invoiceTaxRows.Any())
                    {
                        taxSummaries = invoiceTaxRows
                            .GroupBy(t => t.TaxTypeID)
                            .Select(g =>
                            {
                                taxTypeById.TryGetValue(g.Key, out var taxType);
                                xTaxTypeById.TryGetValue(g.Key, out var xTax);
                                var subTotal = g.Sum(x => x.GrossAmount);
                                var rate = g.Select(x => (decimal)x.TaxPercentage).FirstOrDefault();
                                if (rate <= 0 && taxType != null)
                                {
                                    rate = taxType.TaxPercentage;
                                }
                                var taxAmount = g.Sum(x => x.TaxAmount);
                                var name = xTax != null ? xTax.Name : taxType?.Code;

                                return new InvoiceTaxSummaryDto
                                {
                                    TaxTypeId = g.Key,
                                    Code = taxType?.Code,
                                    Name = name,
                                    SubTotal = subTotal,
                                    Rate = rate,
                                    TaxAmount = taxAmount,
                                    NetTotal = g.Sum(x => x.NetAmount)
                                };
                            })
                            .OrderBy(x => x.Code)
                            .ToList();
                    }
                    else
                    {
                        taxSummaries = lineSubTotalsByTax
                            .Select(g =>
                            {
                                taxTypeById.TryGetValue(g.Key, out var taxType);
                                xTaxTypeById.TryGetValue(g.Key, out var xTax);
                                var subTotal = g.Value;
                                var rate = taxType != null ? taxType.TaxPercentage : 0;
                                var taxAmount = subTotal * rate / 100m;
                                var name = xTax != null ? xTax.Name : taxType?.Code;

                                return new InvoiceTaxSummaryDto
                                {
                                    TaxTypeId = g.Key,
                                    Code = taxType?.Code,
                                    Name = name,
                                    SubTotal = subTotal,
                                    Rate = rate,
                                    TaxAmount = taxAmount,
                                    NetTotal = subTotal + taxAmount
                                };
                            })
                            .OrderBy(x => x.Code)
                            .ToList();
                    }

                    result.Add(new InvoiceDetailDto
                    {
                        Reference = invoice.Reference,
                        JobCustomerName = row.JobCustomerName,
                        InvoiceCustomerName = row.InvoiceCustomerName,
                        Name = invoice.Name,
                        RelatedPerson = invoice.Contact,
                        IssueDate = invoice.IssueDate,
                        StateName = row.StateName,
                        Evaluated = invoice.Evaluated,
                        Notes = invoice.Notes,
                        FooterNote = invoice.Footer,
                        Lines = lineDtos,
                        Jobs = jobDtos,
                        ProductCategories = productCatSummary,
                        Taxes = taxSummaries
                    });
                }

                return ServiceResult<List<InvoiceDetailDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invoice detail fetch failed");
                return ServiceResult<List<InvoiceDetailDto>>.Fail("Fatura detayları yüklenirken hata oluştu.");
            }
        }

        public async Task<ServiceResult<InvoiceHistoryDto>> GetHistoryByReferenceAsync(int reference)
        {
            try
            {
                var snapshot = await _currentPermissionContext.GetSnapshotAsync();
                if (IsDataScopeDenied(snapshot))
                {
                    return ServiceResult<InvoiceHistoryDto>.Fail("Fatura bulunamadı.");
                }

                var activeSnapshot = snapshot!;
                var invoice = await BuildScopedInvoiceQuery(activeSnapshot)
                    .FirstOrDefaultAsync(i => i.Reference == reference);

                if (invoice == null)
                {
                    return ServiceResult<InvoiceHistoryDto>.Fail("Fatura bulunamadı.");
                }

                var detailResult = await GetDetailsByReferencesAsync(new List<int> { reference });
                var detail = detailResult.IsSuccess ? detailResult.Data?.FirstOrDefault() : null;
                if (detail == null)
                {
                    return ServiceResult<InvoiceHistoryDto>.Fail("Fatura bulunamadı.");
                }

                var logs = await (from log in _unitOfWork.Repository<InvoiceLog>().Query()
                                  where log.InvoiceID == invoice.Id
                                  join u in _unitOfWork.Repository<User>().Query() on log.UserID equals u.Id
                                  join xl in _unitOfWork.Repository<XLogAction>().Query().Where(x => x.LanguageID == CurrentLanguageId)
                                      on log.LogActionID equals xl.LogActionID into xlGroup
                                  from xLog in xlGroup.DefaultIfEmpty()
                                  join la in _unitOfWork.Repository<LogAction>().Query()
                                      on log.LogActionID equals la.Id into laGroup
                                  from logAction in laGroup.DefaultIfEmpty()
                                  orderby log.ActionDT descending
                                  select new InvoiceLogDto
                                  {
                                      Id = log.Id,
                                      LogDate = log.ActionDT,
                                      UserId = log.UserID,
                                      UserCode = u.Code,
                                      UserName = u.Name,
                                      LogActionId = log.LogActionID,
                                      ActionName = xLog != null ? xLog.Name : (logAction != null ? logAction.Descr : "Bilinmiyor")
                                  })
                    .ToListAsync();

                var history = new InvoiceHistoryDto
                {
                    Detail = detail,
                    Items = logs
                };

                return ServiceResult<InvoiceHistoryDto>.Success(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invoice history fetch failed. Reference: {Reference}", reference);
                return ServiceResult<InvoiceHistoryDto>.Fail("Fatura geçmişi yüklenirken hata oluştu.");
            }
        }

        private static bool IsDataScopeDenied(PermissionSnapshotDto? snapshot)
        {
            return snapshot == null
                   || snapshot.IsDenied
                   || snapshot.CompanyScopeMode == CompanyScopeMode.Deny
                   || snapshot.AllowedFunctionIds.Count == 0;
        }

        private IQueryable<Job> BuildScopedJobQuery(PermissionSnapshotDto snapshot)
        {
            var query = _unitOfWork.Repository<Job>().Query()
                .Where(j => snapshot.AllowedFunctionIds.Contains(j.FunctionID));

            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound && snapshot.CompanyId.HasValue)
            {
                query = query.Where(j => j.CompanyID == snapshot.CompanyId.Value);
            }

            return query;
        }

        private IQueryable<Invoice> BuildScopedInvoiceQuery(PermissionSnapshotDto snapshot)
        {
            var invoiceQuery = _unitOfWork.Repository<Invoice>().Query();

            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound && snapshot.CompanyId.HasValue)
            {
                invoiceQuery = invoiceQuery.Where(i => i.CompanyID == snapshot.CompanyId.Value);
            }

            var scopedJobs = BuildScopedJobQuery(snapshot);
            var scopedJobIds = scopedJobs.Select(j => j.Id);
            var scopedInvoLineIdsFromJobs = scopedJobs
                .Select(j => j.InvoLineID ?? 0m)
                .Where(invoLineId => invoLineId > 0);

            var scopedInvoiceIds = _unitOfWork.Repository<InvoLine>().Query()
                .Where(line =>
                    line.Deleted == 0
                    && (
                        _unitOfWork.Repository<InvoJob>().Query().Any(invoJob =>
                            invoJob.InvoLineID == line.Id
                            && invoJob.Deleted == 0
                            && scopedJobIds.Contains(invoJob.JobID))
                        || scopedInvoLineIdsFromJobs.Contains(line.Id)
                    ))
                .Select(line => line.InvoiceID)
                .Distinct();

            return invoiceQuery.Where(i => scopedInvoiceIds.Contains(i.Id));
        }

    }
}
