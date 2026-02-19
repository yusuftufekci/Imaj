using System.Threading.Tasks;

namespace Imaj.Web.Authorization
{
    public interface IPageRouteResolver
    {
        Task<PageRouteMatch> ResolveAsync(string controller, string action);
    }
}
