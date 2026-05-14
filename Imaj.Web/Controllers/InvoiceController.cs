using ClosedXML.Excel;
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
        private const int DefaultSearchLimit = 100;
        private const decimal OpenInvoiceStateId = 210m;
        private const decimal PricedJobStateId = 130m;
        private static readonly decimal[] InvoiceStateDisplayOrder = { 210m, 220m, 230m, 240m, 250m };
        private static readonly decimal[] JobStateDisplayOrder = { 110m, 120m, 130m, 140m, 150m, 160m };
        private const decimal ConfirmMethodId = 1385m;
        private const decimal UndoConfirmMethodId = 1386m;
        private const decimal IssueMethodId = 1388m;
        private const decimal KillMethodId = 1389m;
        private const decimal DiscardMethodId = 1387m;
        private const decimal EvaluateMethodId = 1390m;
        private const decimal UndoEvaluateMethodId = 1490m;

        private readonly IInvoiceService _invoiceService;
        private readonly IJobService _jobService;
        private readonly ILookupService _lookupService;
        private readonly IInvoiceReportExcelService _invoiceReportExcelService;
        private readonly IPermissionViewService _permissionViewService;

        public InvoiceController(
            IInvoiceService invoiceService,
            IJobService jobService,
            ILookupService lookupService,
            IInvoiceReportExcelService invoiceReportExcelService,
            IPermissionViewService permissionViewService,
            ILogger<InvoiceController> logger, IStringLocalizer<SharedResource> localizer) : base(logger, localizer)
        {
            _invoiceService = invoiceService;
            _jobService = jobService;
            _lookupService = lookupService;
            _invoiceReportExcelService = invoiceReportExcelService;
            _permissionViewService = permissionViewService;
        }

        public async Task<IActionResult> Index([FromQuery] InvoiceViewModel? filter)
        {
            var statesResult = await _lookupService.GetStatesAsync(StateCategories.Invoice);
            ViewBag.InvoiceStates = statesResult.IsSuccess && statesResult.Data != null
                ? statesResult.Data
                    .Where(x => InvoiceStateDisplayOrder.Contains(x.Id))
                    .OrderBy(x => Array.IndexOf(InvoiceStateDisplayOrder, x.Id))
                    .ToList()
                : new List<Imaj.Service.DTOs.StateDto>();

            var model = filter ?? new InvoiceViewModel();
            var hasQuery = Request.Query.Count > 0;
            if (!hasQuery)
            {
                // IssueDateStart/End string tipinde tutulur; HTML date input'u yyyy-MM-dd bekler
                model.IssueDateStart = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                model.IssueDateEnd = DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            }

            model.First = model.First.HasValue && model.First.Value > 0 ? model.First : DefaultSearchLimit;
            model.PageSize = model.PageSize > 0 ? model.PageSize : 16;

            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(1360)]
        public async Task<IActionResult> Results([FromQuery] InvoiceViewModel? filter)
        {
            var f = filter ?? new InvoiceViewModel();

            // Tarih alanları string olarak model'de tutulup Parsed* property'leri ile elde ediliyor.
            // Tarih girilmemişse ve başka hiçbir daraltıcı filtre yoksa varsayılan 30 günlük aralık uygula.
            // Durum, müşteri, referans veya diğer filtreler girilmişse tarih zorla eklenmez;
            // aksi takdirde NULL IssueDate'li veya eski tarihli kayıtlar gözden kaçar.
            var hasNonDateFilter =
                !string.IsNullOrWhiteSpace(f.Status) ||
                !string.IsNullOrWhiteSpace(f.JobCustomerCode) ||
                !string.IsNullOrWhiteSpace(f.JobCustomerName) ||
                !string.IsNullOrWhiteSpace(f.InvoiceCustomerCode) ||
                !string.IsNullOrWhiteSpace(f.InvoiceCustomerName) ||
                !string.IsNullOrWhiteSpace(f.ReferenceStart) ||
                !string.IsNullOrWhiteSpace(f.ReferenceEnd) ||
                !string.IsNullOrWhiteSpace(f.ReferenceList) ||
                !string.IsNullOrWhiteSpace(f.Name) ||
                !string.IsNullOrWhiteSpace(f.RelatedPerson) ||
                !string.IsNullOrWhiteSpace(f.Evaluated);

            if (!f.ParsedIssueDateStart.HasValue && !f.ParsedIssueDateEnd.HasValue && !hasNonDateFilter)
            {
                // Hiçbir filtre yoksa son 30 günü varsayılan olarak uygula.
                f.IssueDateStart = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                f.IssueDateEnd = DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            }

            f.Page = f.Page <= 0 ? 1 : f.Page;
            f.PageSize = f.PageSize <= 0 ? 10 : f.PageSize;
            f.First = f.First.HasValue && f.First.Value > 0 ? f.First.Value : DefaultSearchLimit;

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

        [HttpGet]
        public async Task<IActionResult> Print(int reference)
        {
            var invoice = await GetInvoiceDetailViewModelAsync(reference);
            if (invoice == null)
            {
                return RedirectToAction("Detail", new { reference });
            }

            var model = BuildInvoicePrintableReport(invoice);
            return View("~/Views/Shared/PrintableReport.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadPrintExcel(int reference)
        {
            var invoice = await GetInvoiceDetailViewModelAsync(reference);
            if (invoice == null)
            {
                return RedirectToAction("Detail", new { reference });
            }

            var fileBytes = BuildInvoicePrintExcel(invoice);
            return File(fileBytes, ExcelContentType, BuildInvoiceFileName("invoice", reference));
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
            if (!hasPermission && request.Action == InvoiceWorkflowAction.Kill)
            {
                hasPermission = await _permissionViewService.CanExecuteMethodAsync(DiscardMethodId, write: true);
            }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(1360, write: true)]
        public async Task<IActionResult> UpdateDetail(InvoiceUpdateViewModel model)
        {
            var selectedReferences = model.SelectedReferences?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList() ?? new List<string>();

            if (model.Reference <= 0)
            {
                ShowError(L("InvoiceReferenceRequired"));
                return RedirectToAction(nameof(Results));
            }

            var referenceText = model.Reference.ToString(CultureInfo.InvariantCulture);
            if (!selectedReferences.Contains(referenceText))
            {
                selectedReferences.Add(referenceText);
            }

            var result = await _invoiceService.UpdateOpenInvoiceAsync(new InvoiceUpdateDto
            {
                Reference = model.Reference,
                InvoiceCustomerCode = model.InvoiceCustomerCode,
                Name = model.Name,
                RelatedPerson = model.RelatedPerson,
                Notes = model.Notes,
                FooterNote = model.FooterNote,
                Lines = (model.Lines ?? new List<InvoiceUpdateLineViewModel>())
                    .Select(x => new InvoiceUpdateLineDto
                    {
                        Id = x.Id,
                        Notes = x.Notes,
                        Amount = x.Amount,
                        VatRate = x.VatRate
                    })
                    .ToList(),
                NewFreeLines = (model.NewFreeLines ?? new List<InvoiceUpdateFreeLineViewModel>())
                    .Select(x => new InvoiceUpdateFreeLineDto
                    {
                        Description = x.Description,
                        Amount = x.Amount,
                        VatRate = x.VatRate
                    })
                    .ToList(),
                ProductCategories = (model.ProductCategories ?? new List<InvoiceUpdateCategoryViewModel>())
                    .Select(x => new InvoiceUpdateProductCategoryDto
                    {
                        LineId = x.LineId,
                        ProdCatId = x.ProdCatId,
                        NetTotal = x.NetTotal
                    })
                    .ToList(),
                Taxes = (model.Taxes ?? new List<InvoiceUpdateTaxViewModel>())
                    .Select(x => new InvoiceUpdateTaxDto
                    {
                        TaxTypeId = x.TaxTypeId,
                        Rate = x.Rate
                    })
                    .ToList()
            });

            if (result.IsSuccess)
            {
                ShowSuccess(result.Message ?? L("SuccessTitle"));
            }
            else if (result.Errors.Any())
            {
                ShowError(string.Join(" ", result.Errors.Select(error => Ui(error, error))));
            }
            else
            {
                ShowError(result.Message ?? L("SaveError"));
            }

            var currentIndex = model.CurrentIndex;
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }
            if (currentIndex >= selectedReferences.Count)
            {
                currentIndex = selectedReferences.Count - 1;
            }

            var returnUrl = string.IsNullOrWhiteSpace(model.ReturnUrl) || !model.ReturnUrl.StartsWith('/')
                ? "/Invoice/Results"
                : model.ReturnUrl;

            return RedirectToAction(nameof(Detail), new
            {
                reference = referenceText,
                selectedReferences,
                page = currentIndex + 1,
                returnUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(1360, write: true)]
        public async Task<IActionResult> AddJobsToInvoiceLine(InvoiceAddJobsToLineViewModel model)
        {
            var result = await _invoiceService.AddJobsToInvoiceLineAsync(new InvoiceAddJobsToLineDto
            {
                Reference = model.Reference,
                LineId = model.LineId,
                JobReferences = model.JobReferences ?? new List<int>()
            });

            ShowInvoiceMutationResult(result);
            return RedirectToInvoiceDetail(model.Reference, model.SelectedReferences, model.CurrentIndex, model.ReturnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(1360, write: true)]
        public async Task<IActionResult> AddJobsToInvoice(InvoiceAddJobsViewModel model)
        {
            var result = await _invoiceService.AddJobsToInvoiceAsync(new InvoiceAddJobsDto
            {
                Reference = model.Reference,
                Mode = model.Mode,
                JobReferences = model.JobReferences ?? new List<int>()
            });

            ShowInvoiceMutationResult(result);
            return RedirectToInvoiceDetail(model.Reference, model.SelectedReferences, model.CurrentIndex, model.ReturnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(1360, write: true)]
        public async Task<IActionResult> DeleteJobsFromInvoiceLine(InvoiceDeleteJobsFromLineViewModel model)
        {
            var result = await _invoiceService.DeleteJobsFromInvoiceLineAsync(new InvoiceDeleteJobsFromLineDto
            {
                Reference = model.Reference,
                LineId = model.LineId,
                JobReferences = model.JobReferences ?? new List<int>()
            });

            ShowInvoiceMutationResult(result);
            return RedirectToInvoiceDetail(model.Reference, model.SelectedReferences, model.CurrentIndex, model.ReturnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(1360, write: true)]
        public async Task<IActionResult> DeleteJobLines(InvoiceDeleteJobLinesViewModel model)
        {
            var result = await _invoiceService.DeleteJobLinesAsync(new InvoiceDeleteJobLinesDto
            {
                Reference = model.Reference,
                LineIds = model.LineIds ?? new List<decimal>()
            });

            ShowInvoiceMutationResult(result);
            return RedirectToInvoiceDetail(model.Reference, model.SelectedReferences, model.CurrentIndex, model.ReturnUrl);
        }

        [HttpGet]
        [RequireMethodPermission(1360)]
        public async Task<IActionResult> JobPicker(InvoiceJobPickerViewModel model)
        {
            await LoadJobPickerDropdownDataAsync();

            if (model.Reference <= 0)
            {
                return RedirectToAction(nameof(Results));
            }

            var invoice = await GetInvoiceDetailViewModelAsync(model.Reference);
            if (invoice == null)
            {
                return RedirectToAction(nameof(Detail), new { reference = model.Reference.ToString(CultureInfo.InvariantCulture) });
            }

            model.Context = string.Equals(model.Context, "line", StringComparison.OrdinalIgnoreCase)
                ? "line"
                : "invoice";
            model.JobCustomerCode = invoice.JobCustomerCode;
            model.JobCustomerName = invoice.JobCustomer;
            model.First = model.First > 0 ? model.First : 100;
            model.PageSize = model.PageSize > 0 ? model.PageSize : 16;
            model.Page = model.Page > 0 ? model.Page : 1;
            model.ReturnUrl = string.IsNullOrWhiteSpace(model.ReturnUrl) ? "/Invoice/Results" : model.ReturnUrl;
            model.SelectedReferences = model.SelectedReferences?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList() ?? new List<string>();

            var referenceText = model.Reference.ToString(CultureInfo.InvariantCulture);
            if (!model.SelectedReferences.Contains(referenceText))
            {
                model.SelectedReferences.Add(referenceText);
            }

            model.Searched = string.Equals(Request.Query[nameof(model.Searched)], "true", StringComparison.OrdinalIgnoreCase);

            if (model.Searched)
            {
                var result = await _jobService.GetByFilterAsync(BuildJobPickerFilter(model));

                if (result.IsSuccess && result.Data != null)
                {
                    model.Items = result.Data.Items.Select(x => new InvoiceJobPickerItemViewModel
                    {
                        Reference = x.Reference,
                        Function = x.FunctionName ?? string.Empty,
                        Name = x.Name ?? string.Empty,
                        CustomerName = x.CustomerName ?? x.CustomerCode ?? string.Empty,
                        StartDate = x.StartDate,
                        EndDate = x.EndDate,
                        WorkAmount = x.WorkAmount,
                        ProductAmount = x.ProductAmount
                    }).ToList();
                }
                else
                {
                    ShowError(result.Message ?? L("GenericError"));
                }
            }

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
        public async Task<IActionResult> Create(string jobCustomerCode, string jobCustomerName)
        {
            var nextReferenceResult = await _invoiceService.GetNextReferenceAsync();

            var resolvedJobCode = jobCustomerCode ?? "101PROD";
            var resolvedJobName = jobCustomerName ?? "101 PRODUCTION";

            var model = new InvoiceCreateViewModel
            {
                Reference = nextReferenceResult.IsSuccess && nextReferenceResult.Data > 0
                    ? nextReferenceResult.Data.ToString(CultureInfo.InvariantCulture)
                    : new Random().Next(10000, 99999).ToString(CultureInfo.InvariantCulture),
                JobCustomerCode = resolvedJobCode,
                JobCustomerName = resolvedJobName,
                // Fatura müşterisini iş müşterisiyle aynı olarak başlat;
                // kullanıcı Create sayfasında "Seç" butonu ile değiştirebilir.
                InvoiceCustomerCode = resolvedJobCode,
                InvoiceCustomerName = resolvedJobName,
            };
            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(1360)]
        public async Task<IActionResult> PricedJobs([FromQuery] string? jobCustomerCode, [FromQuery] string? search, [FromQuery] int first = 100)
        {
            var result = await _invoiceService.GetPricedJobsForInvoiceAsync(new InvoicePricedJobFilterDto
            {
                JobCustomerCode = jobCustomerCode,
                Search = search,
                First = first
            });

            if (!result.IsSuccess || result.Data == null)
            {
                return BadRequest(new { message = Ui(result.Message, L("GenericError")) });
            }

            return Json(new
            {
                items = result.Data.Select(x => new
                {
                    reference = x.Reference,
                    name = x.Name ?? string.Empty,
                    customerCode = x.CustomerCode ?? string.Empty,
                    customerName = x.CustomerName ?? string.Empty,
                    startDate = x.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    endDate = x.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    productAmount = x.ProductAmount,
                    workAmount = x.WorkAmount
                })
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(1360, write: true)]
        public async Task<IActionResult> Save(InvoiceCreateViewModel model)
        {
            model.IssueDate = model.IssueDate == default ? DateTime.Today : model.IssueDate;
            model.Footnote ??= string.Empty;
            model.Lines ??= new List<InvoiceLineViewModel>();
            model.Jobs ??= new List<InvoiceJobSelectionViewModel>();

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
            var stateId = decimal.TryParse(f.Status, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var parsedStateId)
                    ? parsedStateId
                    : (decimal?)null;
            var ignoreIssueDateFilter = stateId == OpenInvoiceStateId && !HasInvoiceFilterBeyondStatus(f);

            return new InvoiceFilterDto
            {
                JobCustomerCode = f.JobCustomerCode,
                JobCustomerName = f.JobCustomerName,
                InvoiceCustomerCode = f.InvoiceCustomerCode,
                InvoiceCustomerName = f.InvoiceCustomerName,
                ReferenceStart = int.TryParse(f.ReferenceStart, out var refStart) ? refStart : null,
                ReferenceEnd = int.TryParse(f.ReferenceEnd, out var refEnd) ? refEnd : null,
                ReferenceList = f.ReferenceList,
                Name = f.Name,
                RelatedPerson = f.RelatedPerson,
                // IssueDateStart/End string olarak gelir; InvariantCulture parse sonucunu kullan
                IssueDateStart = ignoreIssueDateFilter ? null : f.ParsedIssueDateStart,
                IssueDateEnd = ignoreIssueDateFilter ? null : f.ParsedIssueDateEnd,
                StateId = stateId,
                Evaluated = f.Evaluated == "true" ? true : f.Evaluated == "false" ? false : null,
                Page = f.Page,
                PageSize = f.PageSize > 0 ? f.PageSize : 10,
                First = includeFirst
                    ? f.First.HasValue && f.First.Value > 0 ? f.First.Value : DefaultSearchLimit
                    : null
            };
        }

        private static bool HasInvoiceFilterBeyondStatus(InvoiceViewModel f)
        {
            return !string.IsNullOrWhiteSpace(f.JobCustomerCode) ||
                   !string.IsNullOrWhiteSpace(f.JobCustomerName) ||
                   !string.IsNullOrWhiteSpace(f.InvoiceCustomerCode) ||
                   !string.IsNullOrWhiteSpace(f.InvoiceCustomerName) ||
                   !string.IsNullOrWhiteSpace(f.ReferenceStart) ||
                   !string.IsNullOrWhiteSpace(f.ReferenceEnd) ||
                   !string.IsNullOrWhiteSpace(f.ReferenceList) ||
                   !string.IsNullOrWhiteSpace(f.Name) ||
                   !string.IsNullOrWhiteSpace(f.RelatedPerson) ||
                   !string.IsNullOrWhiteSpace(f.Evaluated);
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
                }).ToList(),
                Jobs = model.Jobs.Select(job => new InvoiceCreateJobDto
                {
                    Reference = job.Reference
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

            // Tarihler string'den Parsed* property'leri ile elde edildiğinden
            // doğrudan ParsedIssueDateStart/End kullanıyoruz.
            if (filter.ParsedIssueDateStart.HasValue && filter.ParsedIssueDateEnd.HasValue)
            {
                items.Add(new PrintableReportMetaItem
                {
                    Label = L("DateRange"),
                    Value = $"{FormatDate(filter.ParsedIssueDateStart.Value)} - {FormatDate(filter.ParsedIssueDateEnd.Value)}"
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

            // string → DateTime dönüşümü Parsed* property'leri üzerinden yapılır
            var hasStartDate = filter.ParsedIssueDateStart.HasValue;
            var hasEndDate = filter.ParsedIssueDateEnd.HasValue;

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

            if (filter.ParsedIssueDateEnd!.Value.Date < filter.ParsedIssueDateStart!.Value.Date)
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
                JobCustomerCode = invoice.JobCustomerCode ?? string.Empty,
                JobCustomer = invoice.JobCustomerName ?? "-",
                InvoiceCustomerCode = invoice.InvoiceCustomerCode ?? string.Empty,
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
                    Id = l.Id,
                    Selected = l.Selected,
                    FreeFormat = l.FreeFormat,
                    LineNo = l.Sequence,
                    Notes = l.Notes ?? string.Empty,
                    Quantity = l.Quantity,
                    Price = l.Price,
                    Amount = l.Amount,
                    TaxType = l.TaxType ?? string.Empty,
                    TaxTypeId = l.TaxTypeId,
                    VatRate = l.VatRate,
                    WorkItems = l.Jobs.Select(j => new InvoiceWorkItemViewModel
                    {
                        Selected = j.Selected,
                        Reference = j.Reference.ToString(),
                        Name = j.Name ?? string.Empty,
                        Amount = j.Amount
                    }).ToList(),
                    ProductCategories = l.ProductCategories.Select(p => new InvoiceCategorySummaryViewModel
                    {
                        LineId = p.LineId,
                        ProdCatId = p.ProdCatId,
                        Name = p.Name ?? string.Empty,
                        SubTotal = p.SubTotal,
                        NetTotal = p.NetTotal
                    }).ToList()
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
                    LineId = p.LineId,
                    ProdCatId = p.ProdCatId,
                    Name = p.Name ?? string.Empty,
                    SubTotal = p.SubTotal,
                    NetTotal = p.NetTotal
                }).ToList(),
                Taxes = invoice.Taxes.Select(t => new InvoiceTaxSummaryViewModel
                {
                    TaxTypeId = t.TaxTypeId,
                    Code = t.Code ?? string.Empty,
                    Name = t.Name ?? string.Empty,
                    SubTotal = t.SubTotal,
                    Rate = t.Rate,
                    TaxAmount = t.TaxAmount,
                    NetTotal = t.NetTotal
                }).ToList()
            }).ToList();
        }

        private async Task<InvoiceDetailViewModel?> GetInvoiceDetailViewModelAsync(int reference)
        {
            if (reference <= 0)
            {
                return null;
            }

            var result = await _invoiceService.GetDetailsByReferencesAsync(new List<int> { reference });
            if (!result.IsSuccess || result.Data == null)
            {
                return null;
            }

            return MapDetails(result.Data).FirstOrDefault();
        }

        private async Task LoadJobPickerDropdownDataAsync()
        {
            var statesResult = await _lookupService.GetStatesAsync(StateCategories.Job);
            ViewBag.States = statesResult.IsSuccess && statesResult.Data != null
                ? statesResult.Data
                    .Where(x => JobStateDisplayOrder.Contains(x.Id))
                    .OrderBy(x => Array.IndexOf(JobStateDisplayOrder, x.Id))
                    .ToList()
                : new List<StateDto>();

            var functionsResult = await _lookupService.GetFunctionsAsync();
            ViewBag.Functions = functionsResult.IsSuccess && functionsResult.Data != null
                ? functionsResult.Data
                : new List<FunctionDto>();

            var workTypesResult = await _lookupService.GetWorkTypesAsync();
            ViewBag.WorkTypes = workTypesResult.IsSuccess && workTypesResult.Data != null
                ? workTypesResult.Data
                : new List<WorkTypeDto>();

            var timeTypesResult = await _lookupService.GetTimeTypesAsync();
            ViewBag.TimeTypes = timeTypesResult.IsSuccess && timeTypesResult.Data != null
                ? timeTypesResult.Data
                : new List<TimeTypeDto>();
        }

        private static JobFilterDto BuildJobPickerFilter(InvoiceJobPickerViewModel model)
        {
            return new JobFilterDto
            {
                FunctionId = ParseDecimalFilter(model.Function),
                CustomerCode = model.JobCustomerCode,
                ReferenceStart = int.TryParse(model.ReferenceStart, out var refStart) ? refStart : null,
                ReferenceEnd = int.TryParse(model.ReferenceEnd, out var refEnd) ? refEnd : null,
                ReferenceList = model.ReferenceList,
                JobName = model.Name,
                RelatedPerson = model.RelatedPerson,
                StartDateStart = ParseIsoDate(model.StartDateStart),
                StartDateEnd = ParseIsoDate(model.StartDateEnd),
                EndDateStart = ParseIsoDate(model.EndDateStart),
                EndDateEnd = ParseIsoDate(model.EndDateEnd),
                StateId = PricedJobStateId,
                IsEmailSent = model.EmailSent switch
                {
                    "true" => true,
                    "false" => false,
                    _ => null
                },
                IsEvaluated = false,
                HasInvoice = false,
                EmployeeCode = model.EmployeeCode,
                WorkTypeId = ParseDecimalFilter(model.TaskType),
                TimeTypeId = ParseDecimalFilter(model.OvertimeType),
                ProductId = model.ProductId,
                ProductCode = model.ProductCode,
                Page = model.Page > 0 ? model.Page : 1,
                PageSize = model.PageSize > 0 ? model.PageSize : 16,
                First = model.First > 0 ? model.First : DefaultSearchLimit
            };
        }

        private static decimal? ParseDecimalFilter(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariantValue)
                ? invariantValue
                : decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var currentValue)
                    ? currentValue
                    : null;
        }

        private static DateTime? ParseIsoDate(string? value)
        {
            return DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date
                : null;
        }

        private byte[] BuildInvoicePrintExcel(InvoiceDetailViewModel invoice)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Invoice");

            var lineTotal = invoice.Lines.Sum(x => x.Amount);
            var jobTotal = invoice.WorkItems.Sum(x => x.Amount);
            var taxSubTotal = invoice.Taxes.Sum(x => x.SubTotal);
            var taxTotal = invoice.Taxes.Sum(x => x.TaxAmount);
            var netTotal = invoice.Taxes.Sum(x => x.NetTotal);

            ws.Cell(1, 1).Value = L("Invoice");
            ws.Range(1, 1, 1, 7).Merge();
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 18;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var row = 3;
            WriteLabelValue(ws, row++, 1, L("Reference"), invoice.Reference);
            WriteLabelValue(ws, row++, 1, L("JobCustomer"), invoice.JobCustomer);
            WriteLabelValue(ws, row++, 1, L("InvoiceCustomer"), invoice.InvoiceCustomer);
            WriteLabelValue(ws, row++, 1, L("Name"), invoice.Name);
            WriteLabelValue(ws, row++, 1, L("Related"), invoice.RelatedPerson);
            WriteLabelValue(ws, row++, 1, L("Date"), invoice.IssueDate?.ToString("dd.MM.yyyy") ?? "-");
            WriteLabelValue(ws, row++, 1, L("Status"), invoice.Status);
            WriteLabelValue(ws, row++, 1, L("Evaluated"), invoice.Evaluated ? L("Yes") : L("No"));
            WriteLabelValue(ws, row++, 1, L("Notes"), invoice.Notes);
            WriteLabelValue(ws, row++, 1, L("Footnote"), invoice.FooterNote);

            row++;
            row = WriteInvoiceLinesTable(ws, row, invoice.Lines, lineTotal);

            if (invoice.WorkItems.Any())
            {
                row += 2;
                row = WriteInvoiceJobsTable(ws, row, invoice.WorkItems, jobTotal);
            }

            if (invoice.ProductCategories.Any())
            {
                row += 2;
                row = WriteInvoiceCategoriesTable(ws, row, invoice.ProductCategories);
            }

            if (invoice.Taxes.Any())
            {
                row += 2;
                row = WriteInvoiceTaxesTable(ws, row, invoice.Taxes, taxSubTotal, taxTotal, netTotal);
            }

            ws.Columns().AdjustToContents();
            ws.Column(3).Width = Math.Max(ws.Column(3).Width, 28);
            ws.Column(4).Width = Math.Max(ws.Column(4).Width, 12);
            ws.Column(5).Width = Math.Max(ws.Column(5).Width, 12);
            ws.Column(6).Width = Math.Max(ws.Column(6).Width, 12);
            ws.Column(7).Width = Math.Max(ws.Column(7).Width, 18);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private PrintableReportViewModel BuildInvoicePrintableReport(InvoiceDetailViewModel invoice)
        {
            var lineTotal = invoice.Lines.Sum(x => x.Amount);
            var taxTotal = invoice.Taxes.Sum(x => x.TaxAmount);
            var netTotal = invoice.Taxes.Sum(x => x.NetTotal);

            var blocks = new List<PrintableReportBlock>
            {
                new()
                {
                    Title = L("Notes"),
                    Items = new List<PrintableReportMetaItem>
                    {
                        new() { Label = L("Notes"), Value = string.IsNullOrWhiteSpace(invoice.Notes) ? "-" : invoice.Notes },
                        new() { Label = L("Footnote"), Value = string.IsNullOrWhiteSpace(invoice.FooterNote) ? "-" : invoice.FooterNote }
                    }
                },
                new()
                {
                    Title = L("Total"),
                    Items = new List<PrintableReportMetaItem>
                    {
                        new() { Label = L("SubTotal"), Value = lineTotal.ToString("N2", CultureInfo.CurrentCulture) },
                        new() { Label = L("TaxAmount"), Value = taxTotal.ToString("N2", CultureInfo.CurrentCulture) },
                        new() { Label = L("NetAmount"), Value = netTotal.ToString("N2", CultureInfo.CurrentCulture) }
                    }
                }
            };

            if (invoice.WorkItems.Any())
            {
                blocks.Add(new PrintableReportBlock
                {
                    Title = $"{L("Job")} ({invoice.WorkItems.Count})",
                    Items = invoice.WorkItems
                        .Select(x => new PrintableReportMetaItem
                        {
                            Label = x.Reference,
                            Value = $"{x.Name} - {x.Amount.ToString("N2", CultureInfo.CurrentCulture)}"
                        })
                        .ToList()
                });
            }

            if (invoice.ProductCategories.Any())
            {
                blocks.Add(new PrintableReportBlock
                {
                    Title = $"{L("ProductCategories")} ({invoice.ProductCategories.Count})",
                    Items = invoice.ProductCategories
                        .Select(x => new PrintableReportMetaItem
                        {
                            Label = x.Name,
                            Value = $"{L("SubTotal")}: {x.SubTotal.ToString("N2", CultureInfo.CurrentCulture)} | {L("NetAmount")}: {x.NetTotal.ToString("N2", CultureInfo.CurrentCulture)}"
                        })
                        .ToList()
                });
            }

            if (invoice.Taxes.Any())
            {
                blocks.Add(new PrintableReportBlock
                {
                    Title = $"{L("Taxes")} ({invoice.Taxes.Count})",
                    Items = invoice.Taxes
                        .Select(x => new PrintableReportMetaItem
                        {
                            Label = string.IsNullOrWhiteSpace(x.Name) ? x.Code : x.Name,
                            Value = $"{L("Percentage")}: {x.Rate.ToString("N0", CultureInfo.CurrentCulture)} | {L("TaxAmount")}: {x.TaxAmount.ToString("N2", CultureInfo.CurrentCulture)} | {L("NetAmount")}: {x.NetTotal.ToString("N2", CultureInfo.CurrentCulture)}"
                        })
                        .ToList()
                });
            }

            var rows = invoice.Lines.Select(line => new PrintableReportRow
            {
                Cells = new List<PrintableReportCell>
                {
                    new() { IsCheckbox = true, IsChecked = line.Selected, Alignment = "center" },
                    new() { Value = line.LineNo.ToString(CultureInfo.CurrentCulture) },
                    new() { Value = line.Notes },
                    new() { Value = line.Quantity.ToString("N2", CultureInfo.CurrentCulture), Alignment = "right" },
                    new() { Value = line.Price.ToString("N2", CultureInfo.CurrentCulture), Alignment = "right" },
                    new() { Value = line.Amount.ToString("N2", CultureInfo.CurrentCulture), Alignment = "right" },
                    new() { Value = line.TaxType }
                }
            }).ToList();

            rows.Add(new PrintableReportRow
            {
                Kind = PrintableReportRowKind.GrandTotal,
                Cells = new List<PrintableReportCell>
                {
                    new() { Value = L("Total"), ColSpan = 5, Alignment = "right" },
                    new() { Value = lineTotal.ToString("N2", CultureInfo.CurrentCulture), Alignment = "right" },
                    new() { Value = string.Empty }
                }
            });

            return new PrintableReportViewModel
            {
                Title = L("Invoice"),
                Orientation = "portrait",
                GeneratedAtDisplay = BuildGeneratedAtDisplay(),
                EmptyMessage = L("NoRecordsFound"),
                MetaItems = new List<PrintableReportMetaItem>
                {
                    new() { Label = L("Reference"), Value = invoice.Reference },
                    new() { Label = L("Date"), Value = invoice.IssueDate?.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture) ?? "-" },
                    new() { Label = L("JobCustomer"), Value = invoice.JobCustomer },
                    new() { Label = L("InvoiceCustomer"), Value = invoice.InvoiceCustomer },
                    new() { Label = L("Name"), Value = invoice.Name },
                    new() { Label = L("Related"), Value = invoice.RelatedPerson },
                    new() { Label = L("Status"), Value = invoice.Status },
                    new() { Label = L("Evaluated"), Value = invoice.Evaluated ? L("Yes") : L("No") }
                },
                Blocks = blocks,
                Columns = new List<PrintableReportColumn>
                {
                    new() { Title = L("Selected"), Alignment = "center" },
                    new() { Title = L("Sequence") },
                    new() { Title = L("Notes") },
                    new() { Title = L("Quantity"), Alignment = "right" },
                    new() { Title = L("Price"), Alignment = "right" },
                    new() { Title = L("Amount"), Alignment = "right" },
                    new() { Title = L("TaxType") }
                },
                Rows = rows
            };
        }

        private int WriteInvoiceLinesTable(IXLWorksheet ws, int startRow, List<InvoiceDetailLineViewModel> lines, decimal lineTotal)
        {
            WriteSectionTitle(ws, startRow, $"{L("Line")} ({lines.Count})", 7);
            WriteTableHeader(ws, startRow + 1, new[]
            {
                L("Selected"),
                L("Sequence"),
                L("Notes"),
                L("Quantity"),
                L("Price"),
                L("Amount"),
                L("TaxType")
            });

            var row = startRow + 2;
            foreach (var line in lines)
            {
                ws.Cell(row, 1).Value = line.Selected ? "X" : string.Empty;
                ws.Cell(row, 2).Value = line.LineNo;
                ws.Cell(row, 3).Value = line.Notes;
                ws.Cell(row, 4).Value = line.Quantity;
                ws.Cell(row, 5).Value = line.Price;
                ws.Cell(row, 6).Value = line.Amount;
                ws.Cell(row, 7).Value = line.TaxType;
                SetAmountStyle(ws.Cell(row, 5));
                SetAmountStyle(ws.Cell(row, 6));
                row++;
            }

            ws.Cell(row, 1).Value = L("Total");
            ws.Range(row, 1, row, 5).Merge();
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(row, 6).Value = lineTotal;
            SetAmountStyle(ws.Cell(row, 6));
            StyleTotalRow(ws, row, 7);
            return row;
        }

        private int WriteInvoiceJobsTable(IXLWorksheet ws, int startRow, List<InvoiceWorkItemViewModel> workItems, decimal jobTotal)
        {
            WriteSectionTitle(ws, startRow, $"{L("Job")} ({workItems.Count})", 4);
            WriteTableHeader(ws, startRow + 1, new[]
            {
                L("Selected"),
                L("Reference"),
                L("Name"),
                L("Amount")
            });

            var row = startRow + 2;
            foreach (var work in workItems)
            {
                ws.Cell(row, 1).Value = work.Selected ? "X" : string.Empty;
                ws.Cell(row, 2).Value = work.Reference;
                ws.Cell(row, 3).Value = work.Name;
                ws.Cell(row, 4).Value = work.Amount;
                SetAmountStyle(ws.Cell(row, 4));
                row++;
            }

            ws.Cell(row, 1).Value = L("Total");
            ws.Range(row, 1, row, 3).Merge();
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(row, 4).Value = jobTotal;
            SetAmountStyle(ws.Cell(row, 4));
            StyleTotalRow(ws, row, 4);
            return row;
        }

        private int WriteInvoiceCategoriesTable(IXLWorksheet ws, int startRow, List<InvoiceCategorySummaryViewModel> categories)
        {
            WriteSectionTitle(ws, startRow, $"{L("ProductCategories")} ({categories.Count})", 3);
            WriteTableHeader(ws, startRow + 1, new[]
            {
                L("ProductCategory"),
                L("SubTotal"),
                L("NetAmount")
            });

            var row = startRow + 2;
            foreach (var category in categories)
            {
                ws.Cell(row, 1).Value = category.Name;
                ws.Cell(row, 2).Value = category.SubTotal;
                ws.Cell(row, 3).Value = category.NetTotal;
                SetAmountStyle(ws.Cell(row, 2));
                SetAmountStyle(ws.Cell(row, 3));
                row++;
            }

            ws.Cell(row, 1).Value = L("Total");
            ws.Cell(row, 2).Value = categories.Sum(x => x.SubTotal);
            ws.Cell(row, 3).Value = categories.Sum(x => x.NetTotal);
            SetAmountStyle(ws.Cell(row, 2));
            SetAmountStyle(ws.Cell(row, 3));
            StyleTotalRow(ws, row, 3);
            return row;
        }

        private int WriteInvoiceTaxesTable(IXLWorksheet ws, int startRow, List<InvoiceTaxSummaryViewModel> taxes, decimal subTotal, decimal taxTotal, decimal netTotal)
        {
            WriteSectionTitle(ws, startRow, $"{L("Taxes")} ({taxes.Count})", 6);
            WriteTableHeader(ws, startRow + 1, new[]
            {
                L("Code"),
                L("Name"),
                L("SubTotal"),
                L("Percentage"),
                L("TaxAmount"),
                L("NetAmount")
            });

            var row = startRow + 2;
            foreach (var tax in taxes)
            {
                ws.Cell(row, 1).Value = tax.Code;
                ws.Cell(row, 2).Value = tax.Name;
                ws.Cell(row, 3).Value = tax.SubTotal;
                ws.Cell(row, 4).Value = tax.Rate;
                ws.Cell(row, 5).Value = tax.TaxAmount;
                ws.Cell(row, 6).Value = tax.NetTotal;
                SetAmountStyle(ws.Cell(row, 3));
                ws.Cell(row, 4).Style.NumberFormat.Format = "0";
                SetAmountStyle(ws.Cell(row, 5));
                SetAmountStyle(ws.Cell(row, 6));
                row++;
            }

            ws.Cell(row, 1).Value = L("Total");
            ws.Range(row, 1, row, 2).Merge();
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(row, 3).Value = subTotal;
            ws.Cell(row, 5).Value = taxTotal;
            ws.Cell(row, 6).Value = netTotal;
            SetAmountStyle(ws.Cell(row, 3));
            SetAmountStyle(ws.Cell(row, 5));
            SetAmountStyle(ws.Cell(row, 6));
            StyleTotalRow(ws, row, 6);
            return row;
        }

        private static void WriteLabelValue(IXLWorksheet ws, int row, int labelColumn, string label, string value)
        {
            ws.Cell(row, labelColumn).Value = label;
            ws.Cell(row, labelColumn).Style.Font.Bold = true;
            ws.Cell(row, labelColumn + 1).Value = value;
            ws.Range(row, labelColumn + 1, row, labelColumn + 5).Merge();
        }

        private static void WriteSectionTitle(IXLWorksheet ws, int row, string title, int columnCount)
        {
            ws.Cell(row, 1).Value = title;
            ws.Range(row, 1, row, columnCount).Merge();
            ws.Range(row, 1, row, columnCount).Style.Font.Bold = true;
            ws.Range(row, 1, row, columnCount).Style.Fill.BackgroundColor = XLColor.FromHtml("#DBEAFE");
        }

        private static void WriteTableHeader(IXLWorksheet ws, int row, string[] headers)
        {
            for (var i = 0; i < headers.Length; i++)
            {
                ws.Cell(row, i + 1).Value = headers[i];
            }

            ws.Range(row, 1, row, headers.Length).Style.Font.Bold = true;
            ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
            ws.Range(row, 1, row, headers.Length).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        private static void SetAmountStyle(IXLCell cell)
        {
            cell.Style.NumberFormat.Format = "#,##0.00";
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        }

        private static void StyleTotalRow(IXLWorksheet ws, int row, int columnCount)
        {
            ws.Range(row, 1, row, columnCount).Style.Font.Bold = true;
            ws.Range(row, 1, row, columnCount).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
            ws.Range(row, 1, row, columnCount).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        }

        private void ShowInvoiceMutationResult(ServiceResult result)
        {
            if (result.IsSuccess)
            {
                ShowSuccess(result.Message ?? L("SuccessTitle"));
            }
            else if (result.Errors.Any())
            {
                ShowError(string.Join(" ", result.Errors.Select(error => Ui(error, error))));
            }
            else
            {
                ShowError(result.Message ?? L("GenericError"));
            }
        }

        private IActionResult RedirectToInvoiceDetail(
            int reference,
            List<string>? selectedReferences,
            int currentIndex,
            string? returnUrl)
        {
            var references = selectedReferences?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList() ?? new List<string>();

            var referenceText = reference.ToString(CultureInfo.InvariantCulture);
            if (!references.Contains(referenceText))
            {
                references.Add(referenceText);
            }

            if (currentIndex < 0)
            {
                currentIndex = 0;
            }
            if (currentIndex >= references.Count)
            {
                currentIndex = references.Count - 1;
            }

            var resolvedReturnUrl = string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/')
                ? "/Invoice/Results"
                : returnUrl;

            return RedirectToAction(nameof(Detail), new
            {
                reference = referenceText,
                selectedReferences = references,
                page = currentIndex + 1,
                returnUrl = resolvedReturnUrl
            });
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

        private static string BuildInvoiceFileName(string prefix, int reference)
        {
            return $"{prefix}-{reference}-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
        }
    }
}
