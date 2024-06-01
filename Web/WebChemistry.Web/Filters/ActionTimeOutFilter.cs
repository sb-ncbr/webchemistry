using System.Web.Mvc;

namespace WebChemistry.Web.Filters
{
    public class ActionTimeOutFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            System.Web.HttpContext.Current.GetType().GetField("_timeoutState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(System.Web.HttpContext.Current, 1);
            base.OnActionExecuting(filterContext);
        }
    }
}