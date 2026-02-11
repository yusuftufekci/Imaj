using Imaj.Service.DTOs;

namespace Imaj.Web.Services.Reports
{
    public interface IProductReportExcelService
    {
        byte[] BuildDetailedReport(List<ProductReportRowDto> rows, ProductReportExcelContext context);
    }

    public class ProductReportExcelContext
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ProductGroupDisplay { get; set; } = "Tümü";
        public string ProductDisplay { get; set; } = "Tümü";
        public string CustomerDisplay { get; set; } = "Tümü";
    }
}
