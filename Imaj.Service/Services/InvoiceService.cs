using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Service.DTOs;
using Imaj.Service.DTOs.Security;
using Imaj.Service.Interfaces;
using Imaj.Service.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Imaj.Service.Services
{
    public class InvoiceService : BaseService, IInvoiceService
    {
        private const decimal OpenStateId = 210m;
        private const decimal ConfirmedStateId = 220m;
        private const decimal IssuedStateId = 230m;
        private const decimal KilledStateId = 240m;
        private const decimal DiscardedStateId = 250m;

        private const decimal ConfirmLogActionId = 1110m;
        private const decimal UndoConfirmLogActionId = 1120m;
        private const decimal IssueLogActionId = 1130m;
        private const decimal KillLogActionId = 1140m;
        private const decimal DiscardLogActionId = 1150m;
        private const decimal EvaluateLogActionId = 1160m;
        private const decimal UndoEvaluateLogActionId = 1170m;

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
                var stopwatch = Stopwatch.StartNew();
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
                var invoices = _unitOfWork.Repository<Invoice>().Query().AsNoTracking();
                var customers = _unitOfWork.Repository<Customer>().Query().AsNoTracking();

                // Customer filters are applied with ID subqueries so the heavy joins
                // are not part of the main count/paging query.
                var hasJobCustomerFilter = false;
                var jobCustomerFilterQuery = customers;
                if (!string.IsNullOrWhiteSpace(filter.JobCustomerCode))
                {
                    var code = filter.JobCustomerCode.Trim();
                    jobCustomerFilterQuery = jobCustomerFilterQuery.Where(c => c.Code == code);
                    hasJobCustomerFilter = true;
                }
                if (!string.IsNullOrWhiteSpace(filter.JobCustomerName))
                {
                    var name = filter.JobCustomerName.Trim();
                    jobCustomerFilterQuery = jobCustomerFilterQuery.Where(c => c.Name.Contains(name));
                    hasJobCustomerFilter = true;
                }
                if (hasJobCustomerFilter)
                {
                    var jobCustomerIds = jobCustomerFilterQuery.Select(c => c.Id);
                    invoices = invoices.Where(i => jobCustomerIds.Contains(i.JobCustomerID));
                }

                var hasInvoiceCustomerFilter = false;
                var invoiceCustomerFilterQuery = customers;
                if (!string.IsNullOrWhiteSpace(filter.InvoiceCustomerCode))
                {
                    var code = filter.InvoiceCustomerCode.Trim();
                    invoiceCustomerFilterQuery = invoiceCustomerFilterQuery.Where(c => c.Code == code);
                    hasInvoiceCustomerFilter = true;
                }
                if (!string.IsNullOrWhiteSpace(filter.InvoiceCustomerName))
                {
                    var name = filter.InvoiceCustomerName.Trim();
                    invoiceCustomerFilterQuery = invoiceCustomerFilterQuery.Where(c => c.Name.Contains(name));
                    hasInvoiceCustomerFilter = true;
                }
                if (hasInvoiceCustomerFilter)
                {
                    var invoiceCustomerIds = invoiceCustomerFilterQuery.Select(c => c.Id);
                    invoices = invoices.Where(i => invoiceCustomerIds.Contains(i.InvoCustomerID));
                }

                if (filter.ReferenceStart.HasValue)
                {
                    invoices = invoices.Where(i => i.Reference >= filter.ReferenceStart.Value);
                }

                if (filter.ReferenceEnd.HasValue)
                {
                    invoices = invoices.Where(i => i.Reference <= filter.ReferenceEnd.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.Name))
                {
                    var name = filter.Name.Trim();
                    invoices = invoices.Where(i => i.Name.Contains(name));
                }

                if (!string.IsNullOrWhiteSpace(filter.RelatedPerson))
                {
                    var related = filter.RelatedPerson.Trim();
                    invoices = invoices.Where(i => i.Contact.Contains(related));
                }

                if (filter.IssueDateStart.HasValue)
                {
                    var startDate = filter.IssueDateStart.Value.Date;
                    invoices = invoices.Where(i => i.IssueDate.HasValue && i.IssueDate.Value >= startDate);
                }

                if (filter.IssueDateEnd.HasValue)
                {
                    var endExclusive = filter.IssueDateEnd.Value.Date.AddDays(1);
                    invoices = invoices.Where(i => i.IssueDate.HasValue && i.IssueDate.Value < endExclusive);
                }

                if (filter.StateId.HasValue)
                {
                    invoices = invoices.Where(i => i.StateID == filter.StateId.Value);
                }

                if (filter.Evaluated.HasValue)
                {
                    invoices = invoices.Where(i => i.Evaluated == filter.Evaluated.Value);
                }

                var page = filter.Page > 0 ? filter.Page : 1;
                var pageSize = filter.PageSize > 0 ? filter.PageSize : 10;
                var first = filter.First.HasValue && filter.First.Value > 0 ? filter.First.Value : (int?)null;

                // Materialize scoped IDs in one DB round-trip.
                // Apply TOP (first) in SQL before materialization so DB does not process
                // more rows than needed for the result window.
                var scopedIdQuery = ApplyInvoiceDataScope(invoices, activeSnapshot)
                    .OrderByDescending(i => i.IssueDate)
                    .ThenByDescending(i => i.Id)
                    .Select(i => i.Id);

                if (first.HasValue)
                {
                    scopedIdQuery = scopedIdQuery.Take(first.Value);
                }

                var windowIds = await scopedIdQuery.ToListAsync();
                var scopeElapsedMs = stopwatch.ElapsedMilliseconds;

                var totalCount = windowIds.Count;
                var countElapsedMs = stopwatch.ElapsedMilliseconds;

                var skip = (page - 1) * pageSize;
                var pageIds = windowIds.Skip(skip).Take(pageSize).ToList();

                // Simple lookup by PK — SQL Server resolves this with a clustered index seek per row.
                var pageRows = pageIds.Count == 0
                    ? new List<InvoiceWindowRow>()
                    : await _unitOfWork.Repository<Invoice>().Query()
                        .AsNoTracking()
                        .Where(i => pageIds.Contains(i.Id))
                        .Select(i => new InvoiceWindowRow
                        {
                            Id = i.Id,
                            Reference = i.Reference,
                            JobCustomerID = i.JobCustomerID,
                            InvoCustomerID = i.InvoCustomerID,
                            Name = i.Name,
                            IssueDate = i.IssueDate,
                            GrossAmount = i.GrossAmount,
                            StateID = i.StateID,
                            Evaluated = i.Evaluated
                        })
                        .ToListAsync();

                // Re-apply the original sort order (DB may return rows in any order for IN queries).
                var idOrder = pageIds.Select((id, idx) => (id, idx)).ToDictionary(x => x.id, x => x.idx);
                pageRows = pageRows.OrderBy(r => idOrder.TryGetValue(r.Id, out var o) ? o : int.MaxValue).ToList();
                var pageElapsedMs = stopwatch.ElapsedMilliseconds;

                var customerIds = pageRows
                    .SelectMany(x => new[] { x.JobCustomerID, x.InvoCustomerID })
                    .Distinct()
                    .ToList();

                var customerMap = customerIds.Count == 0
                    ? new Dictionary<decimal, (string? Code, string? Name)>()
                    : await customers
                        .Where(c => customerIds.Contains(c.Id))
                        .Select(c => new { c.Id, c.Code, c.Name })
                        .ToDictionaryAsync(c => c.Id, c => (Code: (string?)c.Code, Name: (string?)c.Name));
                var customerLookupElapsedMs = stopwatch.ElapsedMilliseconds;

                var stateIds = pageRows
                    .Select(x => x.StateID)
                    .Distinct()
                    .ToList();

                var stateMap = stateIds.Count == 0
                    ? new Dictionary<decimal, string?>()
                    : await _unitOfWork.Repository<XState>().Query()
                        .AsNoTracking()
                        .Where(s => s.LanguageID == CurrentLanguageId && stateIds.Contains(s.StateID))
                        .GroupBy(s => s.StateID)
                        .Select(g => new { StateId = g.Key, Name = g.Select(x => x.Name).FirstOrDefault() })
                        .ToDictionaryAsync(x => x.StateId, x => x.Name);
                var stateLookupElapsedMs = stopwatch.ElapsedMilliseconds;

                var items = pageRows.Select(row =>
                {
                    var hasJobCustomer = customerMap.TryGetValue(row.JobCustomerID, out var jobCustomer);
                    var hasInvoiceCustomer = customerMap.TryGetValue(row.InvoCustomerID, out var invoiceCustomer);

                    return new InvoiceDto
                    {
                        Id = row.Id,
                        Reference = row.Reference,
                        JobCustomerCode = hasJobCustomer ? jobCustomer.Code : null,
                        JobCustomerName = hasJobCustomer ? jobCustomer.Name : null,
                        InvoiceCustomerCode = hasInvoiceCustomer ? invoiceCustomer.Code : null,
                        InvoiceCustomerName = hasInvoiceCustomer ? invoiceCustomer.Name : null,
                        Name = row.Name,
                        IssueDate = row.IssueDate,
                        GrossAmount = row.GrossAmount,
                        StateName = stateMap.TryGetValue(row.StateID, out var stateName) ? stateName : null,
                        Evaluated = row.Evaluated
                    };
                }).ToList();

                var result = new PagedResult<InvoiceDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                };

                var countDurationMs = Math.Max(0, countElapsedMs - scopeElapsedMs);
                var pageDurationMs = Math.Max(0, pageElapsedMs - countElapsedMs);
                var customerDurationMs = Math.Max(0, customerLookupElapsedMs - pageElapsedMs);
                var stateDurationMs = Math.Max(0, stateLookupElapsedMs - customerLookupElapsedMs);

                _logger.LogInformation(
                    "Invoice search perf: scope={ScopeMs}ms, count={CountMs}ms, page={PageMs}ms, customers={CustomerMs}ms, states={StateMs}ms, total={TotalMs}ms, page={Page}, pageSize={PageSize}, first={First}",
                    scopeElapsedMs,
                    countDurationMs,
                    pageDurationMs,
                    customerDurationMs,
                    stateDurationMs,
                    stopwatch.ElapsedMilliseconds,
                    page,
                    pageSize,
                    first);

                return ServiceResult<PagedResult<InvoiceDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invoice search failed");
                return ServiceResult<PagedResult<InvoiceDto>>.Fail("Fatura sorgulama sırasında hata oluştu.");
            }
        }

        public async Task<ServiceResult<List<InvoiceDetailedReportRowDto>>> GetDetailedInvoiceReportAsync(InvoiceFilterDto filter, CancellationToken cancellationToken = default)
        {
            try
            {
                var snapshot = await _currentPermissionContext.GetSnapshotAsync();
                if (IsDataScopeDenied(snapshot))
                {
                    return ServiceResult<List<InvoiceDetailedReportRowDto>>.Success(new List<InvoiceDetailedReportRowDto>());
                }

                var activeSnapshot = snapshot!;
                var items = await BuildInvoiceReportBaseQuery(filter, activeSnapshot)
                    .OrderBy(x => x.CustomerName)
                    .ThenBy(x => x.IssueDate)
                    .ThenBy(x => x.Reference)
                    .Select(x => new InvoiceDetailedReportRowDto
                    {
                        CustomerCode = x.CustomerCode,
                        CustomerName = x.CustomerName,
                        Reference = x.Reference,
                        Name = x.Name,
                        IssueDate = x.IssueDate,
                        StatusName = x.StatusName,
                        Evaluated = x.Evaluated,
                        TaxAmount = x.TaxAmount,
                        SubTotal = x.SubTotal,
                        NetTotal = x.NetTotal
                    })
                    .ToListAsync(cancellationToken);

                return ServiceResult<List<InvoiceDetailedReportRowDto>>.Success(items);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detaylı fatura raporu alınırken hata oluştu.");
                return ServiceResult<List<InvoiceDetailedReportRowDto>>.Fail("Detaylı fatura raporu alınırken bir hata oluştu.");
            }
        }

        public async Task<ServiceResult<List<InvoiceSummaryReportRowDto>>> GetSummaryInvoiceReportAsync(InvoiceFilterDto filter, CancellationToken cancellationToken = default)
        {
            try
            {
                var snapshot = await _currentPermissionContext.GetSnapshotAsync();
                if (IsDataScopeDenied(snapshot))
                {
                    return ServiceResult<List<InvoiceSummaryReportRowDto>>.Success(new List<InvoiceSummaryReportRowDto>());
                }

                var activeSnapshot = snapshot!;
                var items = await BuildInvoiceReportBaseQuery(filter, activeSnapshot)
                    .GroupBy(x => new { x.CustomerCode, x.CustomerName })
                    .Select(g => new InvoiceSummaryReportRowDto
                    {
                        CustomerCode = g.Key.CustomerCode,
                        CustomerName = g.Key.CustomerName,
                        Count = g.Count(),
                        TaxAmount = g.Sum(x => x.TaxAmount),
                        SubTotal = g.Sum(x => x.SubTotal),
                        NetTotal = g.Sum(x => x.NetTotal)
                    })
                    .OrderBy(x => x.CustomerName)
                    .ToListAsync(cancellationToken);

                return ServiceResult<List<InvoiceSummaryReportRowDto>>.Success(items);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Özet fatura raporu alınırken hata oluştu.");
                return ServiceResult<List<InvoiceSummaryReportRowDto>>.Fail("Özet fatura raporu alınırken bir hata oluştu.");
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
                var invoiceCandidates = _unitOfWork.Repository<Invoice>().Query()
                    .AsNoTracking()
                    .Where(inv => refList.Contains(inv.Reference));
                var invoices = ApplyInvoiceDataScope(invoiceCandidates, activeSnapshot);
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
                        StateId = invoice.StateID,
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
                var invoiceCandidates = _unitOfWork.Repository<Invoice>().Query()
                    .AsNoTracking()
                    .Where(i => i.Reference == reference);
                var invoice = await ApplyInvoiceDataScope(invoiceCandidates, activeSnapshot).FirstOrDefaultAsync();

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

        public async Task<ServiceResult> ExecuteWorkflowActionAsync(int reference, InvoiceWorkflowAction action, DateTime? issueDate = null)
        {
            if (reference <= 0)
            {
                return ServiceResult.Fail("Fatura referansı zorunludur.");
            }

            if (!_currentPermissionContext.TryGetCurrentUserId(out var userId))
            {
                return ServiceResult.Fail("Kullanıcı bilgisi doğrulanamadı.");
            }

            var snapshot = await _currentPermissionContext.GetSnapshotAsync();
            if (IsDataScopeDenied(snapshot))
            {
                return ServiceResult.Fail("Fatura kaydı kapsam dışında.");
            }

            var activeSnapshot = snapshot!;
            var invoiceRepo = _unitOfWork.Repository<Invoice>();
            var invoice = await ApplyInvoiceDataScope(invoiceRepo.Query(), activeSnapshot)
                .SingleOrDefaultAsync(x => x.Reference == reference);
            if (invoice == null)
            {
                return ServiceResult.Fail("Fatura bulunamadı.");
            }

            if (invoice.Evaluated && action != InvoiceWorkflowAction.UndoEvaluate)
            {
                return ServiceResult.Fail("Değerlendirilmiş faturalarda sadece değerlendirmeyi geri alma işlemi yapılabilir.");
            }

            var nextStateId = invoice.StateID;
            var nextEvaluated = invoice.Evaluated;
            var nextIssueDate = invoice.IssueDate;
            decimal logActionId;

            switch (action)
            {
                case InvoiceWorkflowAction.Confirm:
                    if (invoice.StateID != OpenStateId)
                    {
                        return ServiceResult.Fail("Onaylama işlemi için fatura açık durumda olmalıdır.");
                    }
                    nextStateId = ConfirmedStateId;
                    logActionId = ConfirmLogActionId;
                    break;
                case InvoiceWorkflowAction.UndoConfirm:
                    if (invoice.StateID != ConfirmedStateId)
                    {
                        return ServiceResult.Fail("Onayı geri alma işlemi için fatura onaylandı durumda olmalıdır.");
                    }
                    nextStateId = OpenStateId;
                    logActionId = UndoConfirmLogActionId;
                    break;
                case InvoiceWorkflowAction.Issue:
                    if (invoice.StateID != ConfirmedStateId)
                    {
                        return ServiceResult.Fail("Kesme işlemi için fatura onaylandı durumda olmalıdır.");
                    }
                    if (!issueDate.HasValue)
                    {
                        return ServiceResult.Fail("Kesilme tarihi zorunludur.");
                    }
                    nextStateId = IssuedStateId;
                    nextIssueDate = issueDate.Value.Date;
                    logActionId = IssueLogActionId;
                    break;
                case InvoiceWorkflowAction.Kill:
                    if (!CanKillInvoiceState(invoice.StateID))
                    {
                        return ServiceResult.Fail("İptal et işlemi bu durum için geçerli değildir.");
                    }
                    nextStateId = KilledStateId;
                    logActionId = KillLogActionId;
                    break;
                case InvoiceWorkflowAction.Discard:
                    if (!CanDiscardInvoiceState(invoice.StateID))
                    {
                        return ServiceResult.Fail("Reddet işlemi bu durum için geçerli değildir.");
                    }
                    nextStateId = DiscardedStateId;
                    logActionId = DiscardLogActionId;
                    break;
                case InvoiceWorkflowAction.Evaluate:
                    if (!CanEvaluateInvoiceState(invoice.StateID) || invoice.Evaluated)
                    {
                        return ServiceResult.Fail("Değerlendirme işlemi bu fatura için uygun değildir.");
                    }
                    nextEvaluated = true;
                    logActionId = EvaluateLogActionId;
                    break;
                case InvoiceWorkflowAction.UndoEvaluate:
                    if (!CanEvaluateInvoiceState(invoice.StateID) || !invoice.Evaluated)
                    {
                        return ServiceResult.Fail("Değerlendirmeyi geri alma işlemi bu fatura için uygun değildir.");
                    }
                    nextEvaluated = false;
                    logActionId = UndoEvaluateLogActionId;
                    break;
                default:
                    return ServiceResult.Fail("Geçersiz işlem.");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                invoice.StateID = nextStateId;
                invoice.Evaluated = nextEvaluated;
                invoice.IssueDate = nextIssueDate;
                invoice.Stamp = 1;
                invoiceRepo.Update(invoice);

                var invoiceLogRepo = _unitOfWork.Repository<InvoiceLog>();
                var nextInvoiceLogId = (await invoiceLogRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0) + 1;
                await invoiceLogRepo.AddAsync(new InvoiceLog
                {
                    Id = nextInvoiceLogId,
                    InvoiceID = invoice.Id,
                    UserID = userId,
                    LogActionID = logActionId,
                    ActionDT = DateTime.Now,
                    Stamp = 1
                });

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();
                return ServiceResult.Success("Fatura durumu güncellendi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Fatura workflow işlemi sırasında hata oluştu. Reference={Reference}, Action={Action}", reference, action);
                return ServiceResult.Fail("Fatura durumu güncellenemedi.");
            }
        }

        private static bool CanKillInvoiceState(decimal stateId)
        {
            return stateId == IssuedStateId;
        }

        private static bool CanDiscardInvoiceState(decimal stateId)
        {
            return stateId == OpenStateId;
        }

        private static bool CanEvaluateInvoiceState(decimal stateId)
        {
            return stateId == IssuedStateId
                || stateId == KilledStateId
                || stateId == DiscardedStateId;
        }

        private IQueryable<InvoiceReportBaseRow> BuildInvoiceReportBaseQuery(
            InvoiceFilterDto filter,
            PermissionSnapshotDto snapshot)
        {
            var invoices = BuildScopedInvoiceQuery(snapshot);
            var customers = _unitOfWork.Repository<Customer>().Query().AsNoTracking();
            var states = _unitOfWork.Repository<XState>().Query()
                .AsNoTracking()
                .Where(x => x.LanguageID == CurrentLanguageId);

            var query =
                from inv in invoices
                join jobCustomer in customers on inv.JobCustomerID equals jobCustomer.Id into jobCustomerGroup
                from jobCustomer in jobCustomerGroup.DefaultIfEmpty()
                join invoiceCustomer in customers on inv.InvoCustomerID equals invoiceCustomer.Id into invoiceCustomerGroup
                from invoiceCustomer in invoiceCustomerGroup.DefaultIfEmpty()
                join state in states on inv.StateID equals state.StateID into stateGroup
                from state in stateGroup.DefaultIfEmpty()
                select new InvoiceReportBaseRow
                {
                    JobCustomerCode = jobCustomer != null ? jobCustomer.Code : string.Empty,
                    JobCustomerName = jobCustomer != null ? jobCustomer.Name : string.Empty,
                    InvoiceCustomerCode = invoiceCustomer != null ? invoiceCustomer.Code : string.Empty,
                    InvoiceCustomerName = invoiceCustomer != null ? invoiceCustomer.Name : string.Empty,
                    CustomerCode = invoiceCustomer != null ? invoiceCustomer.Code : (jobCustomer != null ? jobCustomer.Code : string.Empty),
                    CustomerName = invoiceCustomer != null ? invoiceCustomer.Name : (jobCustomer != null ? jobCustomer.Name : string.Empty),
                    Reference = inv.Reference,
                    Name = inv.Name,
                    RelatedPerson = inv.Contact,
                    IssueDate = inv.IssueDate,
                    StateId = inv.StateID,
                    StatusName = state != null ? state.Name : string.Empty,
                    Evaluated = inv.Evaluated,
                    TaxAmount = inv.TaxAmount,
                    SubTotal = inv.NetAmount,
                    NetTotal = inv.GrossAmount
                };

            if (!string.IsNullOrWhiteSpace(filter.JobCustomerCode))
            {
                var code = filter.JobCustomerCode.Trim();
                query = query.Where(x => x.JobCustomerCode == code);
            }

            if (!string.IsNullOrWhiteSpace(filter.JobCustomerName))
            {
                var name = filter.JobCustomerName.Trim();
                query = query.Where(x => x.JobCustomerName.Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(filter.InvoiceCustomerCode))
            {
                var code = filter.InvoiceCustomerCode.Trim();
                query = query.Where(x => x.InvoiceCustomerCode == code);
            }

            if (!string.IsNullOrWhiteSpace(filter.InvoiceCustomerName))
            {
                var name = filter.InvoiceCustomerName.Trim();
                query = query.Where(x => x.InvoiceCustomerName.Contains(name));
            }

            if (filter.ReferenceStart.HasValue)
            {
                query = query.Where(x => x.Reference >= filter.ReferenceStart.Value);
            }

            if (filter.ReferenceEnd.HasValue)
            {
                query = query.Where(x => x.Reference <= filter.ReferenceEnd.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                var name = filter.Name.Trim();
                query = query.Where(x => x.Name.Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(filter.RelatedPerson))
            {
                var relatedPerson = filter.RelatedPerson.Trim();
                query = query.Where(x => x.RelatedPerson.Contains(relatedPerson));
            }

            if (filter.IssueDateStart.HasValue)
            {
                var startDate = filter.IssueDateStart.Value.Date;
                query = query.Where(x => x.IssueDate.HasValue && x.IssueDate.Value >= startDate);
            }

            if (filter.IssueDateEnd.HasValue)
            {
                var endDateExclusive = filter.IssueDateEnd.Value.Date.AddDays(1);
                query = query.Where(x => x.IssueDate.HasValue && x.IssueDate.Value < endDateExclusive);
            }

            if (filter.StateId.HasValue)
            {
                query = query.Where(x => x.StateId == filter.StateId.Value);
            }

            if (filter.Evaluated.HasValue)
            {
                query = query.Where(x => x.Evaluated == filter.Evaluated.Value);
            }

            return query;
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
                .AsNoTracking()
                .Where(j => snapshot.AllowedFunctionIds.Contains(j.FunctionID));

            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound && snapshot.CompanyId.HasValue)
            {
                query = query.Where(j => j.CompanyID == snapshot.CompanyId.Value);
            }

            return query;
        }

        private IQueryable<Invoice> BuildScopedInvoiceQuery(PermissionSnapshotDto snapshot)
        {
            var invoiceQuery = _unitOfWork.Repository<Invoice>().Query().AsNoTracking();
            return ApplyInvoiceDataScope(invoiceQuery, snapshot);
        }

        private IQueryable<Invoice> ApplyInvoiceDataScope(IQueryable<Invoice> invoiceQuery, PermissionSnapshotDto snapshot)
        {
            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound && snapshot.CompanyId.HasValue)
            {
                invoiceQuery = invoiceQuery.Where(i => i.CompanyID == snapshot.CompanyId.Value);
            }

            var jobs = _unitOfWork.Repository<Job>().Query().AsNoTracking()
                .Where(j => snapshot.AllowedFunctionIds.Contains(j.FunctionID));

            if (snapshot.CompanyScopeMode == CompanyScopeMode.CompanyBound && snapshot.CompanyId.HasValue)
            {
                jobs = jobs.Where(j => j.CompanyID == snapshot.CompanyId.Value);
            }

            var activeLines = _unitOfWork.Repository<InvoLine>().Query()
                .AsNoTracking()
                .Where(line => line.Deleted == 0);

            var activeInvoJobs = _unitOfWork.Repository<InvoJob>().Query()
                .AsNoTracking()
                .Where(invoJob => invoJob.Deleted == 0);

            // Legacy-compatible behavior:
            // 1) Invoices linked to at least one allowed job via InvoJob are visible.
            // 2) Invoices with no active InvoJob link at all are also visible.
            var invoiceIdsWithAllowedJobs =
                (from inv in invoiceQuery
                 join line in activeLines on inv.Id equals line.InvoiceID
                 join invoJob in activeInvoJobs on line.Id equals invoJob.InvoLineID
                 join job in jobs on invoJob.JobID equals job.Id
                 select inv.Id)
                .Distinct();

            var invoiceIdsWithoutAnyInvoJob =
                (from inv in invoiceQuery
                 where !(from line in activeLines
                         join invoJob in activeInvoJobs on line.Id equals invoJob.InvoLineID
                         where line.InvoiceID == inv.Id
                         select invoJob.Id).Any()
                 select inv.Id);

            var scopedInvoiceIds = invoiceIdsWithAllowedJobs
                .Union(invoiceIdsWithoutAnyInvoJob)
                .Distinct();

            return invoiceQuery.Where(i => scopedInvoiceIds.Contains(i.Id));
        }

        private sealed class InvoiceReportBaseRow
        {
            public string JobCustomerCode { get; init; } = string.Empty;
            public string JobCustomerName { get; init; } = string.Empty;
            public string InvoiceCustomerCode { get; init; } = string.Empty;
            public string InvoiceCustomerName { get; init; } = string.Empty;
            public string CustomerCode { get; init; } = string.Empty;
            public string CustomerName { get; init; } = string.Empty;
            public int Reference { get; init; }
            public string Name { get; init; } = string.Empty;
            public string RelatedPerson { get; init; } = string.Empty;
            public DateTime? IssueDate { get; init; }
            public decimal StateId { get; init; }
            public string StatusName { get; init; } = string.Empty;
            public bool Evaluated { get; init; }
            public decimal TaxAmount { get; init; }
            public decimal SubTotal { get; init; }
            public decimal NetTotal { get; init; }
        }

        private sealed class InvoiceWindowRow
        {
            public decimal Id { get; init; }
            public int Reference { get; init; }
            public decimal JobCustomerID { get; init; }
            public decimal InvoCustomerID { get; init; }
            public string Name { get; init; } = string.Empty;
            public DateTime? IssueDate { get; init; }
            public decimal GrossAmount { get; init; }
            public decimal StateID { get; init; }
            public bool Evaluated { get; init; }
        }

    }
}
