using System.Web.Mvc;

namespace WebChemistry.Web.Filters
{
    public class MustBeAuthorizedAttribute : AuthorizeAttribute
    {
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.HttpContext.Response.StatusCode = 302;
            filterContext.Result = new ViewResult { ViewName = "Unauthorized" };
        }
    }
}