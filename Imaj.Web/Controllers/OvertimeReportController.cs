using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Web.Authorization;
using Imaj.Web.Models;
using Imaj.Web.Services.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Imaj.Web.Controllers
{
    public class OvertimeReportController : Controller
    {
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly IJobService _jobService;
        private readonly IOvertimeReportExcelService _overtimeReportExcelService;

        public OvertimeReportController(
            IJobService jobService,
            IOvertimeReportExcelService overtimeReportExcelService)
        {
            _jobService = jobService;
            _overtimeReportExcelService = overtimeReportExcelService;
        }

        public IActionResult Index()
        {
            var model = new OvertimeReportViewModel();
            return View(model);
        }

        [HttpGet]
        [RequireMethodPermission(1698)]
        public async Task<IActionResult> DownloadDetailedExcel([FromQuery] OvertimeReportDownloadRequest request)
        {
            if (!TryCreateReportContext(request, out var reportFilter, out var excelContext, out var badRequest))
            {
                return badRequest!;
            }

            var reportResult = await _jobService.GetDetailedOvertimeReportAsync(reportFilter);
            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(reportResult.Message ?? "Rapor verisi alınamadı.");
            }

            var fileBytes = _overtimeReportExcelService.BuildDetailedReport(reportResult.Data, excelContext);
            return File(fileBytes, ExcelContentType, BuildFileName("detayli-mesai-raporu"));
        }

        [HttpGet]
        [RequireMethodPermission(1745)]
        public async Task<IActionResult> DownloadSummaryExcel([FromQuery] OvertimeReportDownloadRequest request)
        {
            if (!TryCreateReportContext(request, out var reportFilter, out var excelContext, out var badRequest))
            {
                return badRequest!;
            }

            var reportResult = await _jobService.GetSummaryOvertimeReportAsync(reportFilter);
            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(reportResult.Message ?? "Rapor verisi alınamadı.");
            }

            var fileBytes = _overtimeReportExcelService.BuildSummaryReport(reportResult.Data, excelContext);
            return File(fileBytes, ExcelContentType, BuildFileName("ozet-mesai-raporu"));
        }

        [HttpGet]
        [RequireMethodPermission(2917)]
        public async Task<IActionResult> DownloadAdministrativeSummaryExcel([FromQuery] OvertimeReportDownloadRequest request)
        {
            if (!TryCreateReportContext(request, out var reportFilter, out var excelContext, out var badRequest))
            {
                return badRequest!;
            }

            var reportResult = await _jobService.GetAdministrativeSummaryOvertimeReportAsync(reportFilter);
            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(reportResult.Message ?? "Rapor verisi alınamadı.");
            }

            var fileBytes = _overtimeReportExcelService.BuildAdministrativeSummaryReport(reportResult.Data, excelContext);
            return File(fileBytes, ExcelContentType, BuildFileName("idari-ozet-mesai-raporu"));
        }

        private static bool TryCreateReportContext(
            OvertimeReportDownloadRequest request,
            out OvertimeReportFilterDto reportFilter,
            out OvertimeReportExcelContext excelContext,
            out IActionResult? badRequestResult)
        {
            reportFilter = new OvertimeReportFilterDto();
            excelContext = new OvertimeReportExcelContext();
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

            var employeeCodes = ParseCsv(request.EmployeeCodes);

            reportFilter = new OvertimeReportFilterDto
            {
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CustomerCode = request.CustomerCode,
                EmployeeCodes = employeeCodes
            };

            excelContext = new OvertimeReportExcelContext
            {
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                EmployeeDisplay = !string.IsNullOrWhiteSpace(request.EmployeeNames)
                    ? request.EmployeeNames!
                    : (employeeCodes.Any() ? string.Join(", ", employeeCodes) : "Tümü"),
                CustomerDisplay = !string.IsNullOrWhiteSpace(request.CustomerName)
                    ? request.CustomerName!
                    : (!string.IsNullOrWhiteSpace(request.CustomerCode) ? request.CustomerCode! : "Tümü")
            };

            return true;
        }

        private static List<string> ParseCsv(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new List<string>();
            }

            return input
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string BuildFileName(string prefix)
        {
            return $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
        }
    }
}
