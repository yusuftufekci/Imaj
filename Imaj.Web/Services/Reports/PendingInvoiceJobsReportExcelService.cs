using ClosedXML.Excel;
using Imaj.Service.DTOs;
using Imaj.Web;
using Microsoft.Extensions.Localization;

namespace Imaj.Web.Services.Reports
{
    public class PendingInvoiceJobsReportExcelService : IPendingInvoiceJobsReportExcelService
    {
        private readonly IStringLocalizer<SharedResource> _localizer;

        public PendingInvoiceJobsReportExcelService(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
        }

        public byte[] BuildDetailedReport(List<PendingInvoiceJobsDetailedReportRowDto> rows, PendingInvoiceJobsReportExcelContext context)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(L("DetailedPendingInvoiceJobsSheet"));

            ConfigureSheet(ws);
            SetDetailedColumns(ws);
            WriteHeader(ws, L("DetailedPendingInvoiceJobsReportTitle"), context, 6);

            const int headerRow = 5;
            WriteTableHeaders(ws, headerRow, new[]
            {
                L("Customer"),
                L("Reference"),
                L("Name"),
                L("StartDate"),
                L("EndDate"),
                L("Amount")
            });

            var currentRow = headerRow + 1;
            var orderedRows = rows
                .OrderBy(x => x.CustomerName)
                .ThenBy(x => x.Reference)
                .ToList();

            if (!orderedRows.Any())
            {
                ws.Range(currentRow, 1, currentRow, 6).Merge();
                ws.Cell(currentRow, 1).Value = L("NoRecordsFound");
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            else
            {
                foreach (var row in orderedRows)
                {
                    var customerDisplay = string.IsNullOrWhiteSpace(row.CustomerName) ? row.CustomerCode : row.CustomerName;

                    ws.Cell(currentRow, 1).Value = customerDisplay;
                    ws.Cell(currentRow, 2).Value = row.Reference;
                    ws.Cell(currentRow, 3).Value = row.JobName;
                    ws.Cell(currentRow, 4).Value = row.StartDate.Date;
                    ws.Cell(currentRow, 5).Value = row.EndDate?.Date;
                    ws.Cell(currentRow, 6).Value = row.Amount;

                    ws.Cell(currentRow, 4).Style.DateFormat.Format = "dd/MM/yyyy";
                    ws.Cell(currentRow, 5).Style.DateFormat.Format = "dd/MM/yyyy";
                    ws.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Range(currentRow, 1, currentRow, 6).Style.Border.BottomBorder = XLBorderStyleValues.Dashed;

                    currentRow++;
                }

                currentRow--;
            }

            FinalizeTable(ws, headerRow, currentRow, 6);
            return Save(workbook);
        }

        public byte[] BuildSummaryReport(List<PendingInvoiceJobsSummaryReportRowDto> rows, PendingInvoiceJobsReportExcelContext context)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(L("SummaryPendingInvoiceJobsSheet"));

            ConfigureSheet(ws);
            SetSummaryColumns(ws);
            WriteHeader(ws, L("SummaryPendingInvoiceJobsReportTitle"), context, 3);

            const int headerRow = 5;
            WriteTableHeaders(ws, headerRow, new[]
            {
                L("Customer"),
                L("Count"),
                L("Amount")
            });

            var currentRow = headerRow + 1;
            var orderedRows = rows
                .OrderBy(x => x.CustomerName)
                .ToList();

            if (!orderedRows.Any())
            {
                ws.Range(currentRow, 1, currentRow, 3).Merge();
                ws.Cell(currentRow, 1).Value = L("NoRecordsFound");
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            else
            {
                foreach (var row in orderedRows)
                {
                    var customerDisplay = string.IsNullOrWhiteSpace(row.CustomerName) ? row.CustomerCode : row.CustomerName;

                    ws.Cell(currentRow, 1).Value = customerDisplay;
                    ws.Cell(currentRow, 2).Value = row.Count;
                    ws.Cell(currentRow, 3).Value = row.Amount;

                    ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(currentRow, 3).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Range(currentRow, 1, currentRow, 3).Style.Border.BottomBorder = XLBorderStyleValues.Dashed;

                    currentRow++;
                }

                currentRow--;
            }

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
            ws.Column(1).Width = 34;
            ws.Column(2).Width = 12;
            ws.Column(3).Width = 52;
            ws.Column(4).Width = 14;
            ws.Column(5).Width = 14;
            ws.Column(6).Width = 16;
        }

        private static void SetSummaryColumns(IXLWorksheet ws)
        {
            ws.Column(1).Width = 40;
            ws.Column(2).Width = 12;
            ws.Column(3).Width = 16;
        }

        private void WriteHeader(IXLWorksheet ws, string title, PendingInvoiceJobsReportExcelContext context, int columnCount)
        {
            ws.Range(1, 1, 1, columnCount).Merge();
            ws.Cell(1, 1).Value = title;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 18;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(3, 1).Value = L("CustomerWithColon");
            ws.Cell(3, 1).Style.Font.Bold = true;
            ws.Range(3, 2, 3, columnCount).Merge();
            ws.Cell(3, 2).Value = context.CustomerDisplay;
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
                cell.Style.Alignment.Horizontal = i == headers.Length - 1 || i == headers.Length - 2
                    ? XLAlignmentHorizontalValues.Right
                    : XLAlignmentHorizontalValues.Left;
            }
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
