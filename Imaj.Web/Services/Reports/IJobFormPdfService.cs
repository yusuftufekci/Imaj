using Imaj.Web.Models;

namespace Imaj.Web.Services.Reports
{
    public interface IJobFormPdfService
    {
        byte[] Build(JobPrintFormViewModel model);
    }
}
