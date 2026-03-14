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
        private readonly IProductService _productService;
        private readonly ILookupService _lookupService;
        private readonly IPendingInvoiceJobsReportExcelService _pendingInvoiceJobsReportExcelService;
        private readonly IJobReportExcelService _jobReportExcelService;
        private readonly IPermissionViewService _permissionViewService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public JobController(
            IJobService jobService,
            ICustomerService customerService,
            IProductService productService,
            ILookupService lookupService,
            IPendingInvoiceJobsReportExcelService pendingInvoiceJobsReportExcelService,
            IJobReportExcelService jobReportExcelService,
            IPermissionViewService permissionViewService,
            IStringLocalizer<SharedResource> localizer)
        {
            _jobService = jobService;
            _customerService = customerService;
            _productService = productService;
            _lookupService = lookupService;
            _pendingInvoiceJobsReportExcelService = pendingInvoiceJobsReportExcelService;
            _jobReportExcelService = jobReportExcelService;
            _permissionViewService = permissionViewService;
            _localizer = localizer;
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
            var historyItems = historyResult.IsSuccess && historyResult.Data != null
                ? historyResult.Data
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
        public async Task<IActionResult> ProductGroups()
        {
            if (!await CanUseProductLookupAsync())
            {
                return Forbid();
            }

            var result = await _productService.GetProductGroupsAsync();
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
                Function = job.FunctionName,
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
                    IsSelected = jw.SelectFlag,
                    EmployeeCode = jw.EmployeeCode,
                    EmployeeName = jw.EmployeeName,
                    TaskType = jw.WorkTypeName,
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
                model.Products = job.JobProds.Select(jp => new JobProductItem
                {
                    IsSelected = jp.SelectFlag,
                    Code = jp.ProductCode,
                    Name = jp.ProductName,
                    Quantity = jp.Quantity,
                    Price = jp.Price,
                    SubTotal = jp.GrossAmount,
                    NetTotal = jp.NetAmount,
                    Notes = jp.Notes
                }).ToList();

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
                IsEmailSent = model.IsEmailSent,
                IsEvaluated = model.IsEvaluated,
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
                    Quantity = x.Quantity,
                    Price = x.Price,
                    Notes = x.Notes
                }).ToList() ?? new List<JobProdDto>()
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
                    Quantity = 1,
                    Price = product.Price
                });
            }

            return items;
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

        private static string FormatDate(DateTime value)
        {
            return value.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture);
        }

        private static string FormatDateTime(DateTime value)
        {
            return value.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
        }

        private static string FormatAmount(decimal value)
        {
            return value.ToString("N2", CultureInfo.CurrentCulture);
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
