using System.Web.Mvc;

namespace WebChemistry.Web.Controllers
{
    public class ErrorsController : Controller
    {
        //
        // GET: /Errors/

        public ActionResult NotFound()
        {
            return View();
        }
    }
}
