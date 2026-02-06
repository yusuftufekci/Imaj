using Imaj.Core.Constants;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Web.Controllers.Base;
using Imaj.Web.Models;
using Microsoft.AspNetCore.Mvc;
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
            ILogger<InvoiceController> logger) : base(logger)
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

        [HttpPost]
        public async Task<IActionResult> Search([FromBody] InvoiceViewModel? filter)
        {
            var f = filter ?? new InvoiceViewModel();

            var serviceFilter = new InvoiceFilterDto
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
                PageSize = f.PageSize > 0 ? f.PageSize : 10
            };

            var result = await _invoiceService.GetByFilterAsync(serviceFilter);

            var items = result.IsSuccess && result.Data != null
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
    }
}
