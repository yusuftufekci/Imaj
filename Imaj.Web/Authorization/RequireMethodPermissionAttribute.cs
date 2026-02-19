using System;
using Microsoft.AspNetCore.Mvc;

namespace Imaj.Web.Authorization
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequireMethodPermissionAttribute : TypeFilterAttribute
    {
        public RequireMethodPermissionAttribute(double baseMethId, bool write = false)
            : base(typeof(MethodPermissionFilter))
        {
            Arguments = new object[] { Convert.ToDecimal(baseMethId), write };
        }
    }
}
