using Imaj.Core.Constants;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Service.Results;
using Imaj.Web.Authorization;
using Imaj.Web.Controllers.Base;
using Imaj.Web;
using Imaj.Web.Models;
using Imaj.Web.Services.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Imaj.Web.Controllers
{
    public class InvoiceController : BaseController
    {
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private static readonly TimeSpan ReportExecutionTimeout = TimeSpan.FromSeconds(45);
        private static readonly decimal[] InvoiceStateDisplayOrder = { 210m, 220m, 230m, 240m, 250m };
        private const decimal ConfirmMethodId = 1385m;
        private const decimal UndoConfirmMethodId = 1386m;
        private const decimal IssueMethodId = 1388m;
        private const decimal KillMethodId = 1389m;
        private const decimal DiscardMethodId = 1387m;
        private const decimal EvaluateMethodId = 1390m;
        private const decimal UndoEvaluateMethodId = 1490m;

        private readonly IInvoiceService _invoiceService;
        private readonly ILookupService _lookupService;
        private readonly IInvoiceReportExcelService _invoiceReportExcelService;
        private readonly IPermissionViewService _permissionViewService;

        public InvoiceController(
            IInvoiceService invoiceService,
            ILookupService lookupService,
            IInvoiceReportExcelService invoiceReportExcelService,
            IPermissionViewService permissionViewService,
            ILogger<InvoiceController> logger, IStringLocalizer<SharedResource> localizer) : base(logger, localizer)
        {
            _invoiceService = invoiceService;
            _lookupService = lookupService;
            _invoiceReportExcelService = invoiceReportExcelService;
            _permissionViewService = permissionViewService;
        }

        public async Task<IActionResult> Index()
        {
            var statesResult = await _lookupService.GetStatesAsync(StateCategories.Invoice);
            ViewBag.InvoiceStates = statesResult.IsSuccess && statesResult.Data != null
                ? statesResult.Data
                    .Where(x => InvoiceStateDisplayOrder.Contains(x.Id))
                    .OrderBy(x => Array.IndexOf(InvoiceStateDisplayOrder, x.Id))
                    .ToList()
                : new List<Imaj.Service.DTOs.StateDto>();

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

            var serviceFilter = BuildFilter(f, includeFirst: true);
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

            var serviceFilter = BuildFilter(f, includeFirst: true);

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
        [RequireMethodPermission(1360)]
        public async Task<IActionResult> DownloadDetailedInvoiceReportExcel([FromQuery] InvoiceViewModel? filter)
        {
            var f = filter ?? new InvoiceViewModel();
            if (!TryValidateIssueDateRange(f, out var badRequestResult))
            {
                return badRequestResult!;
            }

            var reportFilter = BuildFilter(f, includeFirst: false);
            ServiceResult<List<InvoiceDetailedReportRowDto>> reportResult;

            try
            {
                using var cts = new CancellationTokenSource(ReportExecutionTimeout);
                reportResult = await _invoiceService.GetDetailedInvoiceReportAsync(reportFilter, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, L("ReportRequestTimedOut"));
            }

            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(Imaj.Web.Extensions.ControllerMessageLocalizationExtensions.LocalizeUiMessage(this, reportResult.Message, L("ReportDataUnavailable")));
            }

            var fileBytes = _invoiceReportExcelService.BuildDetailedReport(reportResult.Data);
            return File(fileBytes, ExcelContentType, BuildFileName(L("DetailedInvoiceReportFilePrefix")));
        }

        [HttpGet]
        [RequireMethodPermission(1360)]
        public async Task<IActionResult> DownloadSummaryInvoiceReportExcel([FromQuery] InvoiceViewModel? filter)
        {
            var f = filter ?? new InvoiceViewModel();
            if (!TryValidateIssueDateRange(f, out var badRequestResult))
            {
                return badRequestResult!;
            }

            var reportFilter = BuildFilter(f, includeFirst: false);
            ServiceResult<List<InvoiceSummaryReportRowDto>> reportResult;

            try
            {
                using var cts = new CancellationTokenSource(ReportExecutionTimeout);
                reportResult = await _invoiceService.GetSummaryInvoiceReportAsync(reportFilter, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, L("ReportRequestTimedOut"));
            }

            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(Imaj.Web.Extensions.ControllerMessageLocalizationExtensions.LocalizeUiMessage(this, reportResult.Message, L("ReportDataUnavailable")));
            }

            var fileBytes = _invoiceReportExcelService.BuildSummaryReport(reportResult.Data);
            return File(fileBytes, ExcelContentType, BuildFileName(L("SummaryInvoiceReportFilePrefix")));
        }

        [HttpGet]
        [RequireMethodPermission(1360)]
        public async Task<IActionResult> ViewDetailedInvoiceReport([FromQuery] InvoiceViewModel? filter)
        {
            var f = filter ?? new InvoiceViewModel();
            if (!TryValidateIssueDateRange(f, out var badRequestResult))
            {
                return badRequestResult!;
            }

            var reportFilter = BuildFilter(f, includeFirst: false);
            ServiceResult<List<InvoiceDetailedReportRowDto>> reportResult;

            try
            {
                using var cts = new CancellationTokenSource(ReportExecutionTimeout);
                reportResult = await _invoiceService.GetDetailedInvoiceReportAsync(reportFilter, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, L("ReportRequestTimedOut"));
            }

            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(Imaj.Web.Extensions.ControllerMessageLocalizationExtensions.LocalizeUiMessage(this, reportResult.Message, L("ReportDataUnavailable")));
            }

            var model = BuildDetailedPrintableReport(reportResult.Data, f);
            return View("~/Views/Shared/PrintableReport.cshtml", model);
        }

        [HttpGet]
        [RequireMethodPermission(1360)]
        public async Task<IActionResult> ViewSummaryInvoiceReport([FromQuery] InvoiceViewModel? filter)
        {
            var f = filter ?? new InvoiceViewModel();
            if (!TryValidateIssueDateRange(f, out var badRequestResult))
            {
                return badRequestResult!;
            }

            var reportFilter = BuildFilter(f, includeFirst: false);
            ServiceResult<List<InvoiceSummaryReportRowDto>> reportResult;

            try
            {
                using var cts = new CancellationTokenSource(ReportExecutionTimeout);
                reportResult = await _invoiceService.GetSummaryInvoiceReportAsync(reportFilter, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, L("ReportRequestTimedOut"));
            }

            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(Imaj.Web.Extensions.ControllerMessageLocalizationExtensions.LocalizeUiMessage(this, reportResult.Message, L("ReportDataUnavailable")));
            }

            var model = BuildSummaryPrintableReport(reportResult.Data, f);
            return View("~/Views/Shared/PrintableReport.cshtml", model);
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
                SourceView = "Detail",
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
                SourceView = "Summary",
                ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/Invoice/Results" : returnUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WorkflowAction(InvoiceWorkflowActionRequest request)
        {
            if (request.Reference <= 0)
            {
                ShowError(L("HistoryRecordNotFound"));
                return RedirectToAction("Results");
            }

            var methodId = ResolveWorkflowMethodId(request.Action);
            if (!methodId.HasValue)
            {
                ShowError(L("GenericError"));
                return RedirectToInvoiceSource(request);
            }

            var hasPermission = await _permissionViewService.CanExecuteMethodAsync(methodId.Value, write: true);
            if (!hasPermission)
            {
                ShowError(L("AccessDeniedMessage"));
                return RedirectToInvoiceSource(request);
            }

            var result = await _invoiceService.ExecuteWorkflowActionAsync(request.Reference, request.Action, request.IssueDate);
            if (result.IsSuccess)
            {
                ShowSuccess(result.Message ?? L("SuccessTitle"));
            }
            else
            {
                ShowError(result.Message ?? L("GenericError"));
            }

            return RedirectToInvoiceSource(request);
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
        public async Task<IActionResult> Create(string jobCustomerCode, string jobCustomerName)
        {
            var nextReferenceResult = await _invoiceService.GetNextReferenceAsync();
            var model = new InvoiceCreateViewModel
            {
                Reference = nextReferenceResult.IsSuccess && nextReferenceResult.Data > 0
                    ? nextReferenceResult.Data.ToString(CultureInfo.InvariantCulture)
                    : new Random().Next(10000, 99999).ToString(CultureInfo.InvariantCulture),
                JobCustomerCode = jobCustomerCode ?? "101PROD",
                JobCustomerName = jobCustomerName ?? "101 PRODUCTION",
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(1360, write: true)]
        public async Task<IActionResult> Save(InvoiceCreateViewModel model)
        {
            model.IssueDate = model.IssueDate == default ? DateTime.Today : model.IssueDate;
            model.Footnote ??= string.Empty;
            model.Lines ??= new List<InvoiceLineViewModel>();

            var result = await _invoiceService.CreateAsync(BuildCreateRequest(model));
            if (result.IsSuccess && result.Data > 0)
            {
                ShowSuccess(string.Format(L("InvoiceCreatedWithReference"), result.Data));
                return RedirectToAction("Detail", new
                {
                    reference = result.Data.ToString(CultureInfo.InvariantCulture),
                    returnUrl = "/Invoice"
                });
            }

            if (result.Errors.Any())
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, Ui(error, error));
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, Ui(result.Message, L("SaveError")));
            }

            if (string.IsNullOrWhiteSpace(model.Reference))
            {
                var nextReferenceResult = await _invoiceService.GetNextReferenceAsync();
                if (nextReferenceResult.IsSuccess && nextReferenceResult.Data > 0)
                {
                    model.Reference = nextReferenceResult.Data.ToString(CultureInfo.InvariantCulture);
                }
            }

            return View("Create", model);
        }

        private static InvoiceFilterDto BuildFilter(InvoiceViewModel f, bool includeFirst)
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
                First = includeFirst ? f.First : null
            };
        }

        private static InvoiceCreateDto BuildCreateRequest(InvoiceCreateViewModel model)
        {
            return new InvoiceCreateDto
            {
                JobCustomerCode = model.JobCustomerCode,
                JobCustomerName = model.JobCustomerName,
                InvoiceCustomerCode = model.InvoiceCustomerCode,
                InvoiceCustomerName = model.InvoiceCustomerName,
                Name = model.Ad,
                RelatedPerson = model.RelatedPerson,
                IssueDate = model.IssueDate,
                Evaluated = model.IsEvaluated,
                Notes = model.Notes,
                FooterNote = model.Footnote,
                Lines = model.Lines.Select(line => new InvoiceCreateLineDto
                {
                    Description = line.Description,
                    Amount = line.Amount,
                    VatRate = line.VatRate
                }).ToList()
            };
        }

        private PrintableReportViewModel BuildDetailedPrintableReport(
            List<InvoiceDetailedReportRowDto> rows,
            InvoiceViewModel filter)
        {
            var orderedRows = rows
                .OrderBy(x => x.CustomerName)
                .ThenBy(x => x.IssueDate)
                .ThenBy(x => x.Reference)
                .ToList();

            var reportRows = new List<PrintableReportRow>();
            foreach (var customerGroup in orderedRows.GroupBy(x => new { x.CustomerCode, x.CustomerName }))
            {
                var customerDisplay = ResolveCustomerDisplay(customerGroup.Key.CustomerName, customerGroup.Key.CustomerCode);
                foreach (var row in customerGroup)
                {
                    reportRows.Add(new PrintableReportRow
                    {
                        Cells = new List<PrintableReportCell>
                        {
                            new() { Value = customerDisplay },
                            new() { Value = row.Reference.ToString(CultureInfo.CurrentCulture), Alignment = "right" },
                            new() { Value = row.Name },
                            new() { Value = row.IssueDate.HasValue ? FormatDate(row.IssueDate.Value) : "-", Alignment = "center" },
                            new() { Value = row.StatusName },
                            new() { IsCheckbox = true, IsChecked = row.Evaluated, Alignment = "center" },
                            new() { Value = FormatAmount(row.TaxAmount), Alignment = "right" },
                            new() { Value = FormatAmount(row.SubTotal), Alignment = "right" },
                            new() { Value = FormatAmount(row.NetTotal), Alignment = "right" }
                        }
                    });
                }

                reportRows.Add(new PrintableReportRow
                {
                    Kind = PrintableReportRowKind.GroupTotal,
                    Cells = new List<PrintableReportCell>
                    {
                        new()
                        {
                            Value = string.Format(L("CustomerTotalFormat"), customerDisplay),
                            ColSpan = 6,
                            Alignment = "right"
                        },
                        new() { Value = FormatAmount(customerGroup.Sum(x => x.TaxAmount)), Alignment = "right" },
                        new() { Value = FormatAmount(customerGroup.Sum(x => x.SubTotal)), Alignment = "right" },
                        new() { Value = FormatAmount(customerGroup.Sum(x => x.NetTotal)), Alignment = "right" }
                    }
                });
            }

            if (orderedRows.Any())
            {
                reportRows.Add(new PrintableReportRow
                {
                    Kind = PrintableReportRowKind.GrandTotal,
                    Cells = new List<PrintableReportCell>
                    {
                        new() { Value = L("ReportTotal"), ColSpan = 6, Alignment = "right" },
                        new() { Value = FormatAmount(orderedRows.Sum(x => x.TaxAmount)), Alignment = "right" },
                        new() { Value = FormatAmount(orderedRows.Sum(x => x.SubTotal)), Alignment = "right" },
                        new() { Value = FormatAmount(orderedRows.Sum(x => x.NetTotal)), Alignment = "right" }
                    }
                });
            }

            return new PrintableReportViewModel
            {
                Title = L("DetailedInvoiceReportTitle"),
                Orientation = "landscape",
                GeneratedAtDisplay = BuildGeneratedAtDisplay(),
                EmptyMessage = L("NoRecordsFound"),
                MetaItems = BuildInvoiceMetaItems(filter),
                Columns = new List<PrintableReportColumn>
                {
                    new() { Title = L("Customer") },
                    new() { Title = L("Reference"), Alignment = "right" },
                    new() { Title = L("Name") },
                    new() { Title = L("Date"), Alignment = "center" },
                    new() { Title = L("Status") },
                    new() { Title = L("Evaluated"), Alignment = "center" },
                    new() { Title = L("TaxAmount"), Alignment = "right" },
                    new() { Title = L("SubTotal"), Alignment = "right" },
                    new() { Title = L("NetAmount"), Alignment = "right" }
                },
                Rows = reportRows
            };
        }

        private PrintableReportViewModel BuildSummaryPrintableReport(
            List<InvoiceSummaryReportRowDto> rows,
            InvoiceViewModel filter)
        {
            var orderedRows = rows
                .OrderBy(x => x.CustomerName)
                .ToList();

            var reportRows = orderedRows
                .Select(row => new PrintableReportRow
                {
                    Cells = new List<PrintableReportCell>
                    {
                        new() { Value = ResolveCustomerDisplay(row.CustomerName, row.CustomerCode) },
                        new() { Value = row.Count.ToString("N0", CultureInfo.CurrentCulture), Alignment = "right" },
                        new() { Value = FormatAmount(row.TaxAmount), Alignment = "right" },
                        new() { Value = FormatAmount(row.SubTotal), Alignment = "right" },
                        new() { Value = FormatAmount(row.NetTotal), Alignment = "right" }
                    }
                })
                .ToList();

            if (orderedRows.Any())
            {
                reportRows.Add(new PrintableReportRow
                {
                    Kind = PrintableReportRowKind.GrandTotal,
                    Cells = new List<PrintableReportCell>
                    {
                        new() { Value = L("ReportTotal"), Alignment = "right" },
                        new() { Value = orderedRows.Sum(x => x.Count).ToString("N0", CultureInfo.CurrentCulture), Alignment = "right" },
                        new() { Value = FormatAmount(orderedRows.Sum(x => x.TaxAmount)), Alignment = "right" },
                        new() { Value = FormatAmount(orderedRows.Sum(x => x.SubTotal)), Alignment = "right" },
                        new() { Value = FormatAmount(orderedRows.Sum(x => x.NetTotal)), Alignment = "right" }
                    }
                });
            }

            return new PrintableReportViewModel
            {
                Title = L("SummaryInvoiceReportTitle"),
                Orientation = "portrait",
                GeneratedAtDisplay = BuildGeneratedAtDisplay(),
                EmptyMessage = L("NoRecordsFound"),
                MetaItems = BuildInvoiceMetaItems(filter),
                Columns = new List<PrintableReportColumn>
                {
                    new() { Title = L("Customer") },
                    new() { Title = L("Count"), Alignment = "right" },
                    new() { Title = L("TaxAmount"), Alignment = "right" },
                    new() { Title = L("SubTotal"), Alignment = "right" },
                    new() { Title = L("NetAmount"), Alignment = "right" }
                },
                Rows = reportRows
            };
        }

        private List<PrintableReportMetaItem> BuildInvoiceMetaItems(InvoiceViewModel filter)
        {
            var items = new List<PrintableReportMetaItem>();

            if (filter.IssueDateStart.HasValue && filter.IssueDateEnd.HasValue)
            {
                items.Add(new PrintableReportMetaItem
                {
                    Label = L("DateRange"),
                    Value = $"{FormatDate(filter.IssueDateStart.Value)} - {FormatDate(filter.IssueDateEnd.Value)}"
                });
            }

            if (!string.IsNullOrWhiteSpace(filter.JobCustomerCode) || !string.IsNullOrWhiteSpace(filter.JobCustomerName))
            {
                items.Add(new PrintableReportMetaItem
                {
                    Label = L("JobCustomer"),
                    Value = ResolveDisplay(filter.JobCustomerName, filter.JobCustomerCode)
                });
            }

            if (!string.IsNullOrWhiteSpace(filter.InvoiceCustomerCode) || !string.IsNullOrWhiteSpace(filter.InvoiceCustomerName))
            {
                items.Add(new PrintableReportMetaItem
                {
                    Label = L("InvoiceCustomer"),
                    Value = ResolveDisplay(filter.InvoiceCustomerName, filter.InvoiceCustomerCode)
                });
            }

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                items.Add(new PrintableReportMetaItem
                {
                    Label = L("Name"),
                    Value = filter.Name
                });
            }

            if (!string.IsNullOrWhiteSpace(filter.RelatedPerson))
            {
                items.Add(new PrintableReportMetaItem
                {
                    Label = L("Related"),
                    Value = filter.RelatedPerson
                });
            }

            return items;
        }

        private string BuildGeneratedAtDisplay()
        {
            return DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
        }

        private string ResolveDisplay(string? primary, string? fallback)
        {
            if (!string.IsNullOrWhiteSpace(primary))
            {
                return primary!;
            }

            if (!string.IsNullOrWhiteSpace(fallback))
            {
                return fallback!;
            }

            return L("AllOption");
        }

        private static string ResolveCustomerDisplay(string? name, string? code)
        {
            return string.IsNullOrWhiteSpace(name) ? (code ?? string.Empty) : name;
        }

        private static string FormatDate(DateTime value)
        {
            return value.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture);
        }

        private static string FormatAmount(decimal value)
        {
            return value.ToString("N2", CultureInfo.CurrentCulture);
        }

        private bool TryValidateIssueDateRange(InvoiceViewModel filter, out IActionResult? badRequestResult)
        {
            badRequestResult = null;

            var hasStartDate = filter.IssueDateStart.HasValue;
            var hasEndDate = filter.IssueDateEnd.HasValue;

            if (hasStartDate != hasEndDate)
            {
                badRequestResult = new BadRequestObjectResult(L("IssueDateRangeInvalid"));
                return false;
            }

            if (!hasStartDate)
            {
                badRequestResult = new BadRequestObjectResult(L("StartEndDateRequired"));
                return false;
            }

            if (filter.IssueDateEnd!.Value.Date < filter.IssueDateStart!.Value.Date)
            {
                badRequestResult = new BadRequestObjectResult(L("EndDateBeforeStart"));
                return false;
            }

            return true;
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
                StateId = invoice.StateId,
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

        private IActionResult RedirectToInvoiceSource(InvoiceWorkflowActionRequest request)
        {
            var targetAction = string.Equals(request.SourceView, "Summary", StringComparison.OrdinalIgnoreCase)
                ? "Summary"
                : "Detail";

            var selectedReferences = request.SelectedReferences?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList() ?? new List<string>();

            var referenceText = request.Reference.ToString();
            if (!selectedReferences.Contains(referenceText))
            {
                selectedReferences.Add(referenceText);
            }

            var currentIndex = request.CurrentIndex;
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }
            if (currentIndex >= selectedReferences.Count)
            {
                currentIndex = selectedReferences.Count - 1;
            }

            var returnUrl = string.IsNullOrWhiteSpace(request.ReturnUrl) || !request.ReturnUrl.StartsWith('/')
                ? "/Invoice/Results"
                : request.ReturnUrl;

            return RedirectToAction(targetAction, new
            {
                reference = referenceText,
                selectedReferences,
                page = currentIndex + 1,
                returnUrl
            });
        }

        private static decimal? ResolveWorkflowMethodId(InvoiceWorkflowAction action)
        {
            return action switch
            {
                InvoiceWorkflowAction.Confirm => ConfirmMethodId,
                InvoiceWorkflowAction.UndoConfirm => UndoConfirmMethodId,
                InvoiceWorkflowAction.Issue => IssueMethodId,
                InvoiceWorkflowAction.Kill => KillMethodId,
                InvoiceWorkflowAction.Discard => DiscardMethodId,
                InvoiceWorkflowAction.Evaluate => EvaluateMethodId,
                InvoiceWorkflowAction.UndoEvaluate => UndoEvaluateMethodId,
                _ => null
            };
        }

        private static string BuildFileName(string prefix)
        {
            return $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
        }
    }
}
