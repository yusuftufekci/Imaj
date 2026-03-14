namespace Imaj.Web.Models
{
    public class PrintableReportViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Orientation { get; set; } = "landscape";
        public string GeneratedAtDisplay { get; set; } = string.Empty;
        public string EmptyMessage { get; set; } = string.Empty;
        public List<PrintableReportMetaItem> MetaItems { get; set; } = new();
        public List<PrintableReportColumn> Columns { get; set; } = new();
        public List<PrintableReportRow> Rows { get; set; } = new();
        public List<PrintableReportBlock> Blocks { get; set; } = new();
    }

    public class PrintableReportMetaItem
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class PrintableReportColumn
    {
        public string Title { get; set; } = string.Empty;
        public string Alignment { get; set; } = "left";
    }

    public class PrintableReportRow
    {
        public PrintableReportRowKind Kind { get; set; } = PrintableReportRowKind.Data;
        public List<PrintableReportCell> Cells { get; set; } = new();
    }

    public class PrintableReportCell
    {
        public string Value { get; set; } = string.Empty;
        public string? Alignment { get; set; }
        public int ColSpan { get; set; } = 1;
        public bool IsCheckbox { get; set; }
        public bool IsChecked { get; set; }
    }

    public class PrintableReportBlock
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public List<PrintableReportMetaItem> Items { get; set; } = new();
    }

    public enum PrintableReportRowKind
    {
        Data = 0,
        GroupTotal = 1,
        GrandTotal = 2
    }
}
