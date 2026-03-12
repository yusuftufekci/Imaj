using Imaj.Core.Constants;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Web;
using Imaj.Web.Authorization;
using Imaj.Web.Extensions;
using Imaj.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Imaj.Web.Controllers
{
    public class JobEntryController : Controller
    {
        private const double QueryMethodId = 1397;
        private const double AddJobEntryMethodId = 1710;
        private const double ViewJobEntryMethodId = 1711;
        private static readonly string[] DefaultCreateProductCodes = { "OPERATOR", "CAFE" };

        private readonly IJobService _jobService;
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;
        private readonly ILookupService _lookupService;
        private readonly IPermissionViewService _permissionViewService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public JobEntryController(
            IJobService jobService,
            ICustomerService customerService,
            IProductService productService,
            ILookupService lookupService,
            IPermissionViewService permissionViewService,
            IStringLocalizer<SharedResource> localizer)
        {
            _jobService = jobService;
            _customerService = customerService;
            _productService = productService;
            _lookupService = lookupService;
            _permissionViewService = permissionViewService;
            _localizer = localizer;
        }



        /// <summary>
        /// İş geçmişini tam sayfa olarak görüntüler.
        /// </summary>
        [HttpGet]
        [RequireMethodPermission(ViewJobEntryMethodId)]
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
                Status = job.StatusName ?? string.Empty,
                IsEmailSent = job.IsEmailSent,
                IsEvaluated = job.IsEvaluated,
                InvoiceStatus = job.InvoLineId.HasValue ? L("Billed") : "-",
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

        /// <summary>
        /// İş listesi sorgulaması.
        /// Veritabanından filtreleme ile getirir.
        /// page ve pageSize URL query string'den ayrı alınabilir (pagination linkleri için)
        /// </summary>
        [AcceptVerbs("GET", "POST")]
        [RequireMethodPermission(QueryMethodId)]
        public async Task<IActionResult> List(JobViewModel model, int? page, int? pageSize, int? first)
        {
            // Pagination değerlerini URL'den gelen değerlerle override et
            // Bu sayede /JobEntry/List?page=2&pageSize=10 şeklinde linkler çalışır
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
            model.Filter.First = model.Filter.First.HasValue && model.Filter.First.Value > 0
                ? model.Filter.First.Value
                : (model.Filter.PageSize > 0 ? model.Filter.PageSize : 10);
            
            // Dropdown verilerini backend'den al (geri dönüşlerde gerekli)
            await LoadDropdownDataAsync();

            // Filtre DTO'sunu hazırla
            var filterDto = new JobFilterDto
            {
                // Fonksiyon ID string olarak geliyor, decimal'e çevir
                FunctionId = decimal.TryParse(model.Filter.Function, out var funcId) ? funcId : null,
                
                // Müşteri ID
                CustomerId = model.Filter.CustomerId,
                
                // Referans aralığı
                ReferenceStart = int.TryParse(model.Filter.ReferenceStart, out var refStart) ? refStart : null,
                ReferenceEnd = int.TryParse(model.Filter.ReferenceEnd, out var refEnd) ? refEnd : null,
                ReferenceList = model.Filter.ReferenceList,
                
                // İş bilgileri
                JobName = model.Filter.JobName,
                RelatedPerson = model.Filter.RelatedPerson,
                
                // Tarih aralıkları
                StartDateStart = model.Filter.StartDateStart,
                StartDateEnd = model.Filter.StartDateEnd,
                EndDateStart = model.Filter.EndDateStart,
                EndDateEnd = model.Filter.EndDateEnd,
                
                // Durum ID string olarak geliyor
                StateId = decimal.TryParse(model.Filter.Status, out var stateId) ? stateId : null,
                
                // Boolean filtreler
                IsEmailSent = model.Filter.EmailSent switch
                {
                    "true" => true,
                    "false" => false,
                    _ => null
                },
                IsEvaluated = model.Filter.Evaluated switch
                {
                    "true" => true,
                    "false" => false,
                    _ => null
                },
                
                // Ürün filtresi
                ProductId = model.Filter.ProductId,
                
                // Mesai Kriteri filtreleri
                EmployeeCode = model.Filter.EmployeeCode,
                WorkTypeId = decimal.TryParse(model.Filter.TaskType, out var workTypeId) ? workTypeId : null,
                TimeTypeId = decimal.TryParse(model.Filter.OvertimeType, out var timeTypeId) ? timeTypeId : null,
                
                // Sayfalama
                Page = model.Filter.Page > 0 ? model.Filter.Page : 1,
                PageSize = model.Filter.PageSize > 0 ? model.Filter.PageSize : 10,
                First = model.Filter.First
            };

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
                    Status = j.StatusName,
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
        [RequireMethodPermission(ViewJobEntryMethodId)]
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
                ? "/JobEntry/List"
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
                Status = job.StatusName,
                IsEmailSent = job.IsEmailSent,
                IsEvaluated = job.IsEvaluated,
                InvoiceStatus = job.InvoLineId.HasValue ? L("Billed") : "-",
                
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

        /// <summary>
        /// Yeni iş yaratma sayfası.
        /// Dropdown verileri veritabanından yüklenir.
        /// </summary>
        [RequireMethodPermission(AddJobEntryMethodId)]
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
        [RequireMethodPermission(AddJobEntryMethodId, write: true)]
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
            ViewBag.States = statesResult.IsSuccess ? statesResult.Data : new List<StateDto>();

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
            return await _permissionViewService.CanExecuteMethodAsync((decimal)QueryMethodId)
                || await _permissionViewService.CanExecuteMethodAsync((decimal)QueryMethodId, write: true)
                || await _permissionViewService.CanExecuteMethodAsync((decimal)AddJobEntryMethodId)
                || await _permissionViewService.CanExecuteMethodAsync((decimal)AddJobEntryMethodId, write: true);
        }

        private async Task<bool> CanUseProductLookupAsync()
        {
            return await _permissionViewService.CanExecuteMethodAsync((decimal)QueryMethodId)
                || await _permissionViewService.CanExecuteMethodAsync((decimal)QueryMethodId, write: true)
                || await _permissionViewService.CanExecuteMethodAsync((decimal)AddJobEntryMethodId)
                || await _permissionViewService.CanExecuteMethodAsync((decimal)AddJobEntryMethodId, write: true);
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

        private string L(string key)
        {
            return _localizer[key].Value;
        }
    }
}
