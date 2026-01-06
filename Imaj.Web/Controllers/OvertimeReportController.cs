using Imaj.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Imaj.Web.Controllers
{
    public class OvertimeReportController : Controller
    {
        public IActionResult Index()
        {
            var model = new OvertimeReportViewModel();
            return View(model);
        }

        [HttpGet]
        public IActionResult SearchEmployees(string term, int page = 1, int pageSize = 10)
        {
            // Mock large dataset
            var employees = GenerateMockEmployees();

            if (!string.IsNullOrWhiteSpace(term))
            {
                employees = employees.Where(e => 
                    e.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || 
                    e.Code.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var totalCount = employees.Count;
            var items = employees
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Json(new { items, totalCount, page, pageSize });
        }

        [HttpPost]
        public IActionResult SearchCustomers([FromBody] CustomerFilterModel filter)
        {
            // Mock dataset
            var customers = GenerateMockCustomers();

            if (filter != null)
            {
               if(!string.IsNullOrWhiteSpace(filter.Code))
                    customers = customers.Where(c => c.Code.Contains(filter.Code, StringComparison.OrdinalIgnoreCase)).ToList();

               if(!string.IsNullOrWhiteSpace(filter.Name))
                    customers = customers.Where(c => c.Name.Contains(filter.Name, StringComparison.OrdinalIgnoreCase)).ToList();
               
               if(!string.IsNullOrWhiteSpace(filter.City))
                    customers = customers.Where(c => c.City.Contains(filter.City, StringComparison.OrdinalIgnoreCase)).ToList();
                
                // Add more filters as needed for mock...
            }
            
            var totalCount = customers.Count;
            // Assuming simplified paging for customers similar to employees if needed, 
            // otherwise returning all filtered results if logic requires.
            // But let's apply paging to be consistent with large datasets.
            var items = customers
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return Json(new { items, totalCount, page = filter.Page, pageSize = filter.PageSize });
        }

        private List<EmployeeSearchResult> GenerateMockEmployees()
        {
            var list = new List<EmployeeSearchResult>();
            // Generate 50 dummy employees
            for (int i = 1; i <= 50; i++)
            {
                list.Add(new EmployeeSearchResult { Code = $"EMP{i:000}", Name = $"Employee {i} Name" });
            }
            // Add specific ones for search testing
            list.Add(new EmployeeSearchResult { Code = "AAKSOY", Name = "Adnan Aksoy" });
            list.Add(new EmployeeSearchResult { Code = "AAKTURK", Name = "Atilla Aktürk" });
            return list;
        }

        private List<CustomerSearchResult> GenerateMockCustomers()
        {
            return new List<CustomerSearchResult>
            {
                new CustomerSearchResult { Code = "CUST001", Name = "Acme Corp", City = "Istanbul", Phone="0212 123 45 67", Email="contact@acme.com" },
                new CustomerSearchResult { Code = "CUST002", Name = "Global Media", City = "Ankara", Phone="0312 987 65 43", Email="info@global.com" },
                new CustomerSearchResult { Code = "CUST003", Name = "Tech Solutions", City = "Izmir", Phone="0232 555 11 22", Email="support@techsol.com" },
                new CustomerSearchResult { Code = "CUST004", Name = "Creative Arts", City = "Istanbul", Phone="0216 444 88 99", Email="art@creative.com" },
                new CustomerSearchResult { Code = "CUST005", Name = "Beta Ltd", City = "Bursa", Phone="0224 111 22 33", Email="beta@ltd.com" }
            };
        }
    }
}
