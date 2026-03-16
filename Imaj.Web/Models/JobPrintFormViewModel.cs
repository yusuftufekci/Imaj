namespace Imaj.Web.Models
{
    public class JobPrintFormViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string GeneratedAtDisplay { get; set; } = string.Empty;
        public string FunctionReferenceDisplay { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string RelatedPerson { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string EmployeeNames { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string StartDateDisplay { get; set; } = string.Empty;
        public string EndDateDisplay { get; set; } = string.Empty;
        public string VatNote { get; set; } = string.Empty;
        public string TotalAmountDisplay { get; set; } = string.Empty;
        public List<JobPrintFormLineItemViewModel> Items { get; set; } = new();
        public List<JobPrintFormSummaryItemViewModel> SummaryItems { get; set; } = new();
    }

    public class JobPrintFormLineItemViewModel
    {
        public string Code { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string QuantityDisplay { get; set; } = string.Empty;
        public string AmountDisplay { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class JobPrintFormSummaryItemViewModel
    {
        public string Label { get; set; } = string.Empty;
        public string AmountDisplay { get; set; } = string.Empty;
    }
}
