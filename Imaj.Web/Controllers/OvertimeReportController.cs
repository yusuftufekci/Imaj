using Imaj.Core.Constants;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Web.Authorization;
using Imaj.Web.Extensions;
using Imaj.Web.Models;
using Imaj.Web;
using Imaj.Web.Services.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace Imaj.Web.Controllers
{
    public class OvertimeReportController : Controller
    {
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly IJobService _jobService;
        private readonly ICustomerService _customerService;
        private readonly ILookupService _lookupService;
        private readonly IPermissionViewService _permissionViewService;
        private readonly IOvertimeReportExcelService _overtimeReportExcelService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public OvertimeReportController(
            IJobService jobService,
            ICustomerService customerService,
            ILookupService lookupService,
            IPermissionViewService permissionViewService,
            IOvertimeReportExcelService overtimeReportExcelService,
            IStringLocalizer<SharedResource> localizer)
        {
            _jobService = jobService;
            _customerService = customerService;
            _lookupService = lookupService;
            _permissionViewService = permissionViewService;
            _overtimeReportExcelService = overtimeReportExcelService;
            _localizer = localizer;
        }

        public IActionResult Index()
        {
            var model = new OvertimeReportViewModel();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CustomerJobStates()
        {
            if (!await CanUseCustomerLookupAsync())
            {
                return Forbid();
            }

            var result = await _lookupService.GetStatesAsync(StateCategories.Job);
            if (result.IsSuccess)
            {
                return Json(result.Data);
            }

            return BadRequest(this.LocalizeUiMessage(result.Message, L("GenericError")));
        }

        [HttpPost]
        public async Task<IActionResult> CustomerSearch([FromBody] CustomerFilterModel? filter)
        {
            if (!await CanUseCustomerLookupAsync())
            {
                return Forbid();
            }

            var f = filter ?? new CustomerFilterModel();
            f.Page = f.Page > 0 ? f.Page : 1;
            f.PageSize = f.PageSize > 0 ? f.PageSize : 20;
            f.First = f.First.HasValue && f.First.Value > 0 ? f.First.Value : null;

            var serviceFilter = BuildCustomerFilter(f);
            var result = await _customerService.GetByFilterAsync(serviceFilter);

            var items = result.IsSuccess && result.Data != null
                ? result.Data.Items.Select(c => new CustomerSearchResult
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    City = c.City,
                    Phone = c.Phone,
                    Email = c.Email
                }).ToList()
                : new List<CustomerSearchResult>();

            var totalCount = result.IsSuccess && result.Data != null ? result.Data.TotalCount : 0;
            return Json(new { items, totalCount, page = f.Page, pageSize = f.PageSize });
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
                return BadRequest(this.LocalizeUiMessage(reportResult.Message, L("ReportDataUnavailable")));
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
                return BadRequest(this.LocalizeUiMessage(reportResult.Message, L("ReportDataUnavailable")));
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
                return BadRequest(this.LocalizeUiMessage(reportResult.Message, L("ReportDataUnavailable")));
            }

            var fileBytes = _overtimeReportExcelService.BuildAdministrativeSummaryReport(reportResult.Data, excelContext);
            return File(fileBytes, ExcelContentType, BuildFileName(L("AdminSummaryOvertimeFilePrefix")));
        }

        [HttpGet]
        [RequireMethodPermission(1698)]
        public async Task<IActionResult> ViewDetailedReport([FromQuery] OvertimeReportDownloadRequest request)
        {
            if (!TryCreateReportContext(request, out var reportFilter, out var excelContext, out var badRequest))
            {
                return badRequest!;
            }

            var reportResult = await _jobService.GetDetailedOvertimeReportAsync(reportFilter);
            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(this.LocalizeUiMessage(reportResult.Message, L("ReportDataUnavailable")));
            }

            var model = BuildDetailedPrintableReport(reportResult.Data, excelContext);
            return View("~/Views/Shared/PrintableReport.cshtml", model);
        }

        [HttpGet]
        [RequireMethodPermission(1745)]
        public async Task<IActionResult> ViewSummaryReport([FromQuery] OvertimeReportDownloadRequest request)
        {
            if (!TryCreateReportContext(request, out var reportFilter, out var excelContext, out var badRequest))
            {
                return badRequest!;
            }

            var reportResult = await _jobService.GetSummaryOvertimeReportAsync(reportFilter);
            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(this.LocalizeUiMessage(reportResult.Message, L("ReportDataUnavailable")));
            }

            var model = BuildSummaryPrintableReport(reportResult.Data, excelContext);
            return View("~/Views/Shared/PrintableReport.cshtml", model);
        }

        [HttpGet]
        [RequireMethodPermission(2917)]
        public async Task<IActionResult> ViewAdministrativeSummaryReport([FromQuery] OvertimeReportDownloadRequest request)
        {
            if (!TryCreateReportContext(request, out var reportFilter, out var excelContext, out var badRequest))
            {
                return badRequest!;
            }

            var reportResult = await _jobService.GetAdministrativeSummaryOvertimeReportAsync(reportFilter);
            if (!reportResult.IsSuccess || reportResult.Data == null)
            {
                return BadRequest(this.LocalizeUiMessage(reportResult.Message, L("ReportDataUnavailable")));
            }

            var model = BuildAdministrativeSummaryPrintableReport(reportResult.Data, excelContext);
            return View("~/Views/Shared/PrintableReport.cshtml", model);
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

        private PrintableReportViewModel BuildDetailedPrintableReport(
            List<OvertimeReportRowDto> rows,
            OvertimeReportExcelContext context)
        {
            var orderedRows = rows
                .OrderBy(x => x.EmployeeName)
                .ThenBy(x => x.JobDate)
                .ThenBy(x => x.Reference)
                .ToList();

            var reportRows = new List<PrintableReportRow>();
            foreach (var employeeGroup in orderedRows.GroupBy(x => new { x.EmployeeCode, x.EmployeeName }))
            {
                foreach (var row in employeeGroup)
                {
                    var customerDisplay = string.IsNullOrWhiteSpace(row.CustomerName) ? row.CustomerCode : row.CustomerName;
                    reportRows.Add(new PrintableReportRow
                    {
                        Cells = new List<PrintableReportCell>
                        {
                            new() { Value = row.EmployeeName },
                            new() { Value = row.TimeTypeName },
                            new() { Value = row.WorkTypeName },
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
                            Value = string.Format(L("EmployeeTotalFormat"), employeeGroup.Key.EmployeeName),
                            ColSpan = 8,
                            Alignment = "right"
                        },
                        new() { Value = FormatQuantity(employeeGroup.Sum(x => x.Quantity)), Alignment = "right" },
                        new() { Value = FormatAmount(employeeGroup.Sum(x => x.Amount)), Alignment = "right" }
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
                        new() { Value = L("ReportTotal"), ColSpan = 8, Alignment = "right" },
                        new() { Value = FormatQuantity(orderedRows.Sum(x => x.Quantity)), Alignment = "right" },
                        new() { Value = FormatAmount(orderedRows.Sum(x => x.Amount)), Alignment = "right" }
                    }
                });
            }

            return new PrintableReportViewModel
            {
                Title = L("DetailedOvertimeReportTitle"),
                Orientation = "landscape",
                GeneratedAtDisplay = BuildGeneratedAtDisplay(),
                EmptyMessage = L("NoRecordsFound"),
                MetaItems = BuildOvertimeMetaItems(context),
                Columns = new List<PrintableReportColumn>
                {
                    new() { Title = L("Employee") },
                    new() { Title = L("OvertimeType") },
                    new() { Title = L("TaskType") },
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

        private PrintableReportViewModel BuildSummaryPrintableReport(
            List<OvertimeSummaryReportRowDto> rows,
            OvertimeReportExcelContext context)
        {
            var orderedRows = rows
                .OrderBy(x => x.EmployeeName)
                .ThenBy(x => x.TimeTypeName)
                .ThenBy(x => x.WorkTypeName)
                .ToList();

            var reportRows = new List<PrintableReportRow>();
            foreach (var employeeGroup in orderedRows.GroupBy(x => new { x.EmployeeCode, x.EmployeeName }))
            {
                foreach (var row in employeeGroup)
                {
                    reportRows.Add(new PrintableReportRow
                    {
                        Cells = new List<PrintableReportCell>
                        {
                            new() { Value = row.EmployeeName },
                            new() { Value = row.TimeTypeName },
                            new() { Value = row.WorkTypeName },
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
                            Value = string.Format(L("EmployeeTotalFormat"), employeeGroup.Key.EmployeeName),
                            ColSpan = 3,
                            Alignment = "right"
                        },
                        new() { Value = FormatQuantity(employeeGroup.Sum(x => x.Quantity)), Alignment = "right" },
                        new() { Value = FormatAmount(employeeGroup.Sum(x => x.Amount)), Alignment = "right" }
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
                        new() { Value = L("ReportTotal"), ColSpan = 3, Alignment = "right" },
                        new() { Value = FormatQuantity(orderedRows.Sum(x => x.Quantity)), Alignment = "right" },
                        new() { Value = FormatAmount(orderedRows.Sum(x => x.Amount)), Alignment = "right" }
                    }
                });
            }

            return new PrintableReportViewModel
            {
                Title = L("SummaryOvertimeReportTitle"),
                Orientation = "portrait",
                GeneratedAtDisplay = BuildGeneratedAtDisplay(),
                EmptyMessage = L("NoRecordsFound"),
                MetaItems = BuildOvertimeMetaItems(context),
                Columns = new List<PrintableReportColumn>
                {
                    new() { Title = L("Employee") },
                    new() { Title = L("OvertimeType") },
                    new() { Title = L("TaskType") },
                    new() { Title = L("Quantity"), Alignment = "right" },
                    new() { Title = L("Amount"), Alignment = "right" }
                },
                Rows = reportRows
            };
        }

        private PrintableReportViewModel BuildAdministrativeSummaryPrintableReport(
            List<OvertimeAdministrativeSummaryReportRowDto> rows,
            OvertimeReportExcelContext context)
        {
            var orderedRows = rows
                .OrderBy(x => x.EmployeeName)
                .ToList();

            var reportRows = orderedRows
                .Select(row => new PrintableReportRow
                {
                    Cells = new List<PrintableReportCell>
                    {
                        new() { Value = row.EmployeeName },
                        new() { Value = FormatQuantity(row.Quantity), Alignment = "right" },
                        new() { Value = FormatAmount(row.Amount), Alignment = "right" }
                    }
                })
                .ToList();

            if (orderedRows.Any())
            {
                reportRows.Add(new PrintableReportRow
                {
                    Kind = PrintableReportRowKind.GrandTotal,
                    Cells = new List<PrintableReportCell>
                    {
                        new() { Value = L("ReportTotal"), Alignment = "right" },
                        new() { Value = FormatQuantity(orderedRows.Sum(x => x.Quantity)), Alignment = "right" },
                        new() { Value = FormatAmount(orderedRows.Sum(x => x.Amount)), Alignment = "right" }
                    }
                });
            }

            return new PrintableReportViewModel
            {
                Title = L("AdminSummaryOvertimeReportTitle"),
                Orientation = "portrait",
                GeneratedAtDisplay = BuildGeneratedAtDisplay(),
                EmptyMessage = L("NoRecordsFound"),
                MetaItems = BuildOvertimeMetaItems(context),
                Columns = new List<PrintableReportColumn>
                {
                    new() { Title = L("Employee") },
                    new() { Title = L("Quantity"), Alignment = "right" },
                    new() { Title = L("Amount"), Alignment = "right" }
                },
                Rows = reportRows
            };
        }

        private List<PrintableReportMetaItem> BuildOvertimeMetaItems(OvertimeReportExcelContext context)
        {
            return new List<PrintableReportMetaItem>
            {
                new() { Label = L("DateRange"), Value = $"{FormatDate(context.StartDate)} - {FormatDate(context.EndDate)}" },
                new() { Label = L("EmployeeWithColon"), Value = context.EmployeeDisplay },
                new() { Label = L("CustomerWithColon"), Value = context.CustomerDisplay }
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

        private async Task<bool> CanUseCustomerLookupAsync()
        {
            return await _permissionViewService.CanExecuteMethodAsync(1698m)
                || await _permissionViewService.CanExecuteMethodAsync(1745m)
                || await _permissionViewService.CanExecuteMethodAsync(2917m);
        }

        private static CustomerFilterDto BuildCustomerFilter(CustomerFilterModel filter)
        {
            var hasStateId = decimal.TryParse(filter.JobStatus, out var stateId);

            return new CustomerFilterDto
            {
                Code = filter.Code,
                Name = filter.Name,
                City = filter.City,
                AreaCode = filter.AreaCode,
                Country = filter.Country,
                Owner = filter.Owner,
                RelatedPerson = filter.RelatedPerson,
                Phone = filter.Phone,
                Fax = filter.Fax,
                Email = filter.Email,
                TaxOffice = filter.TaxOffice,
                TaxNumber = filter.TaxNumber,
                JobStatus = filter.JobStatus,
                JobStateId = hasStateId ? stateId : null,
                IsInvalid = filter.IsInvalid,
                Page = filter.Page,
                PageSize = filter.PageSize,
                First = filter.First
            };
        }

        private string L(string key)
        {
            return _localizer[key].Value;
        }
    }
}
