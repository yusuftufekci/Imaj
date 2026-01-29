using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Imaj.Web.Controllers
{
    public class JobController : Controller
    {
        private readonly IJobService _jobService;

        public JobController(IJobService jobService)
        {
            _jobService = jobService;
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

        /// <summary>
        /// İş listesi sorgulaması.
        /// Veritabanından filtreleme ile getirir.
        /// </summary>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> List(JobViewModel model)
        {
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
                
                // Sayfalama
                Page = model.Filter.Page > 0 ? model.Filter.Page : 1,
                PageSize = model.Filter.PageSize > 0 ? model.Filter.PageSize : 10
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
                ModelState.AddModelError("", result.Message ?? "Veriler yüklenirken bir hata oluştu.");
            }

            return View(model);
        }

        // Action to view detail. Supports navigation if multiple ids passed
        // Accepted via POST from List or GET from navigation links
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> Detail(string? id, string[]? selectedIds = null, int currentIndex = 0)
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
            
            // Veritabanından getir
            var filter = new JobFilterDto
            {
                ReferenceStart = reference,
                ReferenceEnd = reference,
                PageSize = 1
            };
            
            var result = await _jobService.GetByFilterAsync(filter);
            
            if (!result.IsSuccess || result.Data == null || !result.Data.Items.Any())
            {
                return RedirectToAction("List");
            }
            
            var job = result.Data.Items.First();

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
                Status = job.StatusName,
                IsEmailSent = job.IsEmailSent,
                IsEvaluated = job.IsEvaluated,
                InvoiceStatus = job.InvoLineId.HasValue ? "Faturalandı" : "-",
                
                // Navigation state
                SelectedIds = selectedIds?.ToList() ?? new List<string> { id },
                CurrentIndex = currentIndex,
                TotalSelected = selectedIds?.Length ?? 1
            };

            // TODO: Mesai ve Ürün detayları ayrı bir servisten gelecek
            // Şimdilik boş bırakıyoruz

            return View(model);
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
        public IActionResult Create(JobCreateViewModel model)
        {
            // TODO: Gerçek kaydetme işlemi
            // Şimdilik Index'e yönlendir
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Dropdown verilerini veritabanından yükler ve ViewBag'e ekler.
        /// States, Functions, WorkTypes, TimeTypes
        /// </summary>
        private async Task LoadDropdownDataAsync()
        {
            // Durum listesi
            var statesResult = await _jobService.GetStatesAsync();
            ViewBag.States = statesResult.IsSuccess ? statesResult.Data : new List<StateDto>();

            // Fonksiyon listesi
            var functionsResult = await _jobService.GetFunctionsAsync();
            ViewBag.Functions = functionsResult.IsSuccess ? functionsResult.Data : new List<FunctionDto>();

            // Görev Tipi listesi
            var workTypesResult = await _jobService.GetWorkTypesAsync();
            ViewBag.WorkTypes = workTypesResult.IsSuccess ? workTypesResult.Data : new List<WorkTypeDto>();

            // Mesai Tipi listesi
            var timeTypesResult = await _jobService.GetTimeTypesAsync();
            ViewBag.TimeTypes = timeTypesResult.IsSuccess ? timeTypesResult.Data : new List<TimeTypeDto>();
        }
    }
}
