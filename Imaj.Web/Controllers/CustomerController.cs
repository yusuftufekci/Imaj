using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Web.Controllers.Base;
using Imaj.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Imaj.Web.Controllers
{
    /// <summary>
    /// Müşteri (Customer) CRUD işlemleri için controller.
    /// </summary>
    public class CustomerController : BaseController
    {
        private readonly ICustomerService _customerService;

        public CustomerController(
            ICustomerService customerService,
            ILogger<CustomerController> logger) : base(logger)
        {
            _customerService = customerService;
        }


        public IActionResult Index()
        {
            return View(new CustomerFilterModel());
        }

        [HttpPost]
        public async Task<IActionResult> Search([FromBody] CustomerFilterModel? filter)
        {
            var f = filter ?? new CustomerFilterModel();
            
            var serviceFilter = new CustomerFilterDto
            {
                Code = f.Code,
                Name = f.Name,
                City = f.City,
                AreaCode = f.AreaCode,
                Country = f.Country,
                Owner = f.Owner,
                RelatedPerson = f.RelatedPerson,
                Phone = f.Phone,
                Fax = f.Fax,
                Email = f.Email,
                TaxOffice = f.TaxOffice,
                TaxNumber = f.TaxNumber,
                JobStatus = f.JobStatus,
                IsInvalid = f.IsInvalid,
                Page = f.Page,
                PageSize = f.PageSize > 0 ? f.PageSize : 10
            };

            var result = await _customerService.GetByFilterAsync(serviceFilter);
            
            var items = result.IsSuccess && result.Data != null 
                ? result.Data.Items.Select(c => new CustomerSearchResult 
                { 
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
        public async Task<IActionResult> List(CustomerFilterModel filter)
        {
            var f = filter ?? new CustomerFilterModel();

             var serviceFilter = new CustomerFilterDto
            {
                Code = f.Code,
                Name = f.Name,
                City = f.City,
                AreaCode = f.AreaCode,
                Country = f.Country,
                Owner = f.Owner,
                RelatedPerson = f.RelatedPerson,
                Phone = f.Phone,
                Fax = f.Fax,
                Email = f.Email,
                TaxOffice = f.TaxOffice,
                TaxNumber = f.TaxNumber,
                JobStatus = f.JobStatus,
                IsInvalid = f.IsInvalid,
                Page = f.Page,
                PageSize = f.PageSize > 0 ? f.PageSize : 20
            };

            var result = await _customerService.GetByFilterAsync(serviceFilter);
             
            var items = result.IsSuccess && result.Data != null 
                ? result.Data.Items.Select(c => new CustomerViewModel 
                { 
                     Code = c.Code, 
                     Name = c.Name, 
                     City = c.City, 
                     Phone = c.Phone, 
                     Email = c.Email,
                     Country = c.Country,
                     TaxOffice = c.TaxOffice,
                     TaxNumber = c.TaxNumber,
                     Owner = c.Owner,
                     JobStatus = c.SelectFlag ? "Active" : "Passive", // Map SelectFlag
                     IsInvalid = !c.SelectFlag, // Rough mapping, or maybe Invisible?
                     // Note: ViewModel IsInvalid might strictly mean Invisible. 
                     // Let's check CustomerDto. CustomerDto doesn't expose Invisible, but we can assume we might need it.
                     // But for now, just mapping what's available.
                }).ToList() 
                : new List<CustomerViewModel>();

            var totalCount = result.IsSuccess && result.Data != null ? result.Data.TotalCount : 0;

            var model = new CustomerListViewModel
            {
                Items = items,
                Page = f.Page,
                PageSize = f.PageSize > 0 ? f.PageSize : 20,
                TotalCount = totalCount,
                Filter = f
            };

            return View(model);
        }

        public async Task<IActionResult> Details(string id)
        {
             // Try ID first from decimal
            if (decimal.TryParse(id, out decimal customerId))
            {
                var result = await _customerService.GetByIdAsync(customerId);
                if (result.IsSuccess && result.Data != null)
                     return View(MapToViewModel(result.Data));
            }

            // Try Code
            var codeResult = await _customerService.GetByCodeAsync(id);
            if (codeResult.IsSuccess && codeResult.Data != null)
                 return View(MapToViewModel(codeResult.Data));
             
             return NotFound();
        }

        public async Task<IActionResult> Edit(string id)
        {
             // Try ID first from decimal
            if (decimal.TryParse(id, out decimal customerId))
            {
                var result = await _customerService.GetByIdAsync(customerId);
                if (result.IsSuccess && result.Data != null)
                     return View(MapToViewModel(result.Data));
            }

            // Try Code
            var codeResult = await _customerService.GetByCodeAsync(id);
            if (codeResult.IsSuccess && codeResult.Data != null)
                 return View(MapToViewModel(codeResult.Data));
                 
             return NotFound();
        }

        private CustomerViewModel MapToViewModel(CustomerDto c)
        {
            return new CustomerViewModel 
             {
                 CustomerId = c.Id,
                 Code = c.Code, 
                 Name = c.Name, 
                 City = c.City, 
                 Phone = c.Phone, 
                 Email = c.Email,
                 Country = c.Country, 
                 TaxOffice = c.TaxOffice, 
                 TaxNumber = c.TaxNumber, 
                 Owner = c.Owner,
                 JobStatus = c.SelectFlag ? "Active" : "Passive",
                 Address = c.Address,
                 Notes = c.Notes
             };
        }

        [HttpPost]
        public async Task<IActionResult> Update(CustomerViewModel model) 
        {
            if (!ModelState.IsValid)
            {
                // Note: If we return View("Edit", model), we need to ensure Id binding error is cleared if it was the issue.
                // But renaming property fixes source of error.
                return View("Edit", model);
            }

            var dto = new CustomerDto
            {
                Id = model.CustomerId, // Essential
                Code = model.Code,
                Name = model.Name,
                City = model.City,
                Phone = model.Phone,
                Email = model.Email,
                Country = model.Country,
                TaxOffice = model.TaxOffice,
                TaxNumber = model.TaxNumber,
                Owner = model.Owner,
                Address = model.Address,
                InvoiceName = model.InvoiceName,
                Notes = model.Notes,
                AreaCode = model.AreaCode,
                Fax = model.Fax,
                Contact = model.RelatedPerson,
                SelectFlag = !model.IsInvalid
            };

            var result = await _customerService.UpdateAsync(dto);
            if(result.IsSuccess)
            {
                 TempData["SuccessMessage"] = "Müşteri başarıyla güncellendi.";
                 return RedirectToAction("List");
            }
            
            TempData["ErrorMessage"] = result.Message ?? "Müşteri güncellenirken bir hata oluştu.";
            ModelState.AddModelError("", result.Message ?? "Error");
            return View("Edit", model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CustomerViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(CustomerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // This will trigger the validation summary in the view.
                // If we also want a popup for validation errors, we can set a temp data or viewbag flag
                // OR handle it in the view by checking Model state.
                return View(model);
            }

            var dto = new CustomerDto
            {
                Code = model.Code,
                Name = model.Name,
                City = model.City,
                Phone = model.Phone,
                Email = model.Email,
                Country = model.Country,
                TaxOffice = model.TaxOffice,
                TaxNumber = model.TaxNumber,
                Owner = model.Owner,
                Address = model.Address, // Mapped
                InvoiceName = model.InvoiceName, // Mapped locally if DTO supported it, but let's check DTO
                Notes = model.Notes,
                AreaCode = model.AreaCode,
                Fax = model.Fax,
                Contact = model.RelatedPerson,
                SelectFlag = !model.IsInvalid // If IsInvalid is true, SelectFlag is false (Passive)? Logic needs to be consistent.
                // Actually, if IsInvalid is "Geçersiz", usually it means "Inactive" or "Hidden".
                // I'll assume IsInvalid == true => SelectFlag = false.
            };

            var result = await _customerService.AddAsync(dto);
            if(result.IsSuccess)
            {
                 TempData["SuccessMessage"] = "Müşteri Kaydı başarılı.";
                 return RedirectToAction("List"); 
            }
            
            TempData["ErrorMessage"] = result.Message ?? "Müşteri oluşturulurken bir hata oluştu.";
            // Also add to ModelState to show in summary if desired
            ModelState.AddModelError("", result.Message ?? "Error");
            return View(model);
        }
    }
}
