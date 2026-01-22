using Imaj.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Imaj.Web.Controllers
{
    public class CustomerController : Controller
    {
        public IActionResult Index()
        {
            return View(new CustomerFilterModel());
        }

        [HttpGet]
        public IActionResult List(CustomerFilterModel filter)
        {
            // Mock dataset
            var allCustomers = GenerateMockCustomers();
            var query = allCustomers.AsQueryable();

            if (filter != null)
            {
               if(!string.IsNullOrWhiteSpace(filter.Code))
                    query = query.Where(c => c.Code.Contains(filter.Code, StringComparison.OrdinalIgnoreCase));

               if(!string.IsNullOrWhiteSpace(filter.Name))
                    query = query.Where(c => c.Name.Contains(filter.Name, StringComparison.OrdinalIgnoreCase));
               
               if(!string.IsNullOrWhiteSpace(filter.City))
                    query = query.Where(c => c.City != null && c.City.Contains(filter.City, StringComparison.OrdinalIgnoreCase));
                
               // Add other filters as needed...
            }
            
            var totalCount = query.Count();
            var items = query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            var model = new CustomerListViewModel
            {
                Items = items,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount,
                Filter = filter
            };

            return View(model);
        }

        public IActionResult Details(string id)
        {
            var customer = GenerateMockCustomers().FirstOrDefault(c => c.Code == id);
            if(customer == null) return NotFound();
            return View(customer);
        }

        public IActionResult Edit(string id)
        {
            var customer = GenerateMockCustomers().FirstOrDefault(c => c.Code == id);
            if(customer == null) return NotFound();
            return View(customer);
        }

        [HttpPost]
        public IActionResult Update(CustomerViewModel model) // Use CustomerViewModel
        {
            // Mock update
            return RedirectToAction("Details", new { id = model.Code });
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CustomerViewModel());
        }

        [HttpPost]
        public IActionResult Create(CustomerViewModel model)
        {
            // Mock creation logic
            // In a real app, you would save to DB here.
            
            // Redirect to details of the new customer (mocked ID)
            return RedirectToAction("Details", new { id = model.Code });
        }

        private List<CustomerViewModel> GenerateMockCustomers()
        {
            var list = new List<CustomerViewModel>();
             list.Add(new CustomerViewModel { 
                 Code = "CUST001", Name = "Acme Corp", City = "Istanbul", Phone="0212 123 45 67", Email="contact@acme.com",
                 Country = "Turkey", TaxOffice = "Maslak", TaxNumber = "1234567890", JobStatus = "Active", Owner = "Ahmet Yilmaz"
             });
             list.Add(new CustomerViewModel { 
                 Code = "CUST002", Name = "Global Media", City = "Ankara", Phone="0312 987 65 43", Email="info@global.com",
                 Country = "Turkey", TaxOffice = "Cankaya", TaxNumber = "9876543210", JobStatus = "Completed", Owner = "Ayse Demir"
             });
             list.Add(new CustomerViewModel { 
                 Code = "CUST003", Name = "Tech Solutions", City = "Izmir", Phone="0232 555 11 22", Email="support@techsol.com",
                 Country = "Turkey", TaxOffice = "Konak", TaxNumber = "1122334455", JobStatus = "Active", Owner = "Mehmet Kaya"
             });
             list.Add(new CustomerViewModel { 
                 Code = "CUST004", Name = "Creative Arts", City = "Istanbul", Phone="0216 444 88 99", Email="art@creative.com",
                 Country = "Turkey", TaxOffice = "Kadikoy", TaxNumber = "5566778899", JobStatus = "Active", Owner = "Zeynep Celik"
             });
             list.Add(new CustomerViewModel { 
                 Code = "CUST005", Name = "Beta Ltd", City = "Bursa", Phone="0224 111 22 33", Email="beta@ltd.com",
                 Country = "Turkey", TaxOffice = "Nilufer", TaxNumber = "9988776655", JobStatus = "Completed", Owner = "Can Erol"
             });
             
            for(int i = 6; i < 55; i++)
            {
                 list.Add(new CustomerViewModel { 
                     Code = $"CUST{i:000}", Name = $"Müşteri {i}", City = "Istanbul", Phone="0212 000 00 00", Email=$"musteri{i}@mail.com",
                     Country = "Turkey", TaxOffice = "Sisli", TaxNumber = $"10000000{i:00}", JobStatus = "Active", Owner = "Satis Temsilcisi"
                 });
            }

            return list;
        }
    }
}
