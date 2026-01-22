using Microsoft.AspNetCore.Mvc;
using Imaj.Web.Models;

namespace Imaj.Web.Controllers
{
    public class InvoiceController : Controller
    {
        public IActionResult Index()
        {
            var model = new InvoiceViewModel
            {
                IssueDateStart = DateTime.Now.AddDays(-30),
                IssueDateEnd = DateTime.Now
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult Search([FromBody] InvoiceViewModel filter)
        {
            // Mock Data Generation
            var rng = new Random();
            var items = Enumerable.Range(1, 15).Select(index => new InvoiceSearchResult
            {
                Reference = "REF-" + rng.Next(1000, 9999),
                JobCustomer = "Müşteri " + rng.Next(1, 5),
                InvoiceCustomer = "Fatura Müşterisi " + rng.Next(1, 5),
                IssueDate = DateTime.Now.AddDays(-index),
                Amount = rng.Next(100, 5000),
                Status = index % 2 == 0 ? "Ödendi" : "Bekliyor"
            }).ToList();

            var result = new
            {
                items = items,
                totalCount = 100,
                page = filter.Page
            };

            return Json(result);
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
            // Mock save logic
            return RedirectToAction("Index");
        }
    }
}
