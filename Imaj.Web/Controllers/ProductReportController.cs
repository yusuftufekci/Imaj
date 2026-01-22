using Imaj.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Imaj.Web.Controllers
{
    public class ProductReportController : Controller
    {
        public IActionResult Index()
        {
            return View(new ProductReportViewModel());
        }
    }
}
