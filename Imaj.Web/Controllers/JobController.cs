using Imaj.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Imaj.Web.Controllers
{
    public class JobController : Controller
    {
        // Static mock data to ensure consistency between List and Detail pages
        // In a real application, this would be replaced by a Service/Repository call.
        private static List<JobSearchResult> _mockData = new List<JobSearchResult>();

        static JobController()
        {
            // Initialize Mock Data
            var random = new Random(123); // Seed for reproducibility
            for(int i = 0; i < 20; i++)
            {
                var id = (14 + i).ToString();
                _mockData.Add(new JobSearchResult
                {
                    Code = id,
                    Function = i % 3 == 0 ? "Mesai" : (i % 3 == 1 ? "Aktarma" : "Dublaj"),
                    Name = $"Job {id} - " + (i % 2 == 0 ? "Bölüm 1" : "Fragman"),
                    CustomerName = i % 2 == 0 ? "MESAI - Imaj Mesai Sistemi" : "NETFLIX - Dublaj",
                    StartDate = DateTime.Now.AddDays(-i),
                    EndDate = DateTime.Now.AddDays(-i).AddHours(2),
                    Status = i % 4 == 0 ? "Tamamlandı" : "İptal edildi",
                    IsEmailSent = i % 2 == 0,
                    IsEvaluated = i % 3 == 0
                });
            }
        }

        public IActionResult Index()
        {
            var model = new JobViewModel();
            model.Filter.StartDateStart = DateTime.Now.AddMonths(-1);
            model.Filter.StartDateEnd = DateTime.Now;
            return View(model);
        }

        [AcceptVerbs("GET", "POST")]
        public IActionResult List(JobViewModel model)
        {
            // Return the shared mock data
            // On a GET (Back button), model will be empty but we want to show the list again.
            // Since _mockData is static, we can just reassign it.
            model.Items = _mockData;
            model.TotalCount = _mockData.Count; 

            return View(model);
        }

        // Action to view detail. Supports navigation if multiple ids passed (mock implementation)
        // Accepted via POST from List or GET from navigation links
        [AcceptVerbs("GET", "POST")]
        public IActionResult Detail(string? id, string[]? selectedIds = null, int currentIndex = 0)
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

            // Find the job in our shared mock source
            var job = _mockData.FirstOrDefault(x => x.Code == id);

            if (job == null)
            {
                // Handle not found
                 return RedirectToAction("List");
            }

            // Map shared data to Detail ViewModel
            var model = new JobDetailViewModel
            {
                Code = job.Code,
                Function = job.Function,
                CustomerName = job.CustomerName,
                Name = job.Name,
                RelatedPerson = "Eda Güremel", // Static for now as it wasn't in list model
                StartDate = job.StartDate,
                EndDate = job.EndDate,
                Status = job.Status,
                IsEmailSent = job.IsEmailSent,
                IsEvaluated = job.IsEvaluated,
                InvoiceStatus = "-",
                
                // Navigation state
                SelectedIds = selectedIds?.ToList() ?? new List<string> { id },
                CurrentIndex = currentIndex,
                TotalSelected = selectedIds?.Length ?? 1
            };

            // Add some mock overtimes consistent with the job
            var random = new Random(id.GetHashCode());
            int overtimeCount = random.Next(1, 4);
            for(int k=0; k<overtimeCount; k++)
            {
                model.Overtimes.Add(new JobOvertimeItem
                {
                    EmployeeCode = k % 2 == 0 ? "AAKSOY" : "MYILMAZ",
                    EmployeeName = k % 2 == 0 ? "Adnan Aksoy" : "Mehmet Yılmaz",
                    TaskType = "Personel",
                    OvertimeType = "Haftasonu Mesaisi",
                    Quantity = 2,
                    Amount = 3000,
                    Notes = ""
                });
            }

            return View(model);
        }

        public IActionResult Create()
        {
            var model = new JobCreateViewModel();
            model.StartDate = DateTime.Now;
            model.EndDate = DateTime.Now;
            return View(model);
        }

        [HttpPost]
        public IActionResult Create(JobCreateViewModel model)
        {
            // Mock Save
            // Redirect to List or Detail of new ID
            // For now back to Index
            return RedirectToAction("Index");
        }
    }
}
