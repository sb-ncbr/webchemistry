using System.Web.Mvc;
using WebChemistry.Web.Filters;

namespace WebChemistry.Web.Controllers
{
    [Compress]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.UseContainer = false;
            ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "";

            return View();
        }

        public ActionResult TermsOfUse()
        {
            ViewBag.Message = "";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
