
namespace WebChemistry.Web.Helpers
{
    using System.Web.Mvc;

    public class UnderMaintenanceAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            filterContext.Controller = null;
            filterContext.Result = new HttpStatusCodeResult(503, "Under Maintenance");
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            filterContext.Controller = null;
            filterContext.Result = new HttpStatusCodeResult(503, "Under Maintenance");
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            filterContext.Controller = null;
            filterContext.Result = new HttpStatusCodeResult(503, "Under Maintenance");
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            filterContext.Controller = null;
            filterContext.Result = new HttpStatusCodeResult(503, "Under Maintenance");
        }
    }
}
