using ClosedXML.Excel;
using Imaj.Service.DTOs;

namespace Imaj.Web.Services.Reports
{
    public class ProductReportExcelService : IProductReportExcelService
    {
        public byte[] BuildDetailedReport(List<ProductReportRowDto> rows, ProductReportExcelContext context)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Detayli Urun");

            ConfigureSheet(ws);
            SetColumns(ws);
            WriteHeader(ws, context);
            WriteTable(ws, rows);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static void ConfigureSheet(IXLWorksheet ws)
        {
            ws.Style.Font.FontName = "Arial";
            ws.Style.Font.FontSize = 10;
        }

        private static void SetColumns(IXLWorksheet ws)
        {
            ws.Column(1).Width = 18; // Urun Grubu
            ws.Column(2).Width = 26; // Urun
            ws.Column(3).Width = 10; // Referans
            ws.Column(4).Width = 12; // Tarih
            ws.Column(5).Width = 14; // Musteri
            ws.Column(6).Width = 34; // Ad
            ws.Column(7).Width = 34; // Notlar
            ws.Column(8).Width = 10; // Miktar
            ws.Column(9).Width = 12; // Tutar
        }

        private static void WriteHeader(IXLWorksheet ws, ProductReportExcelContext context)
        {
            ws.Range(1, 1, 1, 9).Merge();
            ws.Cell(1, 1).Value = "Detaylı Ürün Raporu";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 18;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(3, 1).Value = "Tarih Aralığı :";
            ws.Cell(3, 1).Style.Font.Bold = true;
            ws.Cell(3, 2).Value = context.StartDate.Date;
            ws.Cell(3, 2).Style.DateFormat.Format = "dd/MM/yyyy";
            ws.Cell(4, 2).Value = context.EndDate.Date;
            ws.Cell(4, 2).Style.DateFormat.Format = "dd/MM/yyyy";

            ws.Cell(3, 3).Value = "Ürün Grubu :";
            ws.Cell(3, 3).Style.Font.Bold = true;
            ws.Range(3, 4, 3, 5).Merge();
            ws.Cell(3, 4).Value = context.ProductGroupDisplay;

            ws.Cell(3, 6).Value = "Ürün :";
            ws.Cell(3, 6).Style.Font.Bold = true;
            ws.Range(3, 7, 3, 8).Merge();
            ws.Cell(3, 7).Value = context.ProductDisplay;

            ws.Cell(3, 9).Value = "Müşteri :";
            ws.Cell(3, 9).Style.Font.Bold = true;
            ws.Range(4, 9, 4, 9).Merge();
            ws.Cell(4, 9).Value = context.CustomerDisplay;
        }

        private static void WriteTable(IXLWorksheet ws, List<ProductReportRowDto> rows)
        {
            const int headerRow = 6;
            var headers = new[]
            {
                "Ürün Grubu",
                "Ürün",
                "Referans",
                "Tarih",
                "Müşteri",
                "Ad",
                "Notlar",
                "Miktar",
                "Tutar"
            };

            for (var i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F1F1");
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                if (i >= headers.Length - 2)
                {
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }
            }

            var currentRow = headerRow + 1;
            var groupedRows = rows
                .OrderBy(x => x.ProductGroupName)
                .ThenBy(x => x.ProductName)
                .ThenBy(x => x.JobDate)
                .ThenBy(x => x.Reference)
                .GroupBy(x => new { x.ProductGroupName, x.ProductCode, x.ProductName });

            foreach (var productGroup in groupedRows)
            {
                foreach (var row in productGroup)
                {
                    var customerValue = string.IsNullOrWhiteSpace(row.CustomerCode) ? row.CustomerName : row.CustomerCode;

                    ws.Cell(currentRow, 1).Value = row.ProductGroupName;
                    ws.Cell(currentRow, 2).Value = row.ProductName;
                    ws.Cell(currentRow, 3).Value = row.Reference;
                    ws.Cell(currentRow, 4).Value = row.JobDate.Date;
                    ws.Cell(currentRow, 5).Value = customerValue;
                    ws.Cell(currentRow, 6).Value = row.JobName;
                    ws.Cell(currentRow, 7).Value = row.Notes;
                    ws.Cell(currentRow, 8).Value = row.Quantity;
                    ws.Cell(currentRow, 9).Value = row.Amount;

                    ws.Cell(currentRow, 4).Style.DateFormat.Format = "dd/MM/yyyy";
                    ws.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    currentRow++;
                }

                ws.Range(currentRow, 1, currentRow, 7).Merge();
                ws.Cell(currentRow, 1).Value = $"{productGroup.Key.ProductName} Toplamı";
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(currentRow, 8).Value = productGroup.Sum(x => x.Quantity);
                ws.Cell(currentRow, 9).Value = productGroup.Sum(x => x.Amount);
                ws.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0.##";
                ws.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Range(currentRow, 1, currentRow, 9).Style.Font.Bold = true;
                ws.Range(currentRow, 1, currentRow, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#FAFAFA");
                ws.Range(currentRow, 1, currentRow, 9).Style.Border.TopBorder = XLBorderStyleValues.Dashed;

                currentRow++;
            }

            ws.Range(currentRow, 1, currentRow, 7).Merge();
            ws.Cell(currentRow, 1).Value = "Rapor Toplamı";
            ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(currentRow, 8).Value = rows.Sum(x => x.Quantity);
            ws.Cell(currentRow, 9).Value = rows.Sum(x => x.Amount);
            ws.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0.##";
            ws.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Range(currentRow, 1, currentRow, 9).Style.Font.Bold = true;
            ws.Range(currentRow, 1, currentRow, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#EFEFEF");
            ws.Range(currentRow, 1, currentRow, 9).Style.Border.TopBorder = XLBorderStyleValues.Double;

            var tableEndRow = currentRow;
            ws.Range(headerRow, 1, tableEndRow, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(headerRow, 1, tableEndRow, 9).Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            ws.SheetView.FreezeRows(headerRow);
        }
    }
}
