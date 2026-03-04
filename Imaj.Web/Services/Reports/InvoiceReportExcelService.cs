using ClosedXML.Excel;
using Imaj.Service.DTOs;
using Imaj.Web;
using Microsoft.Extensions.Localization;

namespace Imaj.Web.Services.Reports
{
    public class InvoiceReportExcelService : IInvoiceReportExcelService
    {
        private readonly IStringLocalizer<SharedResource> _localizer;

        public InvoiceReportExcelService(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
        }

        public byte[] BuildDetailedReport(List<InvoiceDetailedReportRowDto> rows)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(L("DetailedInvoiceSheet"));

            ConfigureSheet(ws);
            SetDetailedColumns(ws);

            ws.Range(1, 1, 1, 9).Merge();
            ws.Cell(1, 1).Value = L("DetailedInvoiceReportTitle");
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 18;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            const int headerRow = 3;
            WriteTableHeaders(ws, headerRow, new[]
            {
                L("Customer"),
                L("Reference"),
                L("Name"),
                L("Date"),
                L("Status"),
                L("Evaluated"),
                L("TaxAmount"),
                L("SubTotal"),
                L("NetAmount")
            });

            var currentRow = headerRow + 1;
            var orderedRows = rows
                .OrderBy(x => x.CustomerName)
                .ThenBy(x => x.IssueDate)
                .ThenBy(x => x.Reference)
                .ToList();

            if (!orderedRows.Any())
            {
                ws.Range(currentRow, 1, currentRow, 9).Merge();
                ws.Cell(currentRow, 1).Value = L("NoRecordsFound");
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            else
            {
                foreach (var customerGroup in orderedRows.GroupBy(x => new { x.CustomerCode, x.CustomerName }))
                {
                    foreach (var row in customerGroup)
                    {
                        var customerDisplay = string.IsNullOrWhiteSpace(row.CustomerName) ? row.CustomerCode : row.CustomerName;

                        ws.Cell(currentRow, 1).Value = customerDisplay;
                        ws.Cell(currentRow, 2).Value = row.Reference;
                        ws.Cell(currentRow, 3).Value = row.Name;
                        ws.Cell(currentRow, 4).Value = row.IssueDate;
                        ws.Cell(currentRow, 5).Value = row.StatusName;
                        ws.Cell(currentRow, 6).Value = row.Evaluated ? "X" : string.Empty;
                        ws.Cell(currentRow, 7).Value = row.TaxAmount;
                        ws.Cell(currentRow, 8).Value = row.SubTotal;
                        ws.Cell(currentRow, 9).Value = row.NetTotal;

                        ws.Cell(currentRow, 4).Style.DateFormat.Format = "dd/MM/yyyy";
                        ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        SetAmountStyle(ws.Cell(currentRow, 7));
                        SetAmountStyle(ws.Cell(currentRow, 8));
                        SetAmountStyle(ws.Cell(currentRow, 9));
                        ws.Range(currentRow, 1, currentRow, 9).Style.Border.BottomBorder = XLBorderStyleValues.Dashed;

                        currentRow++;
                    }

                    var customerTitle = string.IsNullOrWhiteSpace(customerGroup.Key.CustomerName)
                        ? customerGroup.Key.CustomerCode
                        : customerGroup.Key.CustomerName;

                    ws.Range(currentRow, 1, currentRow, 6).Merge();
                    ws.Cell(currentRow, 1).Value = string.Format(L("CustomerTotalFormat"), customerTitle);
                    ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(currentRow, 7).Value = customerGroup.Sum(x => x.TaxAmount);
                    ws.Cell(currentRow, 8).Value = customerGroup.Sum(x => x.SubTotal);
                    ws.Cell(currentRow, 9).Value = customerGroup.Sum(x => x.NetTotal);
                    SetAmountStyle(ws.Cell(currentRow, 7));
                    SetAmountStyle(ws.Cell(currentRow, 8));
                    SetAmountStyle(ws.Cell(currentRow, 9));
                    ws.Range(currentRow, 1, currentRow, 9).Style.Font.Bold = true;
                    ws.Range(currentRow, 1, currentRow, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#FAFAFA");
                    ws.Range(currentRow, 1, currentRow, 9).Style.Border.TopBorder = XLBorderStyleValues.Dashed;

                    currentRow++;
                }

                ws.Range(currentRow, 1, currentRow, 6).Merge();
                ws.Cell(currentRow, 1).Value = L("ReportTotal");
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(currentRow, 7).Value = orderedRows.Sum(x => x.TaxAmount);
                ws.Cell(currentRow, 8).Value = orderedRows.Sum(x => x.SubTotal);
                ws.Cell(currentRow, 9).Value = orderedRows.Sum(x => x.NetTotal);
                SetAmountStyle(ws.Cell(currentRow, 7));
                SetAmountStyle(ws.Cell(currentRow, 8));
                SetAmountStyle(ws.Cell(currentRow, 9));
                ws.Range(currentRow, 1, currentRow, 9).Style.Font.Bold = true;
                ws.Range(currentRow, 1, currentRow, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#EFEFEF");
                ws.Range(currentRow, 1, currentRow, 9).Style.Border.TopBorder = XLBorderStyleValues.Double;
            }

            FinalizeTable(ws, headerRow, currentRow, 9);
            return Save(workbook);
        }

        public byte[] BuildSummaryReport(List<InvoiceSummaryReportRowDto> rows)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(L("SummaryInvoiceSheet"));

            ConfigureSheet(ws);
            SetSummaryColumns(ws);

            ws.Range(1, 1, 1, 5).Merge();
            ws.Cell(1, 1).Value = L("SummaryInvoiceReportTitle");
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 18;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            const int headerRow = 3;
            WriteTableHeaders(ws, headerRow, new[]
            {
                L("Customer"),
                L("Count"),
                L("TaxAmount"),
                L("SubTotal"),
                L("NetAmount")
            });

            var currentRow = headerRow + 1;
            var orderedRows = rows
                .OrderBy(x => x.CustomerName)
                .ToList();

            if (!orderedRows.Any())
            {
                ws.Range(currentRow, 1, currentRow, 5).Merge();
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
                    ws.Cell(currentRow, 3).Value = row.TaxAmount;
                    ws.Cell(currentRow, 4).Value = row.SubTotal;
                    ws.Cell(currentRow, 5).Value = row.NetTotal;

                    ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    SetAmountStyle(ws.Cell(currentRow, 3));
                    SetAmountStyle(ws.Cell(currentRow, 4));
                    SetAmountStyle(ws.Cell(currentRow, 5));
                    ws.Range(currentRow, 1, currentRow, 5).Style.Border.BottomBorder = XLBorderStyleValues.Dashed;

                    currentRow++;
                }

                ws.Cell(currentRow, 1).Value = L("ReportTotal");
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(currentRow, 2).Value = orderedRows.Sum(x => x.Count);
                ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(currentRow, 3).Value = orderedRows.Sum(x => x.TaxAmount);
                ws.Cell(currentRow, 4).Value = orderedRows.Sum(x => x.SubTotal);
                ws.Cell(currentRow, 5).Value = orderedRows.Sum(x => x.NetTotal);
                SetAmountStyle(ws.Cell(currentRow, 3));
                SetAmountStyle(ws.Cell(currentRow, 4));
                SetAmountStyle(ws.Cell(currentRow, 5));
                ws.Range(currentRow, 1, currentRow, 5).Style.Font.Bold = true;
                ws.Range(currentRow, 1, currentRow, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#EFEFEF");
                ws.Range(currentRow, 1, currentRow, 5).Style.Border.TopBorder = XLBorderStyleValues.Double;
            }

            FinalizeTable(ws, headerRow, currentRow, 5);
            return Save(workbook);
        }

        private static void ConfigureSheet(IXLWorksheet ws)
        {
            ws.Style.Font.FontName = "Arial";
            ws.Style.Font.FontSize = 10;
        }

        private static void SetDetailedColumns(IXLWorksheet ws)
        {
            ws.Column(1).Width = 30;
            ws.Column(2).Width = 12;
            ws.Column(3).Width = 44;
            ws.Column(4).Width = 14;
            ws.Column(5).Width = 16;
            ws.Column(6).Width = 12;
            ws.Column(7).Width = 16;
            ws.Column(8).Width = 16;
            ws.Column(9).Width = 16;
        }

        private static void SetSummaryColumns(IXLWorksheet ws)
        {
            ws.Column(1).Width = 42;
            ws.Column(2).Width = 12;
            ws.Column(3).Width = 16;
            ws.Column(4).Width = 16;
            ws.Column(5).Width = 16;
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
                cell.Style.Alignment.Horizontal = i >= headers.Length - 3
                    ? XLAlignmentHorizontalValues.Right
                    : XLAlignmentHorizontalValues.Left;
            }

            if (headers.Length >= 6)
            {
                ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }

        private static void SetAmountStyle(IXLCell cell)
        {
            cell.Style.NumberFormat.Format = "#,##0.00";
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
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
