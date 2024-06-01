using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebChemistry.Web.Filters;

namespace WebChemistry.Web.Controllers
{
    [Compress]
    public class HelpersController : Controller
    {
        //
        // GET: /Helpers/

        public ActionResult Modals()
        {
            return PartialView("_Modals");
        }
    }
}
