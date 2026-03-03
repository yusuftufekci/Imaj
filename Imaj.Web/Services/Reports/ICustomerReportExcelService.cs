using Imaj.Service.DTOs;

namespace Imaj.Web.Services.Reports
{
    public interface ICustomerReportExcelService
    {
        byte[] BuildReport(List<CustomerDto> rows, CustomerReportExcelContext context);
    }

    public class CustomerReportExcelContext
    {
        public DateTime GeneratedAt { get; set; }
    }
}
