namespace WebChemistry.Web.Filters
{
    using System.Web.Mvc;

    public class CORSAttribute : System.Web.Mvc.ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Origin", "*");
            filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Headers", "X-Requested-With");
            base.OnActionExecuting(filterContext);
        }
    }
}