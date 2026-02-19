using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Web.Authorization;
using Imaj.Web.Models;
using Imaj.Web.Services.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Imaj.Web.Controllers
{
    public class ProductReportController : Controller
    {
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly IProductReportService _productReportService;
        private readonly IProductReportExcelService _productReportExcelService;

        public ProductReportController(
            IProductReportService productReportService,
            IProductReportExcelService productReportExcelService)
        {
            _productReportService = productReportService;
            _productReportExcelService = productReportExcelService;
        }

        public IActionResult Index()
        {
            return View(new ProductReportViewModel());
        }

        [HttpGet]
        [RequireMethodPermission(1754)]
        public async Task<IActionResult> DownloadDetailedExcel([FromQuery] ProductReportDownloadRequest request)
        {
            if (!TryCreateReportContext(request, out var filter, out var excelContext, out var badRequest))
            {
                return badRequest!;
            }

            var reportResult = await _productReportService.GetDetailedReportAsync(filter);
            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(reportResult.Message ?? "Rapor verisi alınamadı.");
            }

            var fileBytes = _productReportExcelService.BuildDetailedReport(reportResult.Data, excelContext);
            return File(fileBytes, ExcelContentType, BuildFileName("detayli-urun-raporu"));
        }

        private static bool TryCreateReportContext(
            ProductReportDownloadRequest request,
            out ProductReportFilterDto filter,
            out ProductReportExcelContext excelContext,
            out IActionResult? badRequestResult)
        {
            filter = new ProductReportFilterDto();
            excelContext = new ProductReportExcelContext();
            badRequestResult = null;

            if (request.StartDate == default || request.EndDate == default)
            {
                badRequestResult = new BadRequestObjectResult("Başlangıç ve bitiş tarihi zorunludur.");
                return false;
            }

            if (request.EndDate.Date < request.StartDate.Date)
            {
                badRequestResult = new BadRequestObjectResult("Bitiş tarihi başlangıç tarihinden küçük olamaz.");
                return false;
            }

            filter = new ProductReportFilterDto
            {
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ProductGroupName = request.ProductGroup,
                ProductCode = request.ProductCode,
                CustomerCode = request.CustomerCode
            };

            excelContext = new ProductReportExcelContext
            {
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ProductGroupDisplay = string.IsNullOrWhiteSpace(request.ProductGroup) ? "Tümü" : request.ProductGroup!,
                ProductDisplay = string.IsNullOrWhiteSpace(request.ProductName)
                    ? (string.IsNullOrWhiteSpace(request.ProductCode) ? "Tümü" : request.ProductCode!)
                    : request.ProductName!,
                CustomerDisplay = string.IsNullOrWhiteSpace(request.CustomerName)
                    ? (string.IsNullOrWhiteSpace(request.CustomerCode) ? "Tümü" : request.CustomerCode!)
                    : request.CustomerName!
            };

            return true;
        }

        private static string BuildFileName(string prefix)
        {
            return $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
        }
    }
}
