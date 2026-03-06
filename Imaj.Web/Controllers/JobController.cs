using Imaj.Core.Constants;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Service.Results;
using Imaj.Web;
using Imaj.Web.Authorization;
using Imaj.Web.Models;
using Imaj.Web.Services.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Imaj.Web.Controllers
{
    public class JobController : Controller
    {
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private static readonly TimeSpan ReportExecutionTimeout = TimeSpan.FromSeconds(45);
        private static readonly decimal[] JobStateDisplayOrder = { 110m, 120m, 130m, 140m, 150m, 160m };
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
        private readonly ILookupService _lookupService;
        private readonly IPendingInvoiceJobsReportExcelService _pendingInvoiceJobsReportExcelService;
        private readonly IJobReportExcelService _jobReportExcelService;
        private readonly IPermissionViewService _permissionViewService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public JobController(
            IJobService jobService,
            ILookupService lookupService,
            IPendingInvoiceJobsReportExcelService pendingInvoiceJobsReportExcelService,
            IJobReportExcelService jobReportExcelService,
            IPermissionViewService permissionViewService,
            IStringLocalizer<SharedResource> localizer)
        {
            _jobService = jobService;
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
                return BadRequest(reportResult.Message ?? L("ReportDataUnavailable"));
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
                return BadRequest(reportResult.Message ?? L("ReportDataUnavailable"));
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
                return BadRequest(reportResult.Message ?? L("ReportDataUnavailable"));
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
                return BadRequest(reportResult.Message ?? L("ReportDataUnavailable"));
            }

            var fileBytes = _jobReportExcelService.BuildSummaryReport(reportResult.Data);
            return File(fileBytes, ExcelContentType, BuildFileName(L("SummaryJobReportFilePrefix")));
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
                ModelState.AddModelError(string.Empty, result.Message ?? L("DataLoadError"));
            }

            return View(model);
        }

        // Action to view detail. Supports navigation if multiple ids passed
        // Accepted via POST from List or GET from navigation links
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> Detail(string? id, string[]? selectedIds = null, int currentIndex = 0, string? returnUrl = null)
        {
            // Dropdown verilerini backend'den al
            await LoadDropdownDataAsync();
            
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

            // Referans numarasından iş bul
            if (!int.TryParse(id, out var reference))
            {
                return RedirectToAction("Index");
            }
            
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
                ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/Job/List" : returnUrl
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
            TempData[result.IsSuccess ? "SuccessMessage" : "ErrorMessage"] = result.Message ?? (result.IsSuccess ? L("SuccessTitle") : L("GenericError"));

            return RedirectToDetailWithContext(request);
        }

        /// <summary>
        /// Yeni iş yaratma sayfası.
        /// Dropdown verileri veritabanından yüklenir.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            // Dropdown verilerini backend'den al
            await LoadDropdownDataAsync();

            var model = new JobCreateViewModel();
            model.StartDate = DateTime.Now;
            model.EndDate = DateTime.Now;
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
                return View(model);
            }

            // Map View Model to DTO
            var jobDto = new JobDto
            {
                CustomerId = model.CustomerId,
                Name = model.Name,
                Contact = model.RelatedPerson,
                // Function Handling
                FunctionId = decimal.TryParse(model.Function, out var fId) ? fId : 1m,
                
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
                return Json(new { success = false, message = result.Message ?? L("SaveError") });
            }

            // TempData ile error mesajı set et
            TempData["ErrorMessage"] = result.Message ?? L("SaveError");
            ModelState.AddModelError(string.Empty, result.Message ?? L("SaveError"));
            await LoadDropdownDataAsync();
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
