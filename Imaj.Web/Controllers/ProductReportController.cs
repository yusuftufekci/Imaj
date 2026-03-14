using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Web.Authorization;
using Imaj.Web.Extensions;
using Imaj.Web;
using Imaj.Web.Models;
using Imaj.Web.Services.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;

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

        [HttpGet]
        [RequireMethodPermission(1754)]
        public async Task<IActionResult> ViewDetailedReport([FromQuery] ProductReportDownloadRequest request)
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

            var model = BuildDetailedPrintableReport(reportResult.Data, excelContext);
            return View("~/Views/Shared/PrintableReport.cshtml", model);
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

        private PrintableReportViewModel BuildDetailedPrintableReport(
            List<ProductReportRowDto> rows,
            ProductReportExcelContext context)
        {
            var orderedRows = rows
                .OrderBy(x => x.ProductGroupName)
                .ThenBy(x => x.ProductName)
                .ThenBy(x => x.JobDate)
                .ThenBy(x => x.Reference)
                .ToList();

            var reportRows = new List<PrintableReportRow>();
            foreach (var productGroup in orderedRows.GroupBy(x => new { x.ProductGroupName, x.ProductCode, x.ProductName }))
            {
                foreach (var row in productGroup)
                {
                    var customerDisplay = string.IsNullOrWhiteSpace(row.CustomerCode) ? row.CustomerName : row.CustomerCode;
                    reportRows.Add(new PrintableReportRow
                    {
                        Cells = new List<PrintableReportCell>
                        {
                            new() { Value = row.ProductGroupName },
                            new() { Value = row.ProductName },
                            new() { Value = row.Reference.ToString(CultureInfo.CurrentCulture), Alignment = "right" },
                            new() { Value = FormatDate(row.JobDate), Alignment = "center" },
                            new() { Value = customerDisplay },
                            new() { Value = row.JobName },
                            new() { Value = row.Notes },
                            new() { Value = FormatQuantity(row.Quantity), Alignment = "right" },
                            new() { Value = FormatAmount(row.Amount), Alignment = "right" }
                        }
                    });
                }

                reportRows.Add(new PrintableReportRow
                {
                    Kind = PrintableReportRowKind.GroupTotal,
                    Cells = new List<PrintableReportCell>
                    {
                        new()
                        {
                            Value = string.Format(L("EmployeeTotalFormat"), productGroup.Key.ProductName),
                            ColSpan = 7,
                            Alignment = "right"
                        },
                        new() { Value = FormatQuantity(productGroup.Sum(x => x.Quantity)), Alignment = "right" },
                        new() { Value = FormatAmount(productGroup.Sum(x => x.Amount)), Alignment = "right" }
                    }
                });
            }

            if (orderedRows.Any())
            {
                reportRows.Add(new PrintableReportRow
                {
                    Kind = PrintableReportRowKind.GrandTotal,
                    Cells = new List<PrintableReportCell>
                    {
                        new() { Value = L("ReportTotal"), ColSpan = 7, Alignment = "right" },
                        new() { Value = FormatQuantity(orderedRows.Sum(x => x.Quantity)), Alignment = "right" },
                        new() { Value = FormatAmount(orderedRows.Sum(x => x.Amount)), Alignment = "right" }
                    }
                });
            }

            return new PrintableReportViewModel
            {
                Title = L("DetailedProductReportTitle"),
                Orientation = "landscape",
                GeneratedAtDisplay = BuildGeneratedAtDisplay(),
                EmptyMessage = L("NoRecordsFound"),
                MetaItems = new List<PrintableReportMetaItem>
                {
                    new() { Label = L("DateRange"), Value = $"{FormatDate(context.StartDate)} - {FormatDate(context.EndDate)}" },
                    new() { Label = L("ProductGroupWithColon"), Value = context.ProductGroupDisplay },
                    new() { Label = L("ProductWithColon"), Value = context.ProductDisplay },
                    new() { Label = L("CustomerWithColon"), Value = context.CustomerDisplay }
                },
                Columns = new List<PrintableReportColumn>
                {
                    new() { Title = L("ProductGroupColumn") },
                    new() { Title = L("ProductColumn") },
                    new() { Title = L("Reference"), Alignment = "right" },
                    new() { Title = L("Date"), Alignment = "center" },
                    new() { Title = L("Customer") },
                    new() { Title = L("Name") },
                    new() { Title = L("Notes") },
                    new() { Title = L("Quantity"), Alignment = "right" },
                    new() { Title = L("Amount"), Alignment = "right" }
                },
                Rows = reportRows
            };
        }

        private string BuildGeneratedAtDisplay()
        {
            return DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
        }

        private static string FormatDate(DateTime value)
        {
            return value.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture);
        }

        private static string FormatAmount(decimal value)
        {
            return value.ToString("N2", CultureInfo.CurrentCulture);
        }

        private static string FormatQuantity(decimal value)
        {
            return value.ToString("#,##0.##", CultureInfo.CurrentCulture);
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
