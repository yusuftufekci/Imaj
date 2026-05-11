using Imaj.Core.Constants;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Service.Results;
using Imaj.Web;
using Imaj.Web.Authorization;
using Imaj.Web.Extensions;
using Imaj.Web.Models;
using Imaj.Web.Services.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Imaj.Web.Controllers
{
    public class JobController : Controller
    {
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private static readonly TimeSpan ReportExecutionTimeout = TimeSpan.FromSeconds(45);
        private static readonly decimal[] JobStateDisplayOrder = { 110m, 120m, 130m, 140m, 150m, 160m };
        private static readonly string[] DefaultCreateProductCodes = { "OPERATOR", "CAFE" };
        private const decimal CompleteMethodId = 1334m;
        private const decimal UndoCompleteMethodId = 1333m;
        private const decimal PriceMethodId = 1336m;
        private const decimal UndoPriceMethodId = 1335m;
        private const decimal CloseMethodId = 1344m;
        private const decimal UndoCloseMethodId = 1338m;
        private const decimal DiscardMethodId = 1340m;
        private const decimal UndoDiscardMethodId = 1339m;
        private const decimal EvaluateMethodId = 1341m;
        private const decimal UndoEvaluateMethodId = 1488m;

        private readonly IJobService _jobService;
        private readonly ICustomerService _customerService;
        private readonly IEmployeeService _employeeService;
        private readonly IEmailService _emailService;
        private readonly IProductService _productService;
        private readonly ILookupService _lookupService;
        private readonly IPendingInvoiceJobsReportExcelService _pendingInvoiceJobsReportExcelService;
        private readonly IJobReportExcelService _jobReportExcelService;
        private readonly IJobFormPdfService _jobFormPdfService;
        private readonly IPermissionViewService _permissionViewService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public JobController(
            IJobService jobService,
            ICustomerService customerService,
            IEmployeeService employeeService,
            IEmailService emailService,
            IProductService productService,
            ILookupService lookupService,
            IPendingInvoiceJobsReportExcelService pendingInvoiceJobsReportExcelService,
            IJobReportExcelService jobReportExcelService,
            IJobFormPdfService jobFormPdfService,
            IPermissionViewService permissionViewService,
            IStringLocalizer<SharedResource> localizer)
        {
            _jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _lookupService = lookupService ?? throw new ArgumentNullException(nameof(lookupService));
            _pendingInvoiceJobsReportExcelService = pendingInvoiceJobsReportExcelService ?? throw new ArgumentNullException(nameof(pendingInvoiceJobsReportExcelService));
            _jobReportExcelService = jobReportExcelService ?? throw new ArgumentNullException(nameof(jobReportExcelService));
            _jobFormPdfService = jobFormPdfService ?? throw new ArgumentNullException(nameof(jobFormPdfService));
            _permissionViewService = permissionViewService ?? throw new ArgumentNullException(nameof(permissionViewService));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }



        /// <summary>
        /// İş geçmişini tam sayfa olarak görüntüler.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetHistory(int id)
        {
            // If id is Reference:
            var reference = id; 
            var jobResult = await _jobService.GetByReferenceAsync(reference);

            if (!jobResult.IsSuccess || jobResult.Data == null)
            {
                return RedirectToAction("List");
            }
            
            var job = jobResult.Data;
            
            // Now get history using the actual Job.Id (PK)
            var historyResult = await _jobService.GetJobHistoryAsync(job.Id);
            var historyItems = historyResult.IsSuccess
                ? historyResult.Data ?? new List<JobLogDto>()
                : new List<JobLogDto>();

            var model = new JobHistoryViewModel
            {
                JobId = job.Id, // PK
                Reference = job.Reference.ToString(),
                Function = job.FunctionName ?? string.Empty,
                CustomerName = job.CustomerName ?? string.Empty,
                RelatedPerson = job.Contact ?? string.Empty, // "İlgili"
                ContactName = job.Name ?? string.Empty, // "Ad" (Job.Name kişi adı gibi kullanılıyor screenshotta)
                StartDate = job.StartDate,
                EndDate = job.EndDate,
                Status = NormalizeJobStatusName(job.StateId, job.StatusName),
                IsEmailSent = job.IsEmailSent,
                IsEvaluated = job.IsEvaluated,
                InvoiceStatus = BuildInvoiceDisplay(job),
                AdminNotes = job.IntNotes ?? string.Empty,
                CustomerNotes = job.ExtNotes ?? string.Empty,
                
                Items = historyItems.Select(x => new JobHistoryItem
                {
                    Date = x.LogDate,
                    UserCode = x.UserCode,
                    UserName = x.UserName,
                    UserEmail = x.UserEmail,
                    Action = x.ActionName
                }).ToList()
            };

            return View("History", model);
        }

        /// <summary>
        /// İş sorgulama sayfası.
        /// Dropdown verileri veritabanından yüklenir.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Dropdown verilerini backend'den al
            await LoadDropdownDataAsync();

            var model = new JobViewModel();
            // Varsayılan tarih filtresini kaldırdık - kullanıcı seçmedikçe filtre uygulanmasın
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CustomerJobStates()
        {
            if (!await CanUseCustomerLookupAsync())
            {
                return Forbid();
            }

            var result = await _lookupService.GetStatesAsync(StateCategories.Job);
            if (result.IsSuccess)
            {
                return Json(result.Data);
            }

            return BadRequest(this.LocalizeUiMessage(result.Message, L("GenericError")));
        }

        [HttpPost]
        public async Task<IActionResult> CustomerSearch([FromBody] CustomerFilterModel? filter)
        {
            if (!await CanUseCustomerLookupAsync())
            {
                return Forbid();
            }

            var f = filter ?? new CustomerFilterModel();
            f.Page = f.Page > 0 ? f.Page : 1;
            f.PageSize = f.PageSize > 0 ? f.PageSize : 20;
            f.First = f.First.HasValue && f.First.Value > 0 ? f.First.Value : null;

            var result = await _customerService.GetByFilterAsync(BuildCustomerFilter(f));

            var items = result.IsSuccess && result.Data != null
                ? result.Data.Items.Select(c => new CustomerSearchResult
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    City = c.City,
                    Phone = c.Phone,
                    Email = c.Email
                }).ToList()
                : new List<CustomerSearchResult>();

            var totalCount = result.IsSuccess && result.Data != null ? result.Data.TotalCount : 0;
            return Json(new { items, totalCount, page = f.Page, pageSize = f.PageSize });
        }

        [HttpGet]
        public async Task<IActionResult> CustomerProductCategories([FromQuery] decimal customerId)
        {
            if (!await CanUseCustomerLookupAsync())
            {
                return Forbid();
            }

            if (customerId <= 0)
            {
                return Json(new List<ProductCategoryDto>());
            }

            var result = await _customerService.GetByIdAsync(customerId);
            if (result.IsSuccess && result.Data != null)
            {
                return Json(result.Data.ProductCategories ?? new List<ProductCategoryDto>());
            }

            return Json(new List<ProductCategoryDto>());
        }

        [HttpGet]
        public async Task<IActionResult> ProductCategories()
        {
            if (!await CanUseProductLookupAsync())
            {
                return Forbid();
            }

            var result = await _productService.GetCategoriesAsync();
            if (result.IsSuccess)
            {
                return Json(result.Data);
            }

            return BadRequest(this.LocalizeUiMessage(result.Message, L("GenericError")));
        }

        [HttpGet]
        public async Task<IActionResult> ProductGroups([FromQuery] decimal? functionId = null)
        {
            if (!await CanUseProductLookupAsync())
            {
                return Forbid();
            }

            var result = await _productService.GetProductGroupsAsync(functionId);
            if (result.IsSuccess)
            {
                return Json(result.Data);
            }

            return BadRequest(this.LocalizeUiMessage(result.Message, L("GenericError")));
        }

        [HttpGet]
        public async Task<IActionResult> ProductFunctions()
        {
            if (!await CanUseProductLookupAsync())
            {
                return Forbid();
            }

            var result = await _productService.GetFunctionsAsync();
            if (result.IsSuccess)
            {
                return Json(result.Data);
            }

            return BadRequest(this.LocalizeUiMessage(result.Message, L("GenericError")));
        }

        [HttpPost]
        public async Task<IActionResult> ProductSearch([FromBody] ProductFilterModel? filter)
        {
            if (!await CanUseProductLookupAsync())
            {
                return Forbid();
            }

            var f = filter ?? new ProductFilterModel();
            f.Page = f.Page > 0 ? f.Page : 1;
            f.PageSize = f.PageSize > 0 ? f.PageSize : 10;
            f.First = f.First.HasValue && f.First.Value > 0 ? f.First.Value : f.PageSize;

            var result = await _productService.GetByFilterAsync(BuildProductFilter(f));

            var items = result.IsSuccess && result.Data != null
                ? result.Data.Items.Select(p => new ProductSearchResult
                {
                    Id = p.Id,
                    CategoryId = p.CategoryId,
                    Code = p.Code,
                    Name = p.Name,
                    Category = p.CategoryName,
                    ProductGroup = p.GroupName,
                    Price = p.Price
                }).ToList()
                : new List<ProductSearchResult>();

            var totalCount = result.IsSuccess && result.Data != null ? result.Data.TotalCount : 0;
            return Json(new { items, totalCount, page = f.Page, pageSize = f.PageSize });
        }

        [HttpGet]
        [RequireMethodPermission(1397)]
        public async Task<IActionResult> DownloadDetailedPendingInvoiceJobsExcel([FromQuery] PendingInvoiceJobsReportDownloadRequest request)
        {
            var filter = new PendingInvoiceJobsReportFilterDto
            {
                CustomerId = request.CustomerId,
                CustomerCode = request.CustomerCode
            };

            ServiceResult<List<PendingInvoiceJobsDetailedReportRowDto>> reportResult;
            try
            {
                using var cts = new CancellationTokenSource(ReportExecutionTimeout);
                reportResult = await _jobService.GetDetailedPendingInvoiceJobsReportAsync(filter, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, L("ReportRequestTimedOut"));
            }
            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(this.LocalizeUiMessage(reportResult.Message, L("ReportDataUnavailable")));
            }

            var context = new PendingInvoiceJobsReportExcelContext
            {
                CustomerDisplay = !string.IsNullOrWhiteSpace(request.CustomerName)
                    ? request.CustomerName!
                    : (!string.IsNullOrWhiteSpace(request.CustomerCode) ? request.CustomerCode! : L("AllOption"))
            };

            var fileBytes = _pendingInvoiceJobsReportExcelService.BuildDetailedReport(reportResult.Data, context);
            return File(fileBytes, ExcelContentType, BuildFileName(L("DetailedPendingInvoiceJobsFilePrefix")));
        }

        [HttpGet]
        [RequireMethodPermission(1397)]
        public async Task<IActionResult> DownloadSummaryPendingInvoiceJobsExcel([FromQuery] PendingInvoiceJobsReportDownloadRequest request)
        {
            var filter = new PendingInvoiceJobsReportFilterDto
            {
                CustomerId = request.CustomerId,
                CustomerCode = request.CustomerCode
            };

            ServiceResult<List<PendingInvoiceJobsSummaryReportRowDto>> reportResult;
            try
            {
                using var cts = new CancellationTokenSource(ReportExecutionTimeout);
                reportResult = await _jobService.GetSummaryPendingInvoiceJobsReportAsync(filter, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, L("ReportRequestTimedOut"));
            }
            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(this.LocalizeUiMessage(reportResult.Message, L("ReportDataUnavailable")));
            }

            var context = new PendingInvoiceJobsReportExcelContext
            {
                CustomerDisplay = !string.IsNullOrWhiteSpace(request.CustomerName)
                    ? request.CustomerName!
                    : (!string.IsNullOrWhiteSpace(request.CustomerCode) ? request.CustomerCode! : L("AllOption"))
            };

            var fileBytes = _pendingInvoiceJobsReportExcelService.BuildSummaryReport(reportResult.Data, context);
            return File(fileBytes, ExcelContentType, BuildFileName(L("SummaryPendingInvoiceJobsFilePrefix")));
        }

        [HttpGet]
        [RequireMethodPermission(1397)]
        public async Task<IActionResult> DownloadDetailedJobReportExcel([FromQuery] JobFilterModel filter)
        {
            if (!TryValidateJobReportDateRanges(filter, out var badRequestResult))
            {
                return badRequestResult!;
            }

            var reportFilter = BuildJobFilterDto(filter, includeFirst: false);
            ServiceResult<List<JobDetailedReportRowDto>> reportResult;
            try
            {
                using var cts = new CancellationTokenSource(ReportExecutionTimeout);
                reportResult = await _jobService.GetDetailedJobReportAsync(reportFilter, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, L("ReportRequestTimedOut"));
            }
            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(this.LocalizeUiMessage(reportResult.Message, L("ReportDataUnavailable")));
            }

            var fileBytes = _jobReportExcelService.BuildDetailedReport(reportResult.Data);
            return File(fileBytes, ExcelContentType, BuildFileName(L("DetailedJobReportFilePrefix")));
        }

        [HttpGet]
        [RequireMethodPermission(1397)]
        public async Task<IActionResult> DownloadSummaryJobReportExcel([FromQuery] JobFilterModel filter)
        {
            if (!TryValidateJobReportDateRanges(filter, out var badRequestResult))
            {
                return badRequestResult!;
            }

            var reportFilter = BuildJobFilterDto(filter, includeFirst: false);
            ServiceResult<List<JobSummaryReportRowDto>> reportResult;
            try
            {
                using var cts = new CancellationTokenSource(ReportExecutionTimeout);
                reportResult = await _jobService.GetSummaryJobReportAsync(reportFilter, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, L("ReportRequestTimedOut"));
            }
            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(this.LocalizeUiMessage(reportResult.Message, L("ReportDataUnavailable")));
            }

            var fileBytes = _jobReportExcelService.BuildSummaryReport(reportResult.Data);
            return File(fileBytes, ExcelContentType, BuildFileName(L("SummaryJobReportFilePrefix")));
        }

        [HttpGet]
        [RequireMethodPermission(1397)]
        public async Task<IActionResult> ViewDetailedPendingInvoiceJobsReport([FromQuery] PendingInvoiceJobsReportDownloadRequest request)
        {
            var filter = new PendingInvoiceJobsReportFilterDto
            {
                CustomerId = request.CustomerId,
                CustomerCode = request.CustomerCode
            };

            ServiceResult<List<PendingInvoiceJobsDetailedReportRowDto>> reportResult;
            try
            {
                using var cts = new CancellationTokenSource(ReportExecutionTimeout);
                reportResult = await _jobService.GetDetailedPendingInvoiceJobsReportAsync(filter, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, L("ReportRequestTimedOut"));
            }

            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(this.LocalizeUiMessage(reportResult.Message, L("ReportDataUnavailable")));
            }

            var model = BuildDetailedPendingInvoicePrintableReport(reportResult.Data, request);
            return View("~/Views/Shared/PrintableReport.cshtml", model);
        }

        [HttpGet]
        [RequireMethodPermission(1397)]
        public async Task<IActionResult> ViewSummaryPendingInvoiceJobsReport([FromQuery] PendingInvoiceJobsReportDownloadRequest request)
        {
            var filter = new PendingInvoiceJobsReportFilterDto
            {
                CustomerId = request.CustomerId,
                CustomerCode = request.CustomerCode
            };

            ServiceResult<List<PendingInvoiceJobsSummaryReportRowDto>> reportResult;
            try
            {
                using var cts = new CancellationTokenSource(ReportExecutionTimeout);
                reportResult = await _jobService.GetSummaryPendingInvoiceJobsReportAsync(filter, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, L("ReportRequestTimedOut"));
            }

            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(this.LocalizeUiMessage(reportResult.Message, L("ReportDataUnavailable")));
            }

            var model = BuildSummaryPendingInvoicePrintableReport(reportResult.Data, request);
            return View("~/Views/Shared/PrintableReport.cshtml", model);
        }

        [HttpGet]
        [RequireMethodPermission(1397)]
        public async Task<IActionResult> ViewDetailedJobReport([FromQuery] JobFilterModel filter)
        {
            if (!TryValidateJobReportDateRanges(filter, out var badRequestResult))
            {
                return badRequestResult!;
            }

            var reportFilter = BuildJobFilterDto(filter, includeFirst: false);
            ServiceResult<List<JobDetailedReportRowDto>> reportResult;
            try
            {
                using var cts = new CancellationTokenSource(ReportExecutionTimeout);
                reportResult = await _jobService.GetDetailedJobReportAsync(reportFilter, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, L("ReportRequestTimedOut"));
            }

            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(this.LocalizeUiMessage(reportResult.Message, L("ReportDataUnavailable")));
            }

            var model = BuildDetailedJobPrintableReport(reportResult.Data, filter);
            return View("~/Views/Shared/PrintableReport.cshtml", model);
        }

        [HttpGet]
        [RequireMethodPermission(1397)]
        public async Task<IActionResult> ViewSummaryJobReport([FromQuery] JobFilterModel filter)
        {
            if (!TryValidateJobReportDateRanges(filter, out var badRequestResult))
            {
                return badRequestResult!;
            }

            var reportFilter = BuildJobFilterDto(filter, includeFirst: false);
            ServiceResult<List<JobSummaryReportRowDto>> reportResult;
            try
            {
                using var cts = new CancellationTokenSource(ReportExecutionTimeout);
                reportResult = await _jobService.GetSummaryJobReportAsync(reportFilter, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(408, L("ReportRequestTimedOut"));
            }

            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(this.LocalizeUiMessage(reportResult.Message, L("ReportDataUnavailable")));
            }

            var model = BuildSummaryJobPrintableReport(reportResult.Data, filter);
            return View("~/Views/Shared/PrintableReport.cshtml", model);
        }

        /// <summary>
        /// İş listesi sorgulaması.
        /// Veritabanından filtreleme ile getirir.
        /// page ve pageSize URL query string'den ayrı alınabilir (pagination linkleri için)
        /// </summary>
        [AcceptVerbs("GET", "POST")]
        [RequireMethodPermission(1397)]
        public async Task<IActionResult> List(JobViewModel model, int? page, int? pageSize, int? first)
        {
            // Pagination değerlerini URL'den gelen değerlerle override et
            // Bu sayede /Job/List?page=2&pageSize=10 şeklinde linkler çalışır
            if (page.HasValue && page.Value > 0)
            {
                model.Filter.Page = page.Value;
            }
            if (pageSize.HasValue && pageSize.Value > 0)
            {
                model.Filter.PageSize = pageSize.Value;
            }
            if (first.HasValue && first.Value > 0)
            {
                model.Filter.First = first.Value;
            }
            model.Filter.First = model.Filter.First.HasValue && model.Filter.First.Value > 0 ? model.Filter.First.Value : (model.Filter.PageSize > 0 ? model.Filter.PageSize : 10);
            
            // Dropdown verilerini backend'den al (geri dönüşlerde gerekli)
            await LoadDropdownDataAsync();

            var filterDto = BuildJobFilterDto(model.Filter, includeFirst: true);

            // Veritabanından verileri getir
            var result = await _jobService.GetByFilterAsync(filterDto);

            if (result.IsSuccess && result.Data != null)
            {
                // DTO'dan ViewModel'e dönüştür
                model.Items = result.Data.Items.Select(j => new JobSearchResult
                {
                    Code = j.Reference.ToString(),
                    StateId = j.StateId,
                    Function = j.FunctionName,
                    Name = j.Name,
                    CustomerName = j.CustomerName,
                    StartDate = j.StartDate,
                    EndDate = j.EndDate,
                    Status = NormalizeJobStatusName(j.StateId, j.StatusName),
                    IsEmailSent = j.IsEmailSent,
                    IsEvaluated = j.IsEvaluated
                }).ToList();
                
                model.TotalCount = result.Data.TotalCount;
            }
            else
            {
                model.Items = new List<JobSearchResult>();
                model.TotalCount = 0;
                // Hatayı ekrana bas
                ModelState.AddModelError(string.Empty, this.LocalizeUiMessage(result.Message, L("DataLoadError")));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(1397, write: true)]
        public async Task<IActionResult> EmailCompose(string[]? selectedIds = null, string? returnUrl = null)
        {
            var normalizedReferences = NormalizeSelectedReferences(selectedIds);
            var normalizedReturnUrl = NormalizeListReturnUrl(returnUrl);

            if (normalizedReferences.Count == 0)
            {
                TempData["ErrorMessage"] = L("PleaseSelectAtLeastOneJob");
                return LocalRedirect(normalizedReturnUrl);
            }

            var draftResult = await _jobService.PrepareEmailDraftAsync(normalizedReferences);
            if (!draftResult.IsSuccess || draftResult.Data == null)
            {
                TempData["ErrorMessage"] = this.LocalizeUiMessage(draftResult.Message, L("GenericError"));
                return LocalRedirect(normalizedReturnUrl);
            }

            var model = BuildJobEmailComposeViewModel(draftResult.Data, normalizedReturnUrl);
            return View("EmailCompose", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(1397, write: true)]
        public async Task<IActionResult> SendEmail(JobEmailComposeViewModel model)
        {
            var normalizedReferences = NormalizeSelectedReferences(model.SelectedIds);
            model.ReturnUrl = NormalizeListReturnUrl(model.ReturnUrl);

            if (normalizedReferences.Count == 0)
            {
                TempData["ErrorMessage"] = L("PleaseSelectAtLeastOneJob");
                return LocalRedirect(model.ReturnUrl);
            }

            var draftResult = await _jobService.PrepareEmailDraftAsync(normalizedReferences);
            if (!draftResult.IsSuccess || draftResult.Data == null)
            {
                TempData["ErrorMessage"] = this.LocalizeUiMessage(draftResult.Message, L("GenericError"));
                return LocalRedirect(model.ReturnUrl);
            }

            var recipientOverrides = BuildRecipientOverrideMap(model.Jobs);
            var multipleJobsSelected = draftResult.Data.Items.Count > 1;
            string? validationErrorMessage = null;

            if (multipleJobsSelected)
            {
                foreach (var item in draftResult.Data.Items)
                {
                    var referenceKey = item.Reference.ToString(CultureInfo.InvariantCulture);
                    recipientOverrides.TryGetValue(referenceKey, out var recipientEmail);

                    if (string.IsNullOrWhiteSpace(recipientEmail) || !IsValidEmailAddress(recipientEmail))
                    {
                        validationErrorMessage = L("JobEmailRecipientValidationMultiple");
                        break;
                    }
                }
            }
            else if (string.IsNullOrWhiteSpace(model.RecipientEmail))
            {
                validationErrorMessage = L("JobEmailRecipientValidationSingle");
            }
            else if (!IsValidEmailAddress(model.RecipientEmail))
            {
                validationErrorMessage = L("JobEmailRecipientValidationSingle");
            }

            if (!string.IsNullOrWhiteSpace(validationErrorMessage))
            {
                TempData["ErrorMessage"] = validationErrorMessage;
                var invalidModel = BuildJobEmailComposeViewModel(
                    draftResult.Data,
                    model.ReturnUrl,
                    multipleJobsSelected ? null : model.RecipientEmail,
                    recipientOverrides);
                return View("EmailCompose", invalidModel);
            }

            ServiceResult sendResult;
            if (multipleJobsSelected)
            {
                sendResult = ServiceResult.Success();

                foreach (var item in draftResult.Data.Items)
                {
                    var referenceKey = item.Reference.ToString(CultureInfo.InvariantCulture);
                    var singleDraft = CreateSingleJobEmailDraft(item, recipientOverrides[referenceKey].Trim());
                    var emailMessageResult = await BuildJobFormEmailMessageAsync(singleDraft, singleDraft.RecipientEmail);
                    if (!emailMessageResult.IsSuccess || emailMessageResult.Data == null)
                    {
                        sendResult = ServiceResult.Fail(emailMessageResult.Message ?? L("GenericError"));
                        break;
                    }

                    sendResult = await _emailService.SendAsync(emailMessageResult.Data);

                    if (!sendResult.IsSuccess)
                    {
                        break;
                    }
                }
            }
            else
            {
                var emailMessageResult = await BuildJobFormEmailMessageAsync(draftResult.Data, model.RecipientEmail!.Trim());
                if (!emailMessageResult.IsSuccess || emailMessageResult.Data == null)
                {
                    TempData["ErrorMessage"] = this.LocalizeUiMessage(emailMessageResult.Message, L("GenericError"));
                    var failedBuildModel = BuildJobEmailComposeViewModel(
                        draftResult.Data,
                        model.ReturnUrl,
                        model.RecipientEmail,
                        recipientOverrides);
                    return View("EmailCompose", failedBuildModel);
                }

                sendResult = await _emailService.SendAsync(emailMessageResult.Data);
            }

            if (!sendResult.IsSuccess)
            {
                TempData["ErrorMessage"] = this.LocalizeUiMessage(sendResult.Message, L("GenericError"));
                var failedModel = BuildJobEmailComposeViewModel(
                    draftResult.Data,
                    model.ReturnUrl,
                    multipleJobsSelected ? null : model.RecipientEmail,
                    recipientOverrides);
                return View("EmailCompose", failedModel);
            }

            var markResult = await _jobService.MarkEmailSentAsync(normalizedReferences);
            if (!markResult.IsSuccess)
            {
                TempData["ErrorMessage"] = this.LocalizeUiMessage(markResult.Message, L("GenericError"));
                var failedModel = BuildJobEmailComposeViewModel(
                    draftResult.Data,
                    model.ReturnUrl,
                    multipleJobsSelected ? null : model.RecipientEmail,
                    recipientOverrides);
                return View("EmailCompose", failedModel);
            }

            TempData["SuccessMessage"] = string.Format(L("JobEmailSentSuccess"), normalizedReferences.Count);
            return LocalRedirect(model.ReturnUrl);
        }

        // Action to view detail. Supports navigation if multiple ids passed
        // Accepted via POST from List or GET from navigation links
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> Detail(string? id, string[]? selectedIds = null, int currentIndex = 0, string? returnUrl = null)
        {
            // If posted from List page with multiple selections but no specific ID
            if (string.IsNullOrEmpty(id) && selectedIds != null && selectedIds.Length > 0)
            {
                id = selectedIds[0];
                currentIndex = 0;
            }

            // Fallback
            if (string.IsNullOrEmpty(id)) 
            {
                 return RedirectToAction("Index");
            }
            
            // If navigating via next/prev
            if (selectedIds != null && selectedIds.Length > 0)
            {
                 if (currentIndex < 0) currentIndex = 0;
                 if (currentIndex >= selectedIds.Length) currentIndex = selectedIds.Length - 1;
                 
                 id = selectedIds[currentIndex];
            }

            var normalizedReturnUrl = string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/')
                ? "/Job/List"
                : returnUrl;
            if (Microsoft.AspNetCore.Http.HttpMethods.IsPost(Request.Method))
            {
                return RedirectToAction(nameof(Detail), new
                {
                    id,
                    selectedIds,
                    currentIndex,
                    returnUrl = normalizedReturnUrl
                });
            }

            // Referans numarasından iş bul
            if (!int.TryParse(id, out var reference))
            {
                return RedirectToAction("Index");
            }

            // Dropdown verilerini backend'den al
            await LoadDropdownDataAsync();
            
            // Veritabanından getir (Referans ve Detaylı)
            var result = await _jobService.GetByReferenceAsync(reference);
            
            if (!result.IsSuccess || result.Data == null)
            {
                // İş bulunamazsa listeye dön
                return RedirectToAction("List");
            }
            
            var job = result.Data;

            // Map to Detail ViewModel
            var model = new JobDetailViewModel
            {
                Code = job.Reference.ToString(),
                FunctionId = job.FunctionId,
                Function = job.FunctionName,
                CustomerId = job.CustomerId,
                CustomerCode = job.CustomerCode,
                CustomerName = job.CustomerName,
                Name = job.Name,
                RelatedPerson = job.Contact,
                StartDate = job.StartDate,
                EndDate = job.EndDate,
                StateId = job.StateId,
                Status = NormalizeJobStatusName(job.StateId, job.StatusName),
                IsEmailSent = job.IsEmailSent,
                IsEvaluated = job.IsEvaluated,
                InvoiceStatus = BuildInvoiceDisplay(job),
                HasInvoiceLink = job.HasInvoiceLink || job.InvoLineId.HasValue,
                
                // Mapped Notes
                AdminNotes = job.IntNotes,
                CustomerNotes = job.ExtNotes,
                
                // Navigation state
                SelectedIds = selectedIds?.ToList() ?? new List<string> { id },
                CurrentIndex = currentIndex,
                TotalSelected = selectedIds?.Length ?? 1,
                ReturnUrl = normalizedReturnUrl
            };

            // Mesai (JobWork) Detaylarını Map Et
            if (job.JobWorks != null && job.JobWorks.Any())
            {
                model.Overtimes = job.JobWorks.Select(jw => new JobOvertimeItem
                {
                    Id = jw.Id,
                    EmployeeId = jw.EmployeeId,
                    IsSelected = jw.SelectFlag,
                    EmployeeCode = jw.EmployeeCode,
                    EmployeeName = jw.EmployeeName,
                    WorkTypeId = jw.WorkTypeId,
                    TaskType = jw.WorkTypeName,
                    TimeTypeId = jw.TimeTypeId,
                    OvertimeType = jw.TimeTypeName,
                    Quantity = jw.Quantity,
                    Amount = jw.Amount,
                    Notes = jw.Notes
                }).ToList();

                // Toplam Tutar Hesapla
                model.TotalOvertimeAmount = model.Overtimes.Sum(x => x.Amount);
            }

            // Ürün Detayları (JobProd)
            if (job.JobProds != null && job.JobProds.Any())
            {
                // 1. Ürün Listesi Mapping
                model.Products = job.JobProds
                    .Select(jp => new JobProductItem
                    {
                        Id = jp.Id,
                        ProductId = jp.ProductId,
                        IsSelected = jp.SelectFlag,
                        Code = jp.ProductCode,
                        Name = jp.ProductName,
                        CategoryId = jp.CategoryId,
                        CategoryName = jp.CategoryName,
                        Quantity = jp.Quantity,
                        Price = jp.Price,
                        SubTotal = jp.GrossAmount,
                        NetTotal = jp.NetAmount,
                        Notes = jp.Notes
                    })
                    .OrderBy(GetJobProductDisplayRank)
                    .ThenBy(x => x.Code)
                    .ThenBy(x => x.Name)
                    .ToList();

                model.TotalProductAmount = model.Products.Sum(x => x.NetTotal);
            }

            // 2. Kategori Özeti (JobProdCat öncelikli)
            if (job.JobProdCats != null && job.JobProdCats.Any())
            {
                model.ProductCategories = job.JobProdCats
                    .GroupBy(jp => new { jp.CategoryId, jp.CategoryName })
                    .Select(g =>
                    {
                        var subTotal = g.Sum(x => x.GrossAmount);
                        var netTotal = g.Sum(x => x.NetAmount);
                        var discAmount = g.Sum(x => x.DiscAmount);
                        var discPercentage = subTotal > 0 ? (discAmount / subTotal) * 100 : 0;
                        return new JobCategorySummaryItem
                        {
                            CategoryId = g.Key.CategoryId,
                            Name = g.Key.CategoryName,
                            SubTotal = subTotal,
                            NetTotal = netTotal,
                            DiscountAmount = discAmount,
                            Discount = discPercentage
                        };
                    })
                    .ToList();

                model.TotalCategoryAmount = model.ProductCategories.Sum(x => x.NetTotal);
            }
            else if (job.JobProds != null && job.JobProds.Any())
            {
                model.ProductCategories = job.JobProds
                    .GroupBy(jp => new { jp.CategoryId, jp.CategoryName })
                    .Select(g =>
                    {
                        // Eğer ürünün birim fiyatı 0 ise (Hizmet, Mesai vb.), Ara Tutar olarak Net Tutar'ı baz alıyoruz.
                        // Aksi takdirde (Ürün vb.) Ara Tutar olarak Gross Tutar'ı (Miktar * Fiyat) alıyoruz.
                        var subTotal = g.Sum(x => x.Price == 0 ? x.NetAmount : x.GrossAmount);
                        var netTotal = g.Sum(x => x.NetAmount);

                        return new JobCategorySummaryItem
                        {
                            CategoryId = g.Key.CategoryId,
                            Name = g.Key.CategoryName,
                            SubTotal = subTotal,
                            NetTotal = netTotal,
                            DiscountAmount = subTotal - netTotal,
                            Discount = subTotal > 0 ? ((subTotal - netTotal) / subTotal) * 100 : 0
                        };
                    })
                    .ToList();

                model.TotalCategoryAmount = model.ProductCategories.Sum(x => x.NetTotal);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireMethodPermission(1397, write: true)]
        public async Task<IActionResult> UpdateDetail(JobDetailViewModel model)
        {
            var selectedIds = model.SelectedIds ?? new List<string>();
            var normalizedReturnUrl = NormalizeListReturnUrl(model.ReturnUrl);

            if (!int.TryParse(model.Code, out var reference) || reference <= 0)
            {
                TempData["ErrorMessage"] = L("JobNotFound");
                return LocalRedirect(normalizedReturnUrl);
            }

            if (selectedIds.Count == 0)
            {
                selectedIds.Add(reference.ToString(CultureInfo.InvariantCulture));
            }

            var updateDto = new JobDto
            {
                Reference = reference,
                FunctionId = model.FunctionId,
                CustomerId = model.CustomerId,
                Name = model.Name,
                Contact = model.RelatedPerson,
                StartDate = model.StartDate,
                EndDate = model.EndDate ?? model.StartDate,
                IntNotes = model.AdminNotes,
                ExtNotes = model.CustomerNotes,
                JobWorks = model.Overtimes.Select(x => new JobWorkDto
                {
                    Id = x.Id,
                    EmployeeId = x.EmployeeId,
                    WorkTypeId = x.WorkTypeId,
                    TimeTypeId = x.TimeTypeId,
                    Quantity = x.Quantity,
                    Amount = x.Amount,
                    Notes = x.Notes,
                    SelectFlag = x.IsSelected
                }).ToList(),
                JobProds = model.Products.Select(x => new JobProdDto
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    CategoryId = x.CategoryId,
                    Quantity = x.Quantity,
                    Price = x.Price,
                    GrossAmount = x.SubTotal,
                    NetAmount = x.NetTotal,
                    Notes = x.Notes,
                    SelectFlag = x.IsSelected
                }).ToList(),
                JobProdCats = model.ProductCategories.Select(x => new JobProdCatDto
                {
                    CategoryId = x.CategoryId,
                    CategoryName = x.Name,
                    GrossAmount = x.SubTotal,
                    DiscAmount = x.DiscountAmount,
                    DiscPercentage = (byte)Math.Clamp((int)Math.Round(x.Discount), 0, 100),
                    NetAmount = x.NetTotal
                }).ToList()
            };

            var result = await _jobService.UpdateAsync(updateDto);
            TempData[result.IsSuccess ? "SuccessMessage" : "ErrorMessage"] = result.IsSuccess
                ? L("JobUpdatedSuccess")
                : this.LocalizeUiMessage(result.Message, L("SaveError"));

            return RedirectToAction(nameof(Detail), new
            {
                id = reference.ToString(CultureInfo.InvariantCulture),
                selectedIds = selectedIds.ToArray(),
                currentIndex = model.CurrentIndex,
                returnUrl = normalizedReturnUrl
            });
        }

        [HttpGet]
        [RequireMethodPermission(1501)]
        public async Task<IActionResult> PrintJobForm(int reference, string? returnUrl = null)
        {
            var normalizedReturnUrl = NormalizeListReturnUrl(returnUrl);
            var result = await _jobService.GetByReferenceAsync(reference);

            if (!result.IsSuccess || result.Data == null)
            {
                return RedirectToAction("List");
            }

            if (!IsPrintableJobFormState(result.Data.StateId))
            {
                return RedirectToAction(nameof(Detail), new
                {
                    id = reference.ToString(CultureInfo.InvariantCulture),
                    returnUrl = normalizedReturnUrl
                });
            }

            var model = BuildJobPrintFormViewModel(result.Data);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WorkflowAction(JobWorkflowActionRequest request)
        {
            if (request.Reference <= 0)
            {
                TempData["ErrorMessage"] = L("JobNotFound");
                return RedirectToAction("List");
            }

            var methodId = ResolveWorkflowMethodId(request.Action);
            if (!methodId.HasValue)
            {
                TempData["ErrorMessage"] = L("GenericError");
                return RedirectToDetailWithContext(request);
            }

            var hasPermission = await _permissionViewService.CanExecuteMethodAsync(methodId.Value, write: true);
            if (!hasPermission)
            {
                TempData["ErrorMessage"] = L("AccessDeniedMessage");
                return RedirectToDetailWithContext(request);
            }

            var result = await _jobService.ExecuteWorkflowActionAsync(request.Reference, request.Action);
            TempData[result.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
                this.LocalizeUiMessage(result.Message, result.IsSuccess ? L("SuccessTitle") : L("GenericError"));

            return RedirectToDetailWithContext(request);
        }

        /// <summary>
        /// Yeni iş yaratma sayfası.
        /// Dropdown verileri veritabanından yüklenir.
        /// </summary>
        public async Task<IActionResult> Create(decimal? functionId = null)
        {
            // Dropdown verilerini backend'den al
            await LoadDropdownDataAsync();

            var functions = ViewBag.Functions as List<FunctionDto> ?? new List<FunctionDto>();
            var selectedFunction = ResolveFunctionSelection(functionId, functions);

            var model = new JobCreateViewModel
            {
                FunctionId = selectedFunction?.Id,
                FunctionName = selectedFunction?.Name ?? "Mesai"
            };
            model.StartDate = DateTime.Now;
            model.EndDate = DateTime.Now;
            model.AutoOvertimeTemplates = await GetDefaultCreateOvertimeTemplatesAsync(selectedFunction?.Id);
            model.Overtimes = BuildInitialAfterHoursOvertimes(model.AutoOvertimeTemplates, model.StartDate, model.EndDate);
            model.Products = await GetDefaultCreateProductsAsync();
            return View(model);
        }

        [HttpPost]
        [RequireMethodPermission(1397, write: true)]
        public async Task<IActionResult> Create(JobCreateViewModel model)
        {
            // AJAX isteği kontrolü
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (!ModelState.IsValid)
            {
                if (isAjax)
                {
                    // Validasyon hatalarını topla
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                        
                    return Json(new { success = false, message = L("PleaseCheckFormFields"), errors = errors });
                }

                await LoadDropdownDataAsync();
                ApplyFunctionSelection(model);
                model.AutoOvertimeTemplates = await GetDefaultCreateOvertimeTemplatesAsync(model.FunctionId);
                return View(model);
            }

            // Map View Model to DTO
            var jobDto = new JobDto
            {
                CustomerId = model.CustomerId,
                Name = model.Name,
                Contact = model.RelatedPerson,
                // Function Handling
                FunctionId = model.FunctionId ?? 1m,
                
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                IsEmailSent = false,
                IsEvaluated = false,
                IntNotes = model.AdminNotes,
                ExtNotes = model.CustomerNotes,
                
                // Map Overtimes
                JobWorks = model.Overtimes?.Select(x => new JobWorkDto
                {
                    EmployeeId = x.EmployeeId,
                    WorkTypeId = x.WorkTypeId,
                    TimeTypeId = x.TimeTypeId,
                    Quantity = x.Quantity,
                    Amount = x.Amount,
                    Notes = x.Notes
                }).ToList() ?? new List<JobWorkDto>(),
                
                // Map Products
                JobProds = model.Products?.Select(x => new JobProdDto
                {
                    ProductId = x.ProductId,
                    CategoryId = x.CategoryId,
                    CategoryName = x.CategoryName,
                    Quantity = x.Quantity,
                    Price = x.Price,
                    NetAmount = x.NetAmount,
                    Notes = x.Notes
                }).ToList() ?? new List<JobProdDto>(),

                JobProdCats = model.ProductCategories?.Select(x => new JobProdCatDto
                {
                    CategoryId = x.CategoryId,
                    CategoryName = x.Name,
                    GrossAmount = x.SubTotal,
                    DiscAmount = x.DiscountAmount,
                    DiscPercentage = (byte)Math.Clamp((int)Math.Round(x.Discount), 0, 100),
                    NetAmount = x.NetTotal
                }).ToList() ?? new List<JobProdCatDto>()
            };

            var result = await _jobService.AddAsync(jobDto);
            
            if (result.IsSuccess && result.Data != null)
            {
                if (isAjax)
                {
                    return Json(new { success = true, message = string.Format(L("JobCreatedWithReference"), result.Data.Reference), redirectUrl = Url.Action("Detail", new { id = result.Data.Reference }) });
                }

                // Başarılı: TempData ile success mesajı set et
                TempData["SuccessMessage"] = string.Format(L("JobCreatedWithReference"), result.Data.Reference);
                
                // Redirect to Detail
                return RedirectToAction("Detail", new { id = result.Data.Reference });
            }
            
            // Başarısız
            if (isAjax)
            {
                return Json(new { success = false, message = this.LocalizeUiMessage(result.Message, L("SaveError")) });
            }

            // TempData ile error mesajı set et
            TempData["ErrorMessage"] = this.LocalizeUiMessage(result.Message, L("SaveError"));
            ModelState.AddModelError(string.Empty, this.LocalizeUiMessage(result.Message, L("SaveError")));
            await LoadDropdownDataAsync();
            ApplyFunctionSelection(model);
            model.AutoOvertimeTemplates = await GetDefaultCreateOvertimeTemplatesAsync(model.FunctionId);
            return View(model);
        }

        /// <summary>
        /// Dropdown verilerini LookupService üzerinden yükler ve ViewBag'e ekler.
        /// States, Functions, WorkTypes, TimeTypes
        /// </summary>
        private async Task LoadDropdownDataAsync()
        {
            // Durum listesi - Job kategorisi (StateCategories constant kullanılıyor)
            var statesResult = await _lookupService.GetStatesAsync(StateCategories.Job);
            ViewBag.States = statesResult.IsSuccess && statesResult.Data != null
                ? statesResult.Data
                    .Where(x => JobStateDisplayOrder.Contains(x.Id))
                    .OrderBy(x => Array.IndexOf(JobStateDisplayOrder, x.Id))
                    .ToList()
                : new List<StateDto>();

            // Fonksiyon listesi
            var functionsResult = await _lookupService.GetFunctionsAsync();
            ViewBag.Functions = functionsResult.IsSuccess ? functionsResult.Data : new List<FunctionDto>();

            // Görev Tipi listesi
            var workTypesResult = await _lookupService.GetWorkTypesAsync();
            ViewBag.WorkTypes = workTypesResult.IsSuccess ? workTypesResult.Data : new List<WorkTypeDto>();

            // Mesai Tipi listesi
            var timeTypesResult = await _lookupService.GetTimeTypesAsync();
            ViewBag.TimeTypes = timeTypesResult.IsSuccess ? timeTypesResult.Data : new List<TimeTypeDto>();
        }

        private async Task<List<JobProductInput>> GetDefaultCreateProductsAsync()
        {
            var items = new List<JobProductInput>();

            foreach (var productCode in DefaultCreateProductCodes)
            {
                var result = await _productService.GetByFilterAsync(new ProductFilterDto
                {
                    Code = productCode,
                    IsInvalid = false,
                    Page = 1,
                    PageSize = 20,
                    First = 20
                });

                var product = result.IsSuccess && result.Data != null
                    ? result.Data.Items.FirstOrDefault(x => string.Equals(x.Code, productCode, StringComparison.OrdinalIgnoreCase))
                    : null;

                if (product == null)
                {
                    continue;
                }

                items.Add(new JobProductInput
                {
                    ProductId = product.Id,
                    Code = product.Code,
                    Name = product.Name,
                    CategoryId = product.CategoryId,
                    CategoryName = product.CategoryName,
                    Quantity = 1,
                    Price = product.Price,
                    NetAmount = product.Price
                });
            }

            return items;
        }

        private async Task<List<JobOvertimeInput>> GetDefaultCreateOvertimeTemplatesAsync(decimal? functionId)
        {
            var items = new List<JobOvertimeInput>();
            var fallbackWorkTypeId = (ViewBag.WorkTypes as List<WorkTypeDto>)?.FirstOrDefault()?.Id ?? 0m;
            var defaultTimeTypeId = (ViewBag.TimeTypes as List<TimeTypeDto>)?.FirstOrDefault()?.Id ?? 0m;

            foreach (var employeeCode in DefaultCreateProductCodes)
            {
                var result = await _employeeService.GetEmployeesAsync(new EmployeeFilterDto
                {
                    Code = employeeCode,
                    FunctionID = functionId,
                    Status = 1,
                    Page = 1,
                    PageSize = 20,
                    First = 20
                });

                var employee = result.IsSuccess && result.Data != null
                    ? result.Data.Items.FirstOrDefault(x => string.Equals(x.Code, employeeCode, StringComparison.OrdinalIgnoreCase))
                    : null;

                if (employee == null)
                {
                    continue;
                }

                items.Add(new JobOvertimeInput
                {
                    EmployeeId = employee.Id,
                    EmployeeCode = employee.Code,
                    EmployeeName = employee.Name,
                    WorkTypeId = employee.DefaultWorkTypeId ?? fallbackWorkTypeId,
                    TimeTypeId = defaultTimeTypeId,
                    Quantity = 1,
                    Amount = 0
                });
            }

            return items;
        }

        private static List<JobOvertimeInput> BuildInitialAfterHoursOvertimes(
            IEnumerable<JobOvertimeInput> templates,
            DateTime startDate,
            DateTime endDate)
        {
            if (!ShouldAddAfterHoursOvertime(startDate, endDate))
            {
                return new List<JobOvertimeInput>();
            }

            return templates
                .Select(x => new JobOvertimeInput
                {
                    EmployeeId = x.EmployeeId,
                    EmployeeCode = x.EmployeeCode,
                    EmployeeName = x.EmployeeName,
                    WorkTypeId = x.WorkTypeId,
                    TimeTypeId = x.TimeTypeId,
                    Quantity = x.Quantity,
                    Amount = x.Amount,
                    Notes = x.Notes
                })
                .ToList();
        }

        private static bool ShouldAddAfterHoursOvertime(DateTime startDate, DateTime endDate)
        {
            return IsAfterOrAtSixPm(startDate) || IsAfterOrAtSixPm(endDate);
        }

        private static bool IsAfterOrAtSixPm(DateTime value)
        {
            return value.TimeOfDay >= new TimeSpan(18, 0, 0);
        }

        private PrintableReportViewModel BuildDetailedPendingInvoicePrintableReport(
            List<PendingInvoiceJobsDetailedReportRowDto> rows,
            PendingInvoiceJobsReportDownloadRequest request)
        {
            var orderedRows = rows
                .OrderBy(x => x.CustomerName)
                .ThenBy(x => x.Reference)
                .ToList();

            var reportRows = orderedRows
                .Select(row => new PrintableReportRow
                {
                    Cells = new List<PrintableReportCell>
                    {
                        new() { Value = ResolveCustomerDisplay(row.CustomerName, row.CustomerCode) },
                        new() { Value = row.Reference.ToString(CultureInfo.CurrentCulture), Alignment = "right" },
                        new() { Value = row.JobName },
                        new() { Value = FormatDate(row.StartDate), Alignment = "center" },
                        new() { Value = row.EndDate.HasValue ? FormatDate(row.EndDate.Value) : "-", Alignment = "center" },
                        new() { Value = FormatAmount(row.Amount), Alignment = "right" }
                    }
                })
                .ToList();

            return new PrintableReportViewModel
            {
                Title = L("DetailedPendingInvoiceJobsReportTitle"),
                Orientation = "landscape",
                GeneratedAtDisplay = BuildGeneratedAtDisplay(),
                EmptyMessage = L("NoRecordsFound"),
                MetaItems = BuildPendingInvoiceMetaItems(request),
                Columns = new List<PrintableReportColumn>
                {
                    new() { Title = L("Customer") },
                    new() { Title = L("Reference"), Alignment = "right" },
                    new() { Title = L("Name") },
                    new() { Title = L("StartDate"), Alignment = "center" },
                    new() { Title = L("EndDate"), Alignment = "center" },
                    new() { Title = L("Amount"), Alignment = "right" }
                },
                Rows = reportRows
            };
        }

        private PrintableReportViewModel BuildSummaryPendingInvoicePrintableReport(
            List<PendingInvoiceJobsSummaryReportRowDto> rows,
            PendingInvoiceJobsReportDownloadRequest request)
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
                        new() { Value = FormatAmount(row.Amount), Alignment = "right" }
                    }
                })
                .ToList();

            return new PrintableReportViewModel
            {
                Title = L("SummaryPendingInvoiceJobsReportTitle"),
                Orientation = "portrait",
                GeneratedAtDisplay = BuildGeneratedAtDisplay(),
                EmptyMessage = L("NoRecordsFound"),
                MetaItems = BuildPendingInvoiceMetaItems(request),
                Columns = new List<PrintableReportColumn>
                {
                    new() { Title = L("Customer") },
                    new() { Title = L("Count"), Alignment = "right" },
                    new() { Title = L("Amount"), Alignment = "right" }
                },
                Rows = reportRows
            };
        }

        private PrintableReportViewModel BuildDetailedJobPrintableReport(
            List<JobDetailedReportRowDto> rows,
            JobFilterModel filter)
        {
            var orderedRows = rows
                .OrderBy(x => x.CustomerName)
                .ThenBy(x => x.StartDate)
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
                            new() { Value = row.FunctionName },
                            new() { Value = row.Reference.ToString(CultureInfo.CurrentCulture), Alignment = "right" },
                            new() { Value = row.JobName },
                            new() { Value = FormatDateTime(row.StartDate), Alignment = "center" },
                            new() { Value = row.EndDate.HasValue ? FormatDateTime(row.EndDate.Value) : "-", Alignment = "center" },
                            new() { Value = row.StatusName },
                            new() { IsCheckbox = true, IsChecked = row.IsEvaluated, Alignment = "center" },
                            new() { Value = FormatAmount(row.WorkAmount), Alignment = "right" },
                            new() { Value = FormatAmount(row.ProductAmount), Alignment = "right" }
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
                            ColSpan = 8,
                            Alignment = "right"
                        },
                        new() { Value = FormatAmount(customerGroup.Sum(x => x.WorkAmount)), Alignment = "right" },
                        new() { Value = FormatAmount(customerGroup.Sum(x => x.ProductAmount)), Alignment = "right" }
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
                        new() { Value = L("ReportTotal"), ColSpan = 8, Alignment = "right" },
                        new() { Value = FormatAmount(orderedRows.Sum(x => x.WorkAmount)), Alignment = "right" },
                        new() { Value = FormatAmount(orderedRows.Sum(x => x.ProductAmount)), Alignment = "right" }
                    }
                });
            }

            return new PrintableReportViewModel
            {
                Title = L("DetailedJobReportTitle"),
                Orientation = "landscape",
                GeneratedAtDisplay = BuildGeneratedAtDisplay(),
                EmptyMessage = L("NoRecordsFound"),
                MetaItems = BuildJobReportMetaItems(filter),
                Columns = new List<PrintableReportColumn>
                {
                    new() { Title = L("Customer") },
                    new() { Title = L("Function") },
                    new() { Title = L("Reference"), Alignment = "right" },
                    new() { Title = L("Name") },
                    new() { Title = L("StartDate"), Alignment = "center" },
                    new() { Title = L("EndDate"), Alignment = "center" },
                    new() { Title = L("Status") },
                    new() { Title = L("Evaluated"), Alignment = "center" },
                    new() { Title = L("WorkAmount"), Alignment = "right" },
                    new() { Title = L("ProductAmount"), Alignment = "right" }
                },
                Rows = reportRows
            };
        }

        private PrintableReportViewModel BuildSummaryJobPrintableReport(
            List<JobSummaryReportRowDto> rows,
            JobFilterModel filter)
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
                        new() { Value = FormatAmount(row.WorkAmount), Alignment = "right" },
                        new() { Value = FormatAmount(row.ProductAmount), Alignment = "right" }
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
                        new() { Value = FormatAmount(orderedRows.Sum(x => x.WorkAmount)), Alignment = "right" },
                        new() { Value = FormatAmount(orderedRows.Sum(x => x.ProductAmount)), Alignment = "right" }
                    }
                });
            }

            return new PrintableReportViewModel
            {
                Title = L("SummaryJobReportTitle"),
                Orientation = "portrait",
                GeneratedAtDisplay = BuildGeneratedAtDisplay(),
                EmptyMessage = L("NoRecordsFound"),
                MetaItems = BuildJobReportMetaItems(filter),
                Columns = new List<PrintableReportColumn>
                {
                    new() { Title = L("Customer") },
                    new() { Title = L("Count"), Alignment = "right" },
                    new() { Title = L("WorkAmount"), Alignment = "right" },
                    new() { Title = L("ProductAmount"), Alignment = "right" }
                },
                Rows = reportRows
            };
        }

        private List<PrintableReportMetaItem> BuildPendingInvoiceMetaItems(PendingInvoiceJobsReportDownloadRequest request)
        {
            return new List<PrintableReportMetaItem>
            {
                new()
                {
                    Label = L("CustomerWithColon"),
                    Value = ResolveDisplay(request.CustomerName, request.CustomerCode)
                }
            };
        }

        private List<PrintableReportMetaItem> BuildJobReportMetaItems(JobFilterModel filter)
        {
            var items = new List<PrintableReportMetaItem>();

            if (filter.StartDateStart.HasValue && filter.StartDateEnd.HasValue)
            {
                items.Add(new PrintableReportMetaItem
                {
                    Label = L("StartDateRange"),
                    Value = $"{FormatDate(filter.StartDateStart.Value)} - {FormatDate(filter.StartDateEnd.Value)}"
                });
            }

            if (filter.EndDateStart.HasValue && filter.EndDateEnd.HasValue)
            {
                items.Add(new PrintableReportMetaItem
                {
                    Label = L("EndDateRange"),
                    Value = $"{FormatDate(filter.EndDateStart.Value)} - {FormatDate(filter.EndDateEnd.Value)}"
                });
            }

            if (!string.IsNullOrWhiteSpace(filter.CustomerCode) || !string.IsNullOrWhiteSpace(filter.CustomerName))
            {
                items.Add(new PrintableReportMetaItem
                {
                    Label = L("CustomerWithColon"),
                    Value = ResolveDisplay(filter.CustomerName, filter.CustomerCode)
                });
            }

            if (!string.IsNullOrWhiteSpace(filter.EmployeeCode) || !string.IsNullOrWhiteSpace(filter.EmployeeName))
            {
                items.Add(new PrintableReportMetaItem
                {
                    Label = L("EmployeeWithColon"),
                    Value = ResolveDisplay(filter.EmployeeName, filter.EmployeeCode)
                });
            }

            if (!string.IsNullOrWhiteSpace(filter.ProductCode) || !string.IsNullOrWhiteSpace(filter.ProductName))
            {
                items.Add(new PrintableReportMetaItem
                {
                    Label = L("Product"),
                    Value = ResolveDisplay(filter.ProductName, filter.ProductCode)
                });
            }

            return items;
        }

        private string BuildGeneratedAtDisplay()
        {
            return DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
        }

        private JobPrintFormViewModel BuildJobPrintFormViewModel(JobDto job)
        {
            var products = job.JobProds ?? new List<JobProdDto>();
            var summarySource = BuildJobPrintFormSummary(job, products);

            return new JobPrintFormViewModel
            {
                Title = L("JobFormTitle"),
                GeneratedAtDisplay = BuildGeneratedAtDisplay(),
                FunctionReferenceDisplay = string.Concat(
                    string.IsNullOrWhiteSpace(job.FunctionName) ? L("Job") : job.FunctionName,
                    " \\ ",
                    job.Reference.ToString(CultureInfo.InvariantCulture)),
                CustomerName = SafeText(job.CustomerName),
                RelatedPerson = SafeText(job.Contact),
                Name = SafeText(job.Name),
                EmployeeNames = ResolveJobFormEmployees(job.JobWorks),
                Notes = ResolveJobFormNotes(job),
                StartDateDisplay = FormatDateTime(job.StartDate),
                EndDateDisplay = job.EndDate == default
                    ? "-"
                    : FormatDateTime(job.EndDate),
                VatNote = L("PricesExcludeVat"),
                TotalAmountDisplay = FormatAmount(summarySource.Sum(x => x.Amount)),
                Items = products.Select(x => new JobPrintFormLineItemViewModel
                {
                    Code = SafeText(x.ProductCode),
                    ProductName = SafeText(x.ProductName),
                    QuantityDisplay = FormatQuantity(x.Quantity),
                    AmountDisplay = FormatAmount(x.NetAmount),
                    Notes = SafeText(x.Notes)
                }).ToList(),
                SummaryItems = summarySource.Select(x => new JobPrintFormSummaryItemViewModel
                {
                    Label = x.Label,
                    AmountDisplay = FormatAmount(x.Amount)
                }).ToList()
            };
        }

        private static string ResolveCustomerDisplay(string? name, string? code)
        {
            return string.IsNullOrWhiteSpace(name) ? (code ?? string.Empty) : name;
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

        private static List<int> NormalizeSelectedReferences(IEnumerable<string>? selectedIds)
        {
            return (selectedIds ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => int.TryParse(x, out var reference) ? reference : (int?)null)
                .Where(x => x.HasValue && x.Value > 0)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();
        }

        private static string NormalizeListReturnUrl(string? returnUrl)
        {
            return !string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/')
                ? returnUrl
                : "/Job/List";
        }

        private JobEmailComposeViewModel BuildJobEmailComposeViewModel(
            JobEmailDraftDto draft,
            string returnUrl,
            string? recipientEmailOverride = null,
            IReadOnlyDictionary<string, string>? recipientOverrides = null)
        {
            var rows = BuildJobEmailRows(draft);

            return new JobEmailComposeViewModel
            {
                RecipientEmail = recipientEmailOverride ?? draft.RecipientEmail,
                ReturnUrl = NormalizeListReturnUrl(returnUrl),
                SelectedIds = rows.Select(x => x.Reference).ToList(),
                Jobs = rows.Select(x => new JobEmailComposeItemViewModel
                {
                    RecipientEmail = ResolveRecipientEmail(x.Reference, draft, recipientOverrides),
                    Function = x.Function,
                    Reference = x.Reference,
                    Customer = x.Customer,
                    Name = x.Name,
                    RelatedPerson = x.RelatedPerson,
                    Status = x.Status,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    EmailSent = x.EmailSent,
                    Evaluated = x.Evaluated,
                    WorkAmount = x.WorkAmount,
                    ProductAmount = x.ProductAmount
                }).ToList()
            };
        }

        private string BuildJobEmailSubject(JobEmailDraftDto draft)
        {
            var references = draft.Items.Select(x => x.Reference).ToList();
            var referenceLabel = references.Count <= 3
                ? string.Join(", ", references)
                : string.Concat(string.Join(", ", references.Take(3)), " +", references.Count - 3);

            return string.Format(L("JobEmailSubject"), referenceLabel);
        }

        private string BuildJobEmailBody(JobEmailDraftDto draft)
        {
            var references = draft.Items.Select(x => x.Reference.ToString(CultureInfo.InvariantCulture)).ToList();
            var builder = new StringBuilder();
            builder.Append("<html><body style=\"font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#0f172a;\">");
            builder.AppendFormat(CultureInfo.InvariantCulture, "<p>{0}</p>", WebUtility.HtmlEncode(L("JobEmailBodyIntro")));
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "<p style=\"margin:0 0 16px;color:#475569;\"><strong>{0}:</strong> {1}</p>",
                WebUtility.HtmlEncode(L("SelectedJobs")),
                WebUtility.HtmlEncode(string.Join(", ", references)));
            builder.AppendFormat(CultureInfo.InvariantCulture, "<p style=\"margin:0;color:#475569;\">{0}</p>", WebUtility.HtmlEncode(L("JobEmailBodyAttachmentNote")));
            builder.Append("</body></html>");
            return builder.ToString();
        }

        private async Task<ServiceResult<EmailMessageDto>> BuildJobFormEmailMessageAsync(JobEmailDraftDto draft, string recipientEmail)
        {
            var attachments = new List<EmailAttachmentDto>();

            foreach (var item in draft.Items)
            {
                var pdfResult = await BuildJobFormPdfAttachmentAsync(item.Reference);
                if (!pdfResult.IsSuccess || pdfResult.Data == null)
                {
                    return ServiceResult<EmailMessageDto>.Fail(pdfResult.Message ?? L("GenericError"));
                }

                attachments.Add(pdfResult.Data);
            }

            return ServiceResult<EmailMessageDto>.Success(new EmailMessageDto
            {
                To = recipientEmail.Trim(),
                Subject = BuildJobEmailSubject(draft),
                HtmlBody = BuildJobEmailBody(draft),
                Attachments = attachments
            });
        }

        private async Task<ServiceResult<EmailAttachmentDto>> BuildJobFormPdfAttachmentAsync(int reference)
        {
            var jobResult = await _jobService.GetByReferenceAsync(reference);
            if (!jobResult.IsSuccess || jobResult.Data == null)
            {
                return ServiceResult<EmailAttachmentDto>.Fail(L("JobFormPdfGenerationFailed"));
            }

            if (!IsPrintableJobFormState(jobResult.Data.StateId))
            {
                return ServiceResult<EmailAttachmentDto>.Fail(L("JobEmailEligibleStatesOnly"));
            }

            try
            {
                var pdfModel = BuildJobPrintFormViewModel(jobResult.Data);
                var pdfBytes = _jobFormPdfService.Build(pdfModel);

                return ServiceResult<EmailAttachmentDto>.Success(new EmailAttachmentDto
                {
                    FileName = BuildJobFormAttachmentFileName(reference),
                    ContentType = "application/pdf",
                    Content = pdfBytes
                });
            }
            catch
            {
                return ServiceResult<EmailAttachmentDto>.Fail(L("JobFormPdfGenerationFailed"));
            }
        }

        private List<JobEmailDisplayRow> BuildJobEmailRows(JobEmailDraftDto draft)
        {
            return draft.Items.Select(x => new JobEmailDisplayRow
            {
                Function = x.FunctionName,
                Reference = x.Reference.ToString(CultureInfo.InvariantCulture),
                Customer = ResolveCustomerDisplay(x.CustomerName, x.CustomerCode),
                Name = x.Name,
                RelatedPerson = x.RelatedPerson,
                Status = NormalizeJobStatusName(x.StateId, x.StatusName),
                StartDate = FormatDateTime(x.StartDate),
                EndDate = x.EndDate.HasValue ? FormatDateTime(x.EndDate.Value) : "-",
                EmailSent = BoolLabel(x.IsEmailSent),
                Evaluated = BoolLabel(x.IsEvaluated),
                WorkAmount = FormatAmount(x.WorkAmount),
                ProductAmount = FormatAmount(x.ProductAmount)
            }).ToList();
        }

        private static Dictionary<string, string> BuildRecipientOverrideMap(IEnumerable<JobEmailComposeItemViewModel>? jobs)
        {
            return (jobs ?? Enumerable.Empty<JobEmailComposeItemViewModel>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Reference))
                .Select(x => new
                {
                    Reference = x.Reference!,
                    RecipientEmail = x.RecipientEmail
                })
                .GroupBy(x => x.Reference, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.Last().RecipientEmail ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);
        }

        private static JobEmailDraftDto CreateSingleJobEmailDraft(JobEmailItemDto item, string recipientEmail)
        {
            return new JobEmailDraftDto
            {
                RecipientEmail = recipientEmail,
                Items = new List<JobEmailItemDto> { item }
            };
        }

        private static string ResolveRecipientEmail(
            string reference,
            JobEmailDraftDto draft,
            IReadOnlyDictionary<string, string>? recipientOverrides)
        {
            if (recipientOverrides != null && recipientOverrides.TryGetValue(reference, out var overrideEmail))
            {
                return overrideEmail ?? string.Empty;
            }

            var item = draft.Items.FirstOrDefault(x => x.Reference.ToString(CultureInfo.InvariantCulture) == reference);
            if (item == null)
            {
                return draft.RecipientEmail;
            }

            return !string.IsNullOrWhiteSpace(item.CustomerEmail)
                ? item.CustomerEmail
                : draft.RecipientEmail;
        }

        private static bool IsValidEmailAddress(string? emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                return false;
            }

            try
            {
                _ = new MailAddress(emailAddress.Trim());
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string BoolLabel(bool value)
        {
            return value ? L("Yes") : L("No");
        }

        private sealed class JobEmailDisplayRow
        {
            public string Function { get; init; } = string.Empty;
            public string Reference { get; init; } = string.Empty;
            public string Customer { get; init; } = string.Empty;
            public string Name { get; init; } = string.Empty;
            public string RelatedPerson { get; init; } = string.Empty;
            public string Status { get; init; } = string.Empty;
            public string StartDate { get; init; } = string.Empty;
            public string EndDate { get; init; } = string.Empty;
            public string EmailSent { get; init; } = string.Empty;
            public string Evaluated { get; init; } = string.Empty;
            public string WorkAmount { get; init; } = string.Empty;
            public string ProductAmount { get; init; } = string.Empty;
        }

        private static string FormatDate(DateTime value)
        {
            return value.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture);
        }

        private static string FormatDateTime(DateTime value)
        {
            return value.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
        }

        private static string FormatQuantity(decimal value)
        {
            return value.ToString("0.##", CultureInfo.CurrentCulture);
        }

        private static string BuildJobFormAttachmentFileName(int reference)
        {
            return $"job-form-{reference.ToString(CultureInfo.InvariantCulture)}.pdf";
        }

        private static string FormatAmount(decimal value)
        {
            return value.ToString("N2", CultureInfo.CurrentCulture);
        }

        private static bool IsPrintableJobFormState(decimal stateId)
        {
            return stateId == 120m || stateId == 130m || stateId == 140m;
        }

        private static string ResolveJobFormEmployees(IEnumerable<JobWorkDto>? jobWorks)
        {
            var employeeNames = (jobWorks ?? Enumerable.Empty<JobWorkDto>())
                .Select(x => x.EmployeeName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .Distinct()
                .ToList();

            return employeeNames.Count > 0
                ? string.Join(", ", employeeNames)
                : "-";
        }

        private static string ResolveJobFormNotes(JobDto job)
        {
            var parts = new[] { job.ExtNotes, job.IntNotes }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct()
                .ToList();

            return parts.Count > 0
                ? string.Join(Environment.NewLine, parts)
                : "-";
        }

        private static List<(string Label, decimal Amount)> BuildJobPrintFormSummary(
            JobDto job,
            IEnumerable<JobProdDto> products)
        {
            var categorySummary = (job.JobProdCats ?? new List<JobProdCatDto>())
                .Where(x => !string.IsNullOrWhiteSpace(x.CategoryName))
                .GroupBy(x => x.CategoryName!.Trim())
                .Select(x => (Label: x.Key, Amount: x.Sum(y => y.NetAmount)))
                .Where(x => x.Amount != 0)
                .ToList();

            if (categorySummary.Count > 0)
            {
                return categorySummary;
            }

            return products
                .GroupBy(x => !string.IsNullOrWhiteSpace(x.CategoryName) ? x.CategoryName!.Trim() : SafeText(x.ProductName))
                .Select(x => (Label: x.Key, Amount: x.Sum(y => y.NetAmount)))
                .Where(x => x.Amount != 0)
                .ToList();
        }

        private static string SafeText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }

        private void ApplyFunctionSelection(JobCreateViewModel model)
        {
            var functions = ViewBag.Functions as List<FunctionDto> ?? new List<FunctionDto>();
            var selectedFunction = ResolveFunctionSelection(model.FunctionId, functions);

            model.FunctionId = selectedFunction?.Id;
            model.FunctionName = selectedFunction?.Name ?? "Mesai";
        }

        private static FunctionDto? ResolveFunctionSelection(decimal? requestedFunctionId, List<FunctionDto> functions)
        {
            if (requestedFunctionId.HasValue
                && requestedFunctionId.Value > 0
                && functions.Any(x => x.Id == requestedFunctionId.Value))
            {
                return functions.First(x => x.Id == requestedFunctionId.Value);
            }

            return functions.FirstOrDefault();
        }

        private async Task<bool> CanUseCustomerLookupAsync()
        {
            return await _permissionViewService.CanExecuteMethodAsync(1397m)
                || await _permissionViewService.CanExecuteMethodAsync(1397m, write: true);
        }

        private async Task<bool> CanUseProductLookupAsync()
        {
            return await _permissionViewService.CanExecuteMethodAsync(1397m)
                || await _permissionViewService.CanExecuteMethodAsync(1397m, write: true);
        }

        private static CustomerFilterDto BuildCustomerFilter(CustomerFilterModel filter)
        {
            var hasStateId = decimal.TryParse(filter.JobStatus, out var stateId);

            return new CustomerFilterDto
            {
                Code = filter.Code,
                Name = filter.Name,
                City = filter.City,
                AreaCode = filter.AreaCode,
                Country = filter.Country,
                Owner = filter.Owner,
                RelatedPerson = filter.RelatedPerson,
                Phone = filter.Phone,
                Fax = filter.Fax,
                Email = filter.Email,
                TaxOffice = filter.TaxOffice,
                TaxNumber = filter.TaxNumber,
                JobStatus = filter.JobStatus,
                JobStateId = hasStateId ? stateId : null,
                IsInvalid = filter.IsInvalid,
                Page = filter.Page > 0 ? filter.Page : 1,
                PageSize = filter.PageSize > 0 ? filter.PageSize : 20,
                First = filter.First
            };
        }

        private static ProductFilterDto BuildProductFilter(ProductFilterModel filter)
        {
            return new ProductFilterDto
            {
                Code = filter.Code,
                Category = IsAllOption(filter.Category) ? null : filter.Category,
                ProductGroup = IsAllOption(filter.ProductGroup) ? null : filter.ProductGroup,
                Function = IsAllOption(filter.Function) ? null : filter.Function,
                IsInvalid = filter.IsInvalid,
                Page = filter.Page > 0 ? filter.Page : 1,
                PageSize = filter.PageSize > 0 ? filter.PageSize : 10,
                First = filter.First
            };
        }

        private static bool IsAllOption(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            return value.Equals("Tümü", StringComparison.OrdinalIgnoreCase)
                || value.Equals("Tumu", StringComparison.OrdinalIgnoreCase)
                || value.Equals("All", StringComparison.OrdinalIgnoreCase);
        }

        private static JobFilterDto BuildJobFilterDto(JobFilterModel filter, bool includeFirst)
        {
            return new JobFilterDto
            {
                FunctionId = decimal.TryParse(filter.Function, out var funcId) ? funcId : null,
                CustomerId = filter.CustomerId,
                CustomerCode = filter.CustomerCode,
                ReferenceStart = int.TryParse(filter.ReferenceStart, out var refStart) ? refStart : null,
                ReferenceEnd = int.TryParse(filter.ReferenceEnd, out var refEnd) ? refEnd : null,
                ReferenceList = filter.ReferenceList,
                JobName = filter.JobName,
                RelatedPerson = filter.RelatedPerson,
                StartDateStart = filter.StartDateStart,
                StartDateEnd = filter.StartDateEnd,
                EndDateStart = filter.EndDateStart,
                EndDateEnd = filter.EndDateEnd,
                StateId = decimal.TryParse(filter.Status, out var stateId) ? stateId : null,
                IsEmailSent = filter.EmailSent switch
                {
                    "true" => true,
                    "false" => false,
                    _ => null
                },
                IsEvaluated = filter.Evaluated switch
                {
                    "true" => true,
                    "false" => false,
                    _ => null
                },
                ProductId = filter.ProductId,
                EmployeeCode = filter.EmployeeCode,
                WorkTypeId = decimal.TryParse(filter.TaskType, out var workTypeId) ? workTypeId : null,
                TimeTypeId = decimal.TryParse(filter.OvertimeType, out var timeTypeId) ? timeTypeId : null,
                Page = filter.Page > 0 ? filter.Page : 1,
                PageSize = filter.PageSize > 0 ? filter.PageSize : 10,
                First = includeFirst ? filter.First : null
            };
        }

        private bool TryValidateJobReportDateRanges(JobFilterModel filter, out IActionResult? badRequestResult)
        {
            badRequestResult = null;

            var hasStartRange = filter.StartDateStart.HasValue && filter.StartDateEnd.HasValue;
            var hasEndRange = filter.EndDateStart.HasValue && filter.EndDateEnd.HasValue;
            var hasStartRangeInput = filter.StartDateStart.HasValue || filter.StartDateEnd.HasValue;
            var hasEndRangeInput = filter.EndDateStart.HasValue || filter.EndDateEnd.HasValue;

            if (hasStartRangeInput && !hasStartRange)
            {
                badRequestResult = new BadRequestObjectResult(L("StartDateRangeInvalid"));
                return false;
            }

            if (hasEndRangeInput && !hasEndRange)
            {
                badRequestResult = new BadRequestObjectResult(L("EndDateRangeInvalid"));
                return false;
            }

            if (!hasStartRange && !hasEndRange)
            {
                badRequestResult = new BadRequestObjectResult(L("StartEndDateRequired"));
                return false;
            }

            if (hasStartRange && filter.StartDateEnd!.Value.Date < filter.StartDateStart!.Value.Date)
            {
                badRequestResult = new BadRequestObjectResult(L("EndDateBeforeStart"));
                return false;
            }

            if (hasEndRange && filter.EndDateEnd!.Value.Date < filter.EndDateStart!.Value.Date)
            {
                badRequestResult = new BadRequestObjectResult(L("EndDateBeforeStart"));
                return false;
            }

            return true;
        }

        private IActionResult RedirectToDetailWithContext(JobWorkflowActionRequest request)
        {
            var selectedIds = request.SelectedIds?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList() ?? new List<string>();

            var referenceText = request.Reference.ToString();
            if (!selectedIds.Contains(referenceText))
            {
                selectedIds.Add(referenceText);
            }

            var currentIndex = request.CurrentIndex;
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }
            if (currentIndex >= selectedIds.Count)
            {
                currentIndex = selectedIds.Count - 1;
            }

            var returnUrl = string.IsNullOrWhiteSpace(request.ReturnUrl) || !request.ReturnUrl.StartsWith('/')
                ? "/Job/List"
                : request.ReturnUrl;

            return RedirectToAction("Detail", new
            {
                id = referenceText,
                selectedIds,
                currentIndex,
                returnUrl
            });
        }

        private static decimal? ResolveWorkflowMethodId(JobWorkflowAction action)
        {
            return action switch
            {
                JobWorkflowAction.Complete => CompleteMethodId,
                JobWorkflowAction.UndoComplete => UndoCompleteMethodId,
                JobWorkflowAction.Price => PriceMethodId,
                JobWorkflowAction.UndoPrice => UndoPriceMethodId,
                JobWorkflowAction.Close => CloseMethodId,
                JobWorkflowAction.UndoClose => UndoCloseMethodId,
                JobWorkflowAction.Discard => DiscardMethodId,
                JobWorkflowAction.UndoDiscard => UndoDiscardMethodId,
                JobWorkflowAction.Evaluate => EvaluateMethodId,
                JobWorkflowAction.UndoEvaluate => UndoEvaluateMethodId,
                _ => null
            };
        }

        private static int GetJobProductDisplayRank(JobProductItem product)
        {
            var combinedText = NormalizeProductSortValue(string.Join(' ', product.Code, product.Name, product.CategoryName));

            if (combinedText.Contains("OPERATOR", StringComparison.Ordinal))
            {
                return 90;
            }

            if (combinedText.Contains("CAFE", StringComparison.Ordinal) ||
                combinedText.Contains("KAFE", StringComparison.Ordinal))
            {
                return 91;
            }

            if (combinedText.Contains("SUIT", StringComparison.Ordinal))
            {
                return 0;
            }

            return 10;
        }

        private static string NormalizeProductSortValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Normalize(NormalizationForm.FormD);
            var chars = normalized
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray();

            return new string(chars).Normalize(NormalizationForm.FormC).ToUpperInvariant();
        }

        private string L(string key)
        {
            return _localizer[key].Value;
        }

        private string BuildInvoiceDisplay(JobDto job)
        {
            if (job.InvoiceReference.HasValue)
            {
                if (!string.IsNullOrWhiteSpace(job.InvoiceName))
                {
                    return $"{job.InvoiceReference.Value} - {job.InvoiceName}";
                }

                return job.InvoiceReference.Value.ToString();
            }

            return job.HasInvoiceLink || job.InvoLineId.HasValue
                ? L("Billed")
                : "-";
        }

        private string NormalizeJobStatusName(decimal stateId, string? statusName)
        {
            return stateId == 1m
                ? L("OpenStatus")
                : (statusName ?? string.Empty);
        }

        private static string BuildFileName(string prefix)
        {
            return $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
        }
    }
}
