using System.Web.Mvc;
using WebChemistry.Web.Filters;

namespace WebChemistry.Web.Controllers
{
    [Compress]
    public class AppController : Controller
    {
        //
        // GET: /Apps/

        public ActionResult List()
        {
            return View();
        }

        public ActionResult Mole(string operation)
        {
            ViewBag.AppIcon = "icon-mole";
            ViewBag.AppName = "Mole";
            return View("Mole");
        }

        public ActionResult CrocoBLAST()
        {
            ViewBag.AppIcon = "icon-fire";
            ViewBag.AppName = "CrocoBLAST";
            ViewBag.Copyright = "Ravi Ramos";
            return View("CrocoBLAST");
        }
    }
}
