using ClosedXML.Excel;
using Imaj.Service.DTOs;
using Imaj.Web;
using Microsoft.Extensions.Localization;

namespace Imaj.Web.Services.Reports
{
    public class CustomerReportExcelService : ICustomerReportExcelService
    {
        private readonly IStringLocalizer<SharedResource> _localizer;

        public CustomerReportExcelService(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
        }

        public byte[] BuildReport(List<CustomerDto> rows, CustomerReportExcelContext context)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(L("CustomerInfoSheet"));

            ConfigureSheet(ws);
            SetColumns(ws);
            WriteHeader(ws, context);
            WriteCustomers(ws, rows);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static void ConfigureSheet(IXLWorksheet ws)
        {
            ws.Style.Font.FontName = "Arial";
            ws.Style.Font.FontSize = 11;
            ws.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
        }

        private static void SetColumns(IXLWorksheet ws)
        {
            ws.Column(1).Width = 16;
            ws.Column(2).Width = 52;
            ws.Column(3).Width = 3;
            ws.Column(4).Width = 16;
            ws.Column(5).Width = 34;
            ws.Column(6).Width = 6;
        }

        private void WriteHeader(IXLWorksheet ws, CustomerReportExcelContext context)
        {
            ws.Range(1, 1, 1, 6).Style.Border.BottomBorder = XLBorderStyleValues.Thick;

            ws.Range(2, 1, 2, 5).Merge();
            ws.Cell(2, 1).Value = L("CustomerInformationTitle");
            ws.Cell(2, 1).Style.Font.Bold = true;
            ws.Cell(2, 1).Style.Font.FontSize = 28;
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(2, 6).Value = context.GeneratedAt;
            ws.Cell(2, 6).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            ws.Cell(2, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(2, 6).Style.Font.Bold = true;

            ws.Range(3, 1, 3, 6).Style.Border.BottomBorder = XLBorderStyleValues.Thick;
        }

        private void WriteCustomers(IXLWorksheet ws, List<CustomerDto> rows)
        {
            var customers = rows
                .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var row = 4;
            foreach (var customer in customers)
            {
                row = WriteCustomerBlock(ws, row, customer);
            }

            if (customers.Count == 0)
            {
                ws.Range(row, 1, row, 6).Merge();
                ws.Cell(row, 1).Value = L("NoRecordsFound");
                ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 1).Style.Font.Italic = true;
            }
        }

        private int WriteCustomerBlock(IXLWorksheet ws, int startRow, CustomerDto customer)
        {
            ws.Range(startRow, 1, startRow, 6).Style.Border.TopBorder = XLBorderStyleValues.Thin;

            WritePair(ws, startRow, L("Code"), customer.Code, L("Name"), customer.Name);
            WritePair(ws, startRow + 1, L("Owner"), customer.Owner, L("Related"), customer.Contact);
            WritePair(ws, startRow + 2, L("Address"), customer.Address, string.Empty, string.Empty);
            WritePair(ws, startRow + 3, L("City"), customer.City, L("AreaCode"), customer.AreaCode);
            WritePair(ws, startRow + 4, L("Email"), customer.Email, string.Empty, string.Empty);
            WritePair(ws, startRow + 5, L("Fax"), customer.Fax, L("Phone"), customer.Phone);
            WritePair(ws, startRow + 6, L("TaxOffice"), customer.TaxOffice, L("TaxNumber"), customer.TaxNumber);
            WritePair(ws, startRow + 7, L("InvoiceName"), customer.InvoiceName, L("Invalid"), string.Empty);

            var invalidBoxCell = ws.Cell(startRow + 7, 6);
            invalidBoxCell.Value = customer.Invisible ? "X" : string.Empty;
            invalidBoxCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            invalidBoxCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            invalidBoxCell.Style.Font.Bold = true;
            invalidBoxCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            ws.Range(startRow + 7, 1, startRow + 7, 6).Style.Border.BottomBorder = XLBorderStyleValues.Medium;

            for (var i = 0; i < 8; i++)
            {
                ws.Row(startRow + i).AdjustToContents();
            }

            return startRow + 9;
        }

        private static void WritePair(
            IXLWorksheet ws,
            int row,
            string leftLabel,
            string? leftValue,
            string rightLabel,
            string? rightValue)
        {
            var leftLabelCell = ws.Cell(row, 1);
            leftLabelCell.Value = leftLabel;
            leftLabelCell.Style.Font.Bold = true;
            leftLabelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var leftValueCell = ws.Cell(row, 2);
            leftValueCell.Value = leftValue ?? string.Empty;
            leftValueCell.Style.Alignment.WrapText = true;

            var rightLabelCell = ws.Cell(row, 4);
            rightLabelCell.Value = rightLabel;
            rightLabelCell.Style.Font.Bold = !string.IsNullOrWhiteSpace(rightLabel);
            rightLabelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var rightValueCell = ws.Cell(row, 5);
            rightValueCell.Value = rightValue ?? string.Empty;
            rightValueCell.Style.Alignment.WrapText = true;
        }

        private string L(string key)
        {
            return _localizer[key].Value;
        }
    }
}
