using Imaj.Web.Models;
using Microsoft.Extensions.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Imaj.Web.Services.Reports
{
    public class JobFormPdfService : IJobFormPdfService
    {
        private readonly IStringLocalizer<SharedResource> _localizer;

        static JobFormPdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public JobFormPdfService(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public byte[] Build(JobPrintFormViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(14, Unit.Millimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Content().Column(column =>
                    {
                        column.Spacing(12);

                        column.Item().Text(model.GeneratedAtDisplay)
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Spacing(10);
                                left.Item().Row(titleRow =>
                                {
                                    titleRow.ConstantItem(110).Text(model.Title).Bold().FontSize(17);
                                    titleRow.RelativeItem().AlignMiddle().Text(model.FunctionReferenceDisplay).FontSize(15);
                                });

                                left.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                            });

                            row.ConstantItem(110)
                                .AlignRight()
                                .Text("imaj")
                                .FontSize(32)
                                .Italic()
                                .FontColor(Colors.Grey.Darken3);
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem(1.9f).Element(container => ComposeInfoTable(container, model));
                            row.ConstantItem(220).Element(container => ComposeDateTable(container, model));
                        });

                        column.Item().Element(container => ComposeItemsTable(container, model));

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().AlignCenter().AlignBottom().Text(model.VatNote).Bold().FontSize(12);
                            row.ConstantItem(280).Element(container => ComposeSummaryTable(container, model));
                        });
                    });
                });
            }).GeneratePdf();
        }

        private void ComposeInfoTable(IContainer container, JobPrintFormViewModel model)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(110);
                    columns.RelativeColumn();
                });

                AddInfoRow(table, _localizer["Customer"], model.CustomerName);
                AddInfoRow(table, _localizer["Related"], model.RelatedPerson);
                AddInfoRow(table, _localizer["Name"], model.Name);
                AddInfoRow(table, _localizer["Employee"], model.EmployeeNames);
                AddInfoRow(table, _localizer["Notes"], model.Notes);
            });
        }

        private void ComposeDateTable(IContainer container, JobPrintFormViewModel model)
        {
            container.AlignRight().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(110);
                    columns.RelativeColumn();
                });

                AddInfoRow(table, _localizer["StartDate"], model.StartDateDisplay, alignRightLabel: true);
                AddInfoRow(table, _localizer["EndDate"], model.EndDateDisplay, alignRightLabel: true);
            });
        }

        private void ComposeItemsTable(IContainer container, JobPrintFormViewModel model)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(72);
                    columns.RelativeColumn(2.6f);
                    columns.ConstantColumn(70);
                    columns.ConstantColumn(90);
                    columns.RelativeColumn(1.6f);
                });

                table.Header(header =>
                {
                    AddHeaderCell(header, _localizer["Code"]);
                    AddHeaderCell(header, _localizer["Product"]);
                    AddHeaderCell(header, _localizer["Quantity"], HorizontalAlignment.Right);
                    AddHeaderCell(header, _localizer["Amount"], HorizontalAlignment.Right);
                    AddHeaderCell(header, _localizer["Notes"]);
                });

                if (model.Items.Count == 0)
                {
                    table.Cell().ColumnSpan(5).Element(CellStyle).AlignCenter().PaddingVertical(12).Text("-");
                    return;
                }

                foreach (var item in model.Items)
                {
                    AddCell(table, item.Code);
                    AddCell(table, item.ProductName);
                    AddCell(table, item.QuantityDisplay, HorizontalAlignment.Right);
                    AddCell(table, item.AmountDisplay, HorizontalAlignment.Right);
                    AddCell(table, item.Notes);
                }
            });
        }

        private void ComposeSummaryTable(IContainer container, JobPrintFormViewModel model)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.ConstantColumn(110);
                });

                foreach (var item in model.SummaryItems)
                {
                    table.Cell().Element(SummaryCellStyle).Text(item.Label);
                    table.Cell().Element(SummaryAmountCellStyle).Text(item.AmountDisplay);
                }

                table.Cell().Element(TotalCellStyle).PaddingTop(8).Text(_localizer["Total"]).Bold().FontSize(12);
                table.Cell().Element(TotalAmountCellStyle).PaddingTop(8).Text(model.TotalAmountDisplay).Bold().FontSize(12);
            });
        }

        private static void AddInfoRow(TableDescriptor table, string label, string value, bool alignRightLabel = false)
        {
            table.Cell().PaddingBottom(6).PaddingRight(10).AlignRight().Text(label).Bold();
            table.Cell().PaddingBottom(6).Text(string.IsNullOrWhiteSpace(value) ? "-" : value);
        }

        private static void AddHeaderCell(TableCellDescriptor header, string text, HorizontalAlignment align = HorizontalAlignment.Left)
        {
            ApplyAlignment(header.Cell().Element(HeaderCellStyle), align).Text(text).Bold();
        }

        private static void AddCell(TableDescriptor table, string text, HorizontalAlignment align = HorizontalAlignment.Left)
        {
            ApplyAlignment(table.Cell().Element(CellStyle), align).Text(string.IsNullOrWhiteSpace(text) ? "-" : text);
        }

        private static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderTop(1)
                .BorderColor(Colors.Grey.Lighten1)
                .PaddingVertical(8)
                .PaddingHorizontal(6);
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container
                .PaddingVertical(7)
                .PaddingHorizontal(6);
        }

        private static IContainer SummaryCellStyle(IContainer container)
        {
            return container.PaddingVertical(4).PaddingRight(12);
        }

        private static IContainer SummaryAmountCellStyle(IContainer container)
        {
            return container.PaddingVertical(4).AlignRight();
        }

        private static IContainer TotalCellStyle(IContainer container)
        {
            return container
                .BorderTop(1)
                .BorderColor(Colors.Grey.Lighten1)
                .PaddingTop(10)
                .PaddingRight(12);
        }

        private static IContainer TotalAmountCellStyle(IContainer container)
        {
            return container
                .BorderTop(1)
                .BorderColor(Colors.Grey.Lighten1)
                .PaddingTop(10)
                .AlignRight();
        }

        private static IContainer ApplyAlignment(IContainer container, HorizontalAlignment align)
        {
            return align switch
            {
                HorizontalAlignment.Center => container.AlignCenter(),
                HorizontalAlignment.Right => container.AlignRight(),
                _ => container.AlignLeft()
            };
        }

    }
}
