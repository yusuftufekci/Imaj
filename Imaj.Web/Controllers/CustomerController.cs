using Imaj.Core.Constants;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Web;
using Imaj.Web.Controllers.Base;
using Imaj.Web.Models;
using Imaj.Web.Services.Reports;
using Imaj.Web.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Imaj.Web.Controllers
{
    /// <summary>
    /// Müşteri (Customer) CRUD işlemleri için controller.
    /// NOTE: Dropdown verileri (States, ProductCategories) artık ILookupService'den alınıyor.
    /// </summary>
    public class CustomerController : BaseController
    {
        // BaseMeth IDs – CustomerQry container (177)
        private const int QueryMethodId = 1090;   // Query – ReadOnly
        private const int AddMethodId = 1076;      // Add
        private const int EditMethodId = 1078;     // Edit
        private const int BrowseMethodId = 1077;   // Browse – ReadOnly

        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly ICustomerService _customerService;
        private readonly ILookupService _lookupService;
        private readonly ICustomerReportExcelService _customerReportExcelService;

        public CustomerController(
            ICustomerService customerService,
            ILookupService lookupService,
            ICustomerReportExcelService customerReportExcelService,
            ILogger<CustomerController> logger,
            IStringLocalizer<SharedResource> localizer) : base(logger, localizer)
        {
            _customerService = customerService;
            _lookupService = lookupService;
            _customerReportExcelService = customerReportExcelService;
        }


        public async Task<IActionResult> Index()
        {
            // State listesini LookupService'den al (StateCategories constant kullanılıyor)
            var statesResult = await _lookupService.GetStatesAsync(StateCategories.Job);
            ViewBag.States = statesResult.IsSuccess ? statesResult.Data : new List<Imaj.Service.DTOs.StateDto>();
            
            return View(new CustomerFilterModel());
        }

        [HttpGet]
        [Route("Customer/GetProductCategories")]
        public async Task<IActionResult> GetProductCategories()
        {
            var result = await _lookupService.GetProductCategoriesAsync();
            if (result.IsSuccess)
            {
                return Json(result.Data);
            }
            return BadRequest(Imaj.Web.Extensions.ControllerMessageLocalizationExtensions.LocalizeUiMessage(this, result.Message));
        }

        [HttpGet]
        [Route("Customer/GetJobStates")]
        public async Task<IActionResult> GetJobStates()
        {
            var result = await _lookupService.GetStatesAsync(StateCategories.Job);
            if (result.IsSuccess)
            {
                return Json(result.Data);
            }
            return BadRequest(Imaj.Web.Extensions.ControllerMessageLocalizationExtensions.LocalizeUiMessage(this, result.Message));
        }

        [HttpPost]
        [RequireMethodPermission(QueryMethodId)]
        public async Task<IActionResult> Search([FromBody] CustomerFilterModel? filter)
        {
            var f = filter ?? new CustomerFilterModel();
            f.Page = f.Page > 0 ? f.Page : 1;
            f.PageSize = f.PageSize > 0 ? f.PageSize : 20;
            f.First = f.First.HasValue && f.First.Value > 0 ? f.First.Value : null;
            
            var serviceFilter = BuildServiceFilter(f, f.Page, f.PageSize > 0 ? f.PageSize : 10, f.First);

            var result = await _customerService.GetByFilterAsync(serviceFilter);
            
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
        [RequireMethodPermission(BrowseMethodId)]
        public async Task<IActionResult> List(CustomerFilterModel filter)
        {
            var f = filter ?? new CustomerFilterModel();
            f.Page = f.Page > 0 ? f.Page : 1;
            f.PageSize = f.PageSize > 0 ? f.PageSize : 20;
            f.First = f.First.HasValue && f.First.Value > 0 ? f.First.Value : null;

            var serviceFilter = BuildServiceFilter(f, f.Page, f.PageSize > 0 ? f.PageSize : 20, f.First);

            var result = await _customerService.GetByFilterAsync(serviceFilter);
             
            var items = result.IsSuccess && result.Data != null 
                ? result.Data.Items.Select(c => new CustomerViewModel 
                { 
                     Code = c.Code, 
                     Name = c.Name, 
                     RelatedPerson = c.Contact,
                     Phone = c.Phone, 
                     Email = c.Email,
                     Country = c.Country,
                     TaxOffice = c.TaxOffice,
                     TaxNumber = c.TaxNumber,
                     Owner = c.Owner,
                     JobStatus = c.SelectFlag ? "Active" : "Passive", // Map SelectFlag
                     IsInvalid = c.Invisible
                }).ToList() 
                : new List<CustomerViewModel>();

            var totalCount = result.IsSuccess && result.Data != null ? result.Data.TotalCount : 0;

            var model = new CustomerListViewModel
            {
                Items = items,
                Page = f.Page,
                PageSize = f.PageSize > 0 ? f.PageSize : 20,
                First = f.First,
                TotalCount = totalCount,
                Filter = f
            };

            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(QueryMethodId)]
        public async Task<IActionResult> DownloadReportExcel([FromQuery] CustomerFilterModel? filter)
        {
            var normalizedFilter = filter ?? new CustomerFilterModel();

            var allCustomers = await GetAllReportCustomersAsync(normalizedFilter);
            if (allCustomers == null)
            {
                return BadRequest(L("ReportDataUnavailable"));
            }

            var fileBytes = _customerReportExcelService.BuildReport(
                allCustomers,
                new CustomerReportExcelContext
                {
                    GeneratedAt = DateTime.Now
                });

            return File(fileBytes, ExcelContentType, BuildReportFileName());
        }

        [HttpGet]
        [RequireMethodPermission(QueryMethodId)]
        public async Task<IActionResult> ViewReport([FromQuery] CustomerFilterModel? filter)
        {
            var normalizedFilter = filter ?? new CustomerFilterModel();

            var allCustomers = await GetAllReportCustomersAsync(normalizedFilter);
            if (allCustomers == null)
            {
                return BadRequest(L("ReportDataUnavailable"));
            }

            var model = new PrintableReportViewModel
            {
                Title = L("CustomerInformationTitle"),
                Orientation = "landscape",
                GeneratedAtDisplay = BuildGeneratedAtDisplay(),
                EmptyMessage = L("NoRecordsFound"),
                MetaItems = BuildCustomerFilterMetaItems(normalizedFilter),
                Blocks = BuildCustomerBlocks(allCustomers)
            };

            return View("~/Views/Shared/PrintableReport.cshtml", model);
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

        private async Task<List<CustomerDto>?> GetAllReportCustomersAsync(CustomerFilterModel filter)
        {
            const int reportPageSize = 1000;
            var allCustomers = new List<CustomerDto>();
            var currentPage = 1;
            int? totalCount = null;

            while (true)
            {
                var serviceFilter = BuildServiceFilter(filter, currentPage, reportPageSize, null);
                var pageResult = await _customerService.GetByFilterAsync(serviceFilter);

                if (!pageResult.IsSuccess || pageResult.Data == null)
                {
                    return null;
                }

                totalCount ??= pageResult.Data.TotalCount;
                if (pageResult.Data.Items.Count == 0)
                {
                    break;
                }

                allCustomers.AddRange(pageResult.Data.Items);
                if (allCustomers.Count >= totalCount.Value)
                {
                    break;
                }

                currentPage++;
            }

            return allCustomers;
        }

        private static CustomerFilterDto BuildServiceFilter(CustomerFilterModel filter, int page, int pageSize, int? first)
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
                Page = page,
                PageSize = pageSize,
                First = first
            };
        }

        private string BuildReportFileName()
        {
            return $"{L("CustomerReportFilePrefix")}-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
        }

        private string BuildGeneratedAtDisplay()
        {
            return DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
        }

        private List<PrintableReportMetaItem> BuildCustomerFilterMetaItems(CustomerFilterModel filter)
        {
            var items = new List<PrintableReportMetaItem>();

            void AddIfHasValue(string label, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    items.Add(new PrintableReportMetaItem
                    {
                        Label = label,
                        Value = value
                    });
                }
            }

            AddIfHasValue(L("Code"), filter.Code);
            AddIfHasValue(L("Name"), filter.Name);
            AddIfHasValue(L("City"), filter.City);
            AddIfHasValue(L("AreaCode"), filter.AreaCode);
            AddIfHasValue(L("Country"), filter.Country);
            AddIfHasValue(L("Owner"), filter.Owner);
            AddIfHasValue(L("Related"), filter.RelatedPerson);
            AddIfHasValue(L("Phone"), filter.Phone);
            AddIfHasValue(L("Fax"), filter.Fax);
            AddIfHasValue(L("Email"), filter.Email);
            AddIfHasValue(L("TaxOffice"), filter.TaxOffice);
            AddIfHasValue(L("TaxNumber"), filter.TaxNumber);

            if (filter.IsInvalid.HasValue)
            {
                items.Add(new PrintableReportMetaItem
                {
                    Label = L("Invalid"),
                    Value = filter.IsInvalid.Value ? L("Yes") : L("No")
                });
            }

            return items;
        }

        private List<PrintableReportBlock> BuildCustomerBlocks(List<CustomerDto> customers)
        {
            return customers
                .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(customer => new PrintableReportBlock
                {
                    Title = string.IsNullOrWhiteSpace(customer.Code)
                        ? customer.Name
                        : $"{customer.Code} - {customer.Name}",
                    Subtitle = string.Join(" / ", new[] { customer.City, customer.Country }.Where(x => !string.IsNullOrWhiteSpace(x))),
                    Items = new List<PrintableReportMetaItem>
                    {
                        new() { Label = L("Owner"), Value = ValueOrDash(customer.Owner) },
                        new() { Label = L("Related"), Value = ValueOrDash(customer.Contact) },
                        new() { Label = L("Address"), Value = ValueOrDash(customer.Address) },
                        new() { Label = L("City"), Value = ValueOrDash(customer.City) },
                        new() { Label = L("AreaCode"), Value = ValueOrDash(customer.AreaCode) },
                        new() { Label = L("Country"), Value = ValueOrDash(customer.Country) },
                        new() { Label = L("Email"), Value = ValueOrDash(customer.Email) },
                        new() { Label = L("Fax"), Value = ValueOrDash(customer.Fax) },
                        new() { Label = L("Phone"), Value = ValueOrDash(customer.Phone) },
                        new() { Label = L("TaxOffice"), Value = ValueOrDash(customer.TaxOffice) },
                        new() { Label = L("TaxNumber"), Value = ValueOrDash(customer.TaxNumber) },
                        new() { Label = L("InvoiceName"), Value = ValueOrDash(customer.InvoiceName) },
                        new() { Label = L("Invalid"), Value = customer.Invisible ? L("Yes") : L("No") }
                    }
                })
                .ToList();
        }

        private static string ValueOrDash(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
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
                 Notes = c.Notes,
                 ProductCategories = c.ProductCategories.Select(pc => new ProductCategoryViewModel 
                 {
                     Id = pc.Id,
                     Name = pc.Name,
                     Discount = pc.Discount 
                 }).ToList()
             };
        }

        [HttpPost]
        [RequireMethodPermission(EditMethodId, write: true)]
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
                SelectFlag = !model.IsInvalid,
                ProductCategories = model.ProductCategories.Select(pc => new ProductCategoryDto 
                {
                    Id = pc.Id,
                    Discount = pc.Discount
                }).ToList()
            };

            var result = await _customerService.UpdateAsync(dto);
            if(result.IsSuccess)
            {
                 TempData["SuccessMessage"] = L("CustomerUpdatedSuccess");
                 return RedirectToAction("List");
            }
            
            TempData["ErrorMessage"] = Ui(result.Message, L("CustomerUpdateFailed"));
            ModelState.AddModelError(string.Empty, Ui(result.Message, L("GenericError")));
            return View("Edit", model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CustomerViewModel());
        }

        [HttpPost]
        [RequireMethodPermission(AddMethodId, write: true)]
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
                SelectFlag = !model.IsInvalid, // If IsInvalid is true, SelectFlag is false (Passive)? Logic needs to be consistent.
                // Actually, if IsInvalid is "Geçersiz", usually it means "Inactive" or "Hidden".
                // I'll assume IsInvalid == true => SelectFlag = false.
                ProductCategories = model.ProductCategories.Select(pc => new ProductCategoryDto 
                {
                    Id = pc.Id,
                    Discount = pc.Discount
                }).ToList()
            };

            var result = await _customerService.AddAsync(dto);
            if(result.IsSuccess)
            {
                 TempData["SuccessMessage"] = L("CustomerCreatedRecordSuccess");
                 return RedirectToAction("List"); 
            }
            
            TempData["ErrorMessage"] = Ui(result.Message, L("CustomerCreateFailed"));
            // Also add to ModelState to show in summary if desired
            ModelState.AddModelError(string.Empty, Ui(result.Message, L("GenericError")));
            return View(model);
        }
    }
}
