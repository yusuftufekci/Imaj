using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Web.Authorization;
using Imaj.Web.Models;
using Imaj.Web;
using Imaj.Web.Services.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Imaj.Web.Controllers
{
    public class OvertimeReportController : Controller
    {
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly IJobService _jobService;
        private readonly IOvertimeReportExcelService _overtimeReportExcelService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public OvertimeReportController(
            IJobService jobService,
            IOvertimeReportExcelService overtimeReportExcelService,
            IStringLocalizer<SharedResource> localizer)
        {
            _jobService = jobService;
            _overtimeReportExcelService = overtimeReportExcelService;
            _localizer = localizer;
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
                return BadRequest(reportResult.Message ?? L("ReportDataUnavailable"));
            }

            var fileBytes = _overtimeReportExcelService.BuildDetailedReport(reportResult.Data, excelContext);
            return File(fileBytes, ExcelContentType, BuildFileName(L("DetailedOvertimeFilePrefix")));
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
                return BadRequest(reportResult.Message ?? L("ReportDataUnavailable"));
            }

            var fileBytes = _overtimeReportExcelService.BuildSummaryReport(reportResult.Data, excelContext);
            return File(fileBytes, ExcelContentType, BuildFileName(L("SummaryOvertimeFilePrefix")));
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
                return BadRequest(reportResult.Message ?? L("ReportDataUnavailable"));
            }

            var fileBytes = _overtimeReportExcelService.BuildAdministrativeSummaryReport(reportResult.Data, excelContext);
            return File(fileBytes, ExcelContentType, BuildFileName(L("AdminSummaryOvertimeFilePrefix")));
        }

        private bool TryCreateReportContext(
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
                badRequestResult = new BadRequestObjectResult(L("StartEndDateRequired"));
                return false;
            }

            if (request.EndDate.Date < request.StartDate.Date)
            {
                badRequestResult = new BadRequestObjectResult(L("EndDateBeforeStart"));
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
                    : (employeeCodes.Any() ? string.Join(", ", employeeCodes) : L("AllOption")),
                CustomerDisplay = !string.IsNullOrWhiteSpace(request.CustomerName)
                    ? request.CustomerName!
                    : (!string.IsNullOrWhiteSpace(request.CustomerCode) ? request.CustomerCode! : L("AllOption"))
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

        private string L(string key)
        {
            return _localizer[key].Value;
        }
    }
}
