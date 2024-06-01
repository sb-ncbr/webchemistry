
namespace WebChemistry.Web.Helpers
{
    using System.Web.Mvc;

    public class ReviewModeAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            filterContext.Controller.ViewBag.IsReviewMode = true;
        }
    }
}
