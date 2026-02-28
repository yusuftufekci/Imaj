using Imaj.Core.Constants;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Service.Results;
using Imaj.Web.Authorization;
using Imaj.Web.Controllers.Base;
using Imaj.Web;
using Imaj.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Imaj.Web.Controllers
{
    public class InvoiceController : BaseController
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ILookupService _lookupService;

        public InvoiceController(
            IInvoiceService invoiceService,
            ILookupService lookupService,
            ILogger<InvoiceController> logger, IStringLocalizer<SharedResource> localizer) : base(logger, localizer)
        {
            _invoiceService = invoiceService;
            _lookupService = lookupService;
        }

        public async Task<IActionResult> Index()
        {
            var statesResult = await _lookupService.GetStatesAsync(StateCategories.Invoice);
            ViewBag.InvoiceStates = statesResult.IsSuccess ? statesResult.Data : new List<Imaj.Service.DTOs.StateDto>();

            var model = new InvoiceViewModel
            {
                IssueDateStart = DateTime.Now.AddDays(-30),
                IssueDateEnd = DateTime.Now
            };
            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(1360)]
        public async Task<IActionResult> Results([FromQuery] InvoiceViewModel? filter)
        {
            var f = filter ?? new InvoiceViewModel();

            if (!f.IssueDateStart.HasValue && !f.IssueDateEnd.HasValue)
            {
                f.IssueDateStart = DateTime.Now.AddDays(-30);
                f.IssueDateEnd = DateTime.Now;
            }

            f.Page = f.Page <= 0 ? 1 : f.Page;
            f.PageSize = f.PageSize <= 0 ? 10 : f.PageSize;
            f.First = f.First.HasValue && f.First.Value > 0 ? f.First.Value : f.PageSize;

            var serviceFilter = BuildFilter(f);
            var result = await _invoiceService.GetByFilterAsync(serviceFilter);

            f.Items = MapItems(result);
            f.TotalCount = result.IsSuccess && result.Data != null ? result.Data.TotalCount : 0;
            f.Page = result.Data?.PageNumber ?? f.Page;
            f.PageSize = result.Data?.PageSize ?? f.PageSize;

            return View(f);
        }

        [HttpPost]
        [RequireMethodPermission(1360)]
        public async Task<IActionResult> Search([FromBody] InvoiceViewModel? filter)
        {
            var f = filter ?? new InvoiceViewModel();

            var serviceFilter = BuildFilter(f);

            var result = await _invoiceService.GetByFilterAsync(serviceFilter);

            var items = MapItems(result);
            var totalCount = result.IsSuccess && result.Data != null ? result.Data.TotalCount : 0;

            return Json(new
            {
                items,
                totalCount,
                page = result.Data?.PageNumber ?? f.Page,
                pageSize = result.Data?.PageSize ?? f.PageSize
            });
        }

        [HttpGet]
        public async Task<IActionResult> Detail(string? reference, [FromQuery] string[]? selectedReferences, int page = 1, string? returnUrl = null)
        {
            var refs = NormalizeReferences(reference, selectedReferences);
            var (currentIndex, currentRef) = ResolveCurrentReference(refs, reference, page);

            var references = new List<int>();
            if (!string.IsNullOrWhiteSpace(currentRef) && int.TryParse(currentRef, out var refValue))
            {
                references.Add(refValue);
            }

            var result = await _invoiceService.GetDetailsByReferencesAsync(references);
            var model = new InvoiceDisplayViewModel
            {
                Invoices = MapDetails(result.Data ?? new List<InvoiceDetailDto>()),
                SelectedReferences = refs,
                CurrentIndex = currentIndex,
                ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/Invoice/Results" : returnUrl
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Summary(string? reference, [FromQuery] string[]? selectedReferences, int page = 1, string? returnUrl = null)
        {
            var refs = NormalizeReferences(reference, selectedReferences);
            var (currentIndex, currentRef) = ResolveCurrentReference(refs, reference, page);

            var references = new List<int>();
            if (!string.IsNullOrWhiteSpace(currentRef) && int.TryParse(currentRef, out var refValue))
            {
                references.Add(refValue);
            }

            var result = await _invoiceService.GetDetailsByReferencesAsync(references);
            var model = new InvoiceDisplayViewModel
            {
                Invoices = MapDetails(result.Data ?? new List<InvoiceDetailDto>()),
                SelectedReferences = refs,
                CurrentIndex = currentIndex,
                ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/Invoice/Results" : returnUrl
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> History(string? reference, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(reference) || !int.TryParse(reference, out var refValue))
            {
                return RedirectToAction("Results");
            }

            var historyResult = await _invoiceService.GetHistoryByReferenceAsync(refValue);
            if (!historyResult.IsSuccess || historyResult.Data?.Detail == null)
            {
                return RedirectToAction("Results");
            }

            var detail = MapDetails(new List<InvoiceDetailDto> { historyResult.Data.Detail }).FirstOrDefault()
                ?? new InvoiceDetailViewModel();

            var model = new InvoiceHistoryViewModel
            {
                Invoice = detail,
                Items = historyResult.Data.Items.Select(x => new InvoiceHistoryItem
                {
                    Date = x.LogDate,
                    UserCode = x.UserCode,
                    UserName = x.UserName,
                    Action = x.ActionName
                }).ToList(),
                ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/Invoice/Detail?reference=" + reference : returnUrl
            };

            return View("History", model);
        }

        [HttpGet]
        public IActionResult Create(string jobCustomerCode, string jobCustomerName)
        {
            var model = new InvoiceCreateViewModel
            {
                Reference = new Random().Next(10000, 99999).ToString(),
                JobCustomerCode = jobCustomerCode ?? "101PROD",
                JobCustomerName = jobCustomerName ?? "101 PRODUCTION",
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult Save(InvoiceCreateViewModel model)
        {
            return RedirectToAction("Index");
        }

        private static InvoiceFilterDto BuildFilter(InvoiceViewModel f)
        {
            return new InvoiceFilterDto
            {
                JobCustomerCode = f.JobCustomerCode,
                JobCustomerName = f.JobCustomerName,
                InvoiceCustomerCode = f.InvoiceCustomerCode,
                InvoiceCustomerName = f.InvoiceCustomerName,
                ReferenceStart = int.TryParse(f.ReferenceStart, out var refStart) ? refStart : null,
                ReferenceEnd = int.TryParse(f.ReferenceEnd, out var refEnd) ? refEnd : null,
                Name = f.Name,
                RelatedPerson = f.RelatedPerson,
                IssueDateStart = f.IssueDateStart,
                IssueDateEnd = f.IssueDateEnd,
                StateId = decimal.TryParse(f.Status, out var stateId) ? stateId : null,
                Evaluated = f.Evaluated == "true" ? true : f.Evaluated == "false" ? false : null,
                Page = f.Page,
                PageSize = f.PageSize > 0 ? f.PageSize : 10,
                First = f.First
            };
        }

        private static List<InvoiceSearchResult> MapItems(ServiceResult<PagedResult<InvoiceDto>> result)
        {
            return result.IsSuccess && result.Data != null
                ? result.Data.Items.Select(i => new InvoiceSearchResult
                {
                    Reference = i.Reference.ToString(),
                    JobCustomer = i.JobCustomerName ?? i.JobCustomerCode,
                    InvoiceCustomer = i.InvoiceCustomerName ?? i.InvoiceCustomerCode,
                    Name = i.Name,
                    IssueDate = i.IssueDate,
                    Amount = i.GrossAmount,
                    Status = i.StateName,
                    Evaluated = i.Evaluated
                }).ToList()
                : new List<InvoiceSearchResult>();
        }

        private static List<string> NormalizeReferences(string? reference, string[]? selectedReferences)
        {
            var refs = new List<string>();

            if (selectedReferences != null && selectedReferences.Length > 0)
            {
                refs.AddRange(selectedReferences.Where(r => !string.IsNullOrWhiteSpace(r)));
            }

            if (!string.IsNullOrWhiteSpace(reference) && !refs.Contains(reference))
            {
                refs.Insert(0, reference);
            }

            return refs;
        }

        private static (int currentIndex, string? currentRef) ResolveCurrentReference(List<string> refs, string? reference, int page)
        {
            if (refs.Count == 0)
            {
                return (0, reference);
            }

            var index = 0;
            if (page > 0)
            {
                index = page - 1;
            }
            else if (!string.IsNullOrWhiteSpace(reference))
            {
                var found = refs.IndexOf(reference);
                index = found >= 0 ? found : 0;
            }

            if (index < 0) index = 0;
            if (index >= refs.Count) index = refs.Count - 1;

            return (index, refs[index]);
        }

        private static List<InvoiceDetailViewModel> MapDetails(List<InvoiceDetailDto> invoices)
        {
            return invoices.Select(invoice => new InvoiceDetailViewModel
            {
                Reference = invoice.Reference.ToString(),
                JobCustomer = invoice.JobCustomerName ?? "-",
                InvoiceCustomer = invoice.InvoiceCustomerName ?? "-",
                Name = invoice.Name ?? "-",
                RelatedPerson = invoice.RelatedPerson ?? "-",
                IssueDate = invoice.IssueDate,
                Status = invoice.StateName ?? "-",
                Evaluated = invoice.Evaluated,
                Notes = invoice.Notes ?? string.Empty,
                FooterNote = invoice.FooterNote ?? string.Empty,
                Lines = invoice.Lines.Select(l => new InvoiceDetailLineViewModel
                {
                    Selected = l.Selected,
                    LineNo = l.Sequence,
                    Notes = l.Notes ?? string.Empty,
                    Quantity = l.Quantity,
                    Price = l.Price,
                    Amount = l.Amount,
                    TaxType = l.TaxType ?? string.Empty
                }).ToList(),
                WorkItems = invoice.Jobs.Select(j => new InvoiceWorkItemViewModel
                {
                    Selected = j.Selected,
                    Reference = j.Reference.ToString(),
                    Name = j.Name ?? string.Empty,
                    Amount = j.Amount
                }).ToList(),
                ProductCategories = invoice.ProductCategories.Select(p => new InvoiceCategorySummaryViewModel
                {
                    Name = p.Name ?? string.Empty,
                    SubTotal = p.SubTotal,
                    NetTotal = p.NetTotal
                }).ToList(),
                Taxes = invoice.Taxes.Select(t => new InvoiceTaxSummaryViewModel
                {
                    Code = t.Code ?? string.Empty,
                    Name = t.Name ?? string.Empty,
                    SubTotal = t.SubTotal,
                    Rate = t.Rate,
                    TaxAmount = t.TaxAmount,
                    NetTotal = t.NetTotal
                }).ToList()
            }).ToList();
        }
    }
}
