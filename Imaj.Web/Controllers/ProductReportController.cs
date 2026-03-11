using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Web.Authorization;
using Imaj.Web.Extensions;
using Imaj.Web;
using Imaj.Web.Models;
using Imaj.Web.Services.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Imaj.Web.Controllers
{
    public class ProductReportController : Controller
    {
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly IProductReportService _productReportService;
        private readonly IProductReportExcelService _productReportExcelService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ProductReportController(
            IProductReportService productReportService,
            IProductReportExcelService productReportExcelService,
            IStringLocalizer<SharedResource> localizer)
        {
            _productReportService = productReportService;
            _productReportExcelService = productReportExcelService;
            _localizer = localizer;
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
                return BadRequest(this.LocalizeUiMessage(reportResult.Message, L("ReportDataUnavailable")));
            }

            var fileBytes = _productReportExcelService.BuildDetailedReport(reportResult.Data, excelContext);
            return File(fileBytes, ExcelContentType, BuildFileName(L("DetailedProductFilePrefix")));
        }

        private bool TryCreateReportContext(
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
                badRequestResult = new BadRequestObjectResult(L("StartEndDateRequired"));
                return false;
            }

            if (request.EndDate.Date < request.StartDate.Date)
            {
                badRequestResult = new BadRequestObjectResult(L("EndDateBeforeStart"));
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
                ProductGroupDisplay = string.IsNullOrWhiteSpace(request.ProductGroup) ? L("AllOption") : request.ProductGroup!,
                ProductDisplay = string.IsNullOrWhiteSpace(request.ProductName)
                    ? (string.IsNullOrWhiteSpace(request.ProductCode) ? L("AllOption") : request.ProductCode!)
                    : request.ProductName!,
                CustomerDisplay = string.IsNullOrWhiteSpace(request.CustomerName)
                    ? (string.IsNullOrWhiteSpace(request.CustomerCode) ? L("AllOption") : request.CustomerCode!)
                    : request.CustomerName!
            };

            return true;
        }

        private static string BuildFileName(string prefix)
        {
            return $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
        }

        private string L(string key)
        {
            return _localizer[key].Value;
        }
    }
}
