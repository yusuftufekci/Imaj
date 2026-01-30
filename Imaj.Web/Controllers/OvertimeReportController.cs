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
    }
}
