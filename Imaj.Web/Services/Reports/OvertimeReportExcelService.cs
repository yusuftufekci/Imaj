using ClosedXML.Excel;
using Imaj.Service.DTOs;
using Imaj.Web;
using Microsoft.Extensions.Localization;

namespace Imaj.Web.Services.Reports
{
    public class OvertimeReportExcelService : IOvertimeReportExcelService
    {
        private readonly IStringLocalizer<SharedResource> _localizer;

        public OvertimeReportExcelService(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
        }

        public byte[] BuildDetailedReport(List<OvertimeReportRowDto> rows, OvertimeReportExcelContext context)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(L("DetailedOvertimeSheet"));

            ConfigureSheet(ws);
            SetDetailedColumns(ws);
            WriteCommonHeader(ws, L("DetailedOvertimeReportTitle"), context, 10);

            const int headerRow = 6;
            WriteTableHeaders(ws, headerRow, new[]
            {
                L("Employee"),
                L("OvertimeType"),
                L("TaskType"),
                L("Reference"),
                L("Date"),
                L("Customer"),
                L("Name"),
                L("Notes"),
                L("Quantity"),
                L("Amount")
            });

            var currentRow = headerRow + 1;
            var groupedRows = rows
                .OrderBy(x => x.EmployeeName)
                .ThenBy(x => x.JobDate)
                .ThenBy(x => x.Reference)
                .GroupBy(x => new { x.EmployeeCode, x.EmployeeName });

            foreach (var employeeGroup in groupedRows)
            {
                foreach (var row in employeeGroup)
                {
                    var customerValue = string.IsNullOrWhiteSpace(row.CustomerName) ? row.CustomerCode : row.CustomerName;

                    ws.Cell(currentRow, 1).Value = row.EmployeeName;
                    ws.Cell(currentRow, 2).Value = row.TimeTypeName;
                    ws.Cell(currentRow, 3).Value = row.WorkTypeName;
                    ws.Cell(currentRow, 4).Value = row.Reference;
                    ws.Cell(currentRow, 5).Value = row.JobDate.Date;
                    ws.Cell(currentRow, 6).Value = customerValue;
                    ws.Cell(currentRow, 7).Value = row.JobName;
                    ws.Cell(currentRow, 8).Value = row.Notes;
                    ws.Cell(currentRow, 9).Value = row.Quantity;
                    ws.Cell(currentRow, 10).Value = row.Amount;

                    ws.Cell(currentRow, 5).Style.DateFormat.Format = "dd/MM/yyyy";
                    SetNumericCellStyle(ws.Cell(currentRow, 9), "#,##0.##");
                    SetNumericCellStyle(ws.Cell(currentRow, 10), "#,##0.00");

                    currentRow++;
                }

                ws.Range(currentRow, 1, currentRow, 8).Merge();
                ws.Cell(currentRow, 1).Value = string.Format(L("EmployeeTotalFormat"), employeeGroup.Key.EmployeeName);
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(currentRow, 9).Value = employeeGroup.Sum(x => x.Quantity);
                ws.Cell(currentRow, 10).Value = employeeGroup.Sum(x => x.Amount);
                SetNumericCellStyle(ws.Cell(currentRow, 9), "#,##0.##");
                SetNumericCellStyle(ws.Cell(currentRow, 10), "#,##0.00");
                StyleSubtotalRow(ws, currentRow, 10);

                currentRow++;
            }

            WriteGrandTotalRow(
                ws,
                currentRow,
                8,
                9,
                10,
                rows.Sum(x => x.Quantity),
                rows.Sum(x => x.Amount),
                L("ReportTotal"));

            FinalizeTable(ws, headerRow, currentRow, 10);
            return Save(workbook);
        }

        public byte[] BuildSummaryReport(List<OvertimeSummaryReportRowDto> rows, OvertimeReportExcelContext context)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(L("SummaryOvertimeSheet"));

            ConfigureSheet(ws);
            SetSummaryColumns(ws);
            WriteCommonHeader(ws, L("SummaryOvertimeReportTitle"), context, 5);

            const int headerRow = 6;
            WriteTableHeaders(ws, headerRow, new[]
            {
                L("Employee"),
                L("OvertimeType"),
                L("TaskType"),
                L("Quantity"),
                L("Amount")
            });

            var currentRow = headerRow + 1;
            var groupedRows = rows
                .OrderBy(x => x.EmployeeName)
                .ThenBy(x => x.TimeTypeName)
                .ThenBy(x => x.WorkTypeName)
                .GroupBy(x => new { x.EmployeeCode, x.EmployeeName });

            foreach (var employeeGroup in groupedRows)
            {
                foreach (var row in employeeGroup)
                {
                    ws.Cell(currentRow, 1).Value = row.EmployeeName;
                    ws.Cell(currentRow, 2).Value = row.TimeTypeName;
                    ws.Cell(currentRow, 3).Value = row.WorkTypeName;
                    ws.Cell(currentRow, 4).Value = row.Quantity;
                    ws.Cell(currentRow, 5).Value = row.Amount;

                    SetNumericCellStyle(ws.Cell(currentRow, 4), "#,##0.##");
                    SetNumericCellStyle(ws.Cell(currentRow, 5), "#,##0.00");

                    currentRow++;
                }

                ws.Range(currentRow, 1, currentRow, 3).Merge();
                ws.Cell(currentRow, 1).Value = string.Format(L("EmployeeTotalFormat"), employeeGroup.Key.EmployeeName);
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(currentRow, 4).Value = employeeGroup.Sum(x => x.Quantity);
                ws.Cell(currentRow, 5).Value = employeeGroup.Sum(x => x.Amount);
                SetNumericCellStyle(ws.Cell(currentRow, 4), "#,##0.##");
                SetNumericCellStyle(ws.Cell(currentRow, 5), "#,##0.00");
                StyleSubtotalRow(ws, currentRow, 5);

                currentRow++;
            }

            WriteGrandTotalRow(
                ws,
                currentRow,
                3,
                4,
                5,
                rows.Sum(x => x.Quantity),
                rows.Sum(x => x.Amount),
                L("ReportTotal"));

            FinalizeTable(ws, headerRow, currentRow, 5);
            return Save(workbook);
        }

        public byte[] BuildAdministrativeSummaryReport(List<OvertimeAdministrativeSummaryReportRowDto> rows, OvertimeReportExcelContext context)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(L("AdminSummaryOvertimeSheet"));

            ConfigureSheet(ws);
            SetAdministrativeSummaryColumns(ws);
            WriteCommonHeader(ws, L("AdminSummaryOvertimeReportTitle"), context, 3);

            const int headerRow = 6;
            WriteTableHeaders(ws, headerRow, new[]
            {
                L("Employee"),
                L("Quantity"),
                L("Amount")
            });

            var currentRow = headerRow + 1;
            foreach (var row in rows.OrderBy(x => x.EmployeeName))
            {
                ws.Cell(currentRow, 1).Value = row.EmployeeName;
                ws.Cell(currentRow, 2).Value = row.Quantity;
                ws.Cell(currentRow, 3).Value = row.Amount;
                SetNumericCellStyle(ws.Cell(currentRow, 2), "#,##0.##");
                SetNumericCellStyle(ws.Cell(currentRow, 3), "#,##0.00");

                currentRow++;
            }

            WriteGrandTotalRow(
                ws,
                currentRow,
                1,
                2,
                3,
                rows.Sum(x => x.Quantity),
                rows.Sum(x => x.Amount),
                L("ReportTotal"));

            FinalizeTable(ws, headerRow, currentRow, 3);
            return Save(workbook);
        }

        private static void ConfigureSheet(IXLWorksheet ws)
        {
            ws.Style.Font.FontName = "Arial";
            ws.Style.Font.FontSize = 10;
        }

        private static void SetDetailedColumns(IXLWorksheet ws)
        {
            ws.Column(1).Width = 22;
            ws.Column(2).Width = 16;
            ws.Column(3).Width = 16;
            ws.Column(4).Width = 10;
            ws.Column(5).Width = 12;
            ws.Column(6).Width = 20;
            ws.Column(7).Width = 42;
            ws.Column(8).Width = 40;
            ws.Column(9).Width = 10;
            ws.Column(10).Width = 12;
        }

        private static void SetSummaryColumns(IXLWorksheet ws)
        {
            ws.Column(1).Width = 28;
            ws.Column(2).Width = 20;
            ws.Column(3).Width = 24;
            ws.Column(4).Width = 12;
            ws.Column(5).Width = 14;
        }

        private static void SetAdministrativeSummaryColumns(IXLWorksheet ws)
        {
            ws.Column(1).Width = 36;
            ws.Column(2).Width = 12;
            ws.Column(3).Width = 14;
        }

        private void WriteCommonHeader(IXLWorksheet ws, string title, OvertimeReportExcelContext context, int columnCount)
        {
            ws.Range(1, 1, 1, columnCount).Merge();
            ws.Cell(1, 1).Value = title;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 18;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(3, 1).Value = L("DateRange");
            ws.Cell(3, 1).Style.Font.Bold = true;
            ws.Cell(3, 2).Value = context.StartDate.Date;
            ws.Cell(4, 2).Value = context.EndDate.Date;
            ws.Cell(3, 2).Style.DateFormat.Format = "dd/MM/yyyy";
            ws.Cell(4, 2).Style.DateFormat.Format = "dd/MM/yyyy";

            if (columnCount >= 10)
            {
                ws.Cell(3, 4).Value = L("EmployeeWithColon");
                ws.Cell(3, 4).Style.Font.Bold = true;
                ws.Range(3, 5, 3, 7).Merge();
                ws.Cell(3, 5).Value = context.EmployeeDisplay;

                ws.Cell(3, 8).Value = L("CustomerWithColon");
                ws.Cell(3, 8).Style.Font.Bold = true;
                ws.Range(3, 9, 3, 10).Merge();
                ws.Cell(3, 9).Value = context.CustomerDisplay;
                return;
            }

            if (columnCount >= 5)
            {
                ws.Cell(3, 3).Value = L("EmployeeWithColon");
                ws.Cell(3, 3).Style.Font.Bold = true;
                ws.Range(3, 4, 3, columnCount).Merge();
                ws.Cell(3, 4).Value = context.EmployeeDisplay;

                ws.Cell(4, 3).Value = L("CustomerWithColon");
                ws.Cell(4, 3).Style.Font.Bold = true;
                ws.Range(4, 4, 4, columnCount).Merge();
                ws.Cell(4, 4).Value = context.CustomerDisplay;
                return;
            }

            ws.Cell(3, 3).Value = $"{L("EmployeeWithColon")} {context.EmployeeDisplay}";
            ws.Cell(3, 3).Style.Font.Bold = true;
            ws.Cell(4, 3).Value = $"{L("CustomerWithColon")} {context.CustomerDisplay}";
            ws.Cell(4, 3).Style.Font.Bold = true;
        }

        private static void WriteTableHeaders(IXLWorksheet ws, int row, string[] headers)
        {
            for (var i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F1F1");
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = i >= headers.Length - 2
                    ? XLAlignmentHorizontalValues.Right
                    : XLAlignmentHorizontalValues.Left;
            }
        }

        private static void SetNumericCellStyle(IXLCell cell, string numberFormat)
        {
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cell.Style.NumberFormat.Format = numberFormat;
        }

        private static void StyleSubtotalRow(IXLWorksheet ws, int row, int lastColumn)
        {
            ws.Range(row, 1, row, lastColumn).Style.Font.Bold = true;
            ws.Range(row, 1, row, lastColumn).Style.Fill.BackgroundColor = XLColor.FromHtml("#FAFAFA");
            ws.Range(row, 1, row, lastColumn).Style.Border.TopBorder = XLBorderStyleValues.Dashed;
        }

        private static void WriteGrandTotalRow(
            IXLWorksheet ws,
            int row,
            int labelLastColumn,
            int quantityColumn,
            int amountColumn,
            decimal totalQuantity,
            decimal totalAmount,
            string totalLabel)
        {
            ws.Range(row, 1, row, labelLastColumn).Merge();
            ws.Cell(row, 1).Value = totalLabel;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            ws.Cell(row, quantityColumn).Value = totalQuantity;
            ws.Cell(row, amountColumn).Value = totalAmount;
            SetNumericCellStyle(ws.Cell(row, quantityColumn), "#,##0.##");
            SetNumericCellStyle(ws.Cell(row, amountColumn), "#,##0.00");

            ws.Range(row, 1, row, amountColumn).Style.Font.Bold = true;
            ws.Range(row, 1, row, amountColumn).Style.Fill.BackgroundColor = XLColor.FromHtml("#EFEFEF");
            ws.Range(row, 1, row, amountColumn).Style.Border.TopBorder = XLBorderStyleValues.Double;
        }

        private static void FinalizeTable(IXLWorksheet ws, int headerRow, int lastRow, int lastColumn)
        {
            ws.Range(headerRow, 1, lastRow, lastColumn).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(headerRow, 1, lastRow, lastColumn).Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            ws.SheetView.FreezeRows(headerRow);
        }

        private static byte[] Save(XLWorkbook workbook)
        {
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private string L(string key)
        {
            return _localizer[key].Value;
        }
    }
}
