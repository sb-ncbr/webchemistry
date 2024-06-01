using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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

        public ActionResult MotiveExplorer(string operation)
        {
            ViewBag.IsApp = true;
            ViewBag.CheckExit = true;
            ViewBag.AppIcon = "icon-motiveexplorer";
            ViewBag.AppName = "MotiveExplorer";
            ViewBag.XapPath = Url.Content("~/AppsBin/WebChemistry.Queries.Silverlight.xap");
            return View("SilverlightAppHost");
        }

        public ActionResult SiteBinderPlugin(string operation)
        {
            ViewBag.IsApp = true;
            ViewBag.CheckExit = true;
            ViewBag.AppIcon = "icon-sitebinder";
            ViewBag.AppName = "SiteBinder";
            ViewBag.XapPath = Url.Content("~/AppsBin/WebChemistry.SiteBinder.Silverlight.xap");
            return View("SilverlightAppHost");
        }

        public ActionResult SiteBinder(string operation)
        {
            ViewBag.AppIcon = "icon-sitebinder";
            ViewBag.AppName = "SiteBinder";
            return View("SiteBinder");
        }

        public ActionResult Charges(string operation)
        {
            ViewBag.IsApp = true;
            ViewBag.CheckExit = true;
            ViewBag.AppIcon = "icon-charges";
            ViewBag.AppName = "Charges";
            ViewBag.XapPath = Url.Content("~/AppsBin/WebChemistry.Charges.Silverlight.xap");
            return View("SilverlightAppHost");
        }

        public ActionResult Mole(string operation)
        {
            //ViewBag.IsApp = true;
            ViewBag.AppIcon = "icon-mole";
            ViewBag.AppName = "Mole";
            //ViewBag.Use
            //ViewBag.XapPath = Url.Content("~/AppsBin/WebChemistry.Charges.Silverlight.xap");
            return View("Mole");
        }

        public ActionResult CrocoBLAST()
        {
            //ViewBag.IsApp = true;
            ViewBag.AppIcon = "icon-fire";
            ViewBag.AppName = "CrocoBLAST";
            ViewBag.Copyright = "Ravi Ramos";
            //ViewBag.Use
            //ViewBag.XapPath = Url.Content("~/AppsBin/WebChemistry.Charges.Silverlight.xap");
            return View("CrocoBLAST");
        }

        //public ActionResult List(string operation)
        //{
        //    ViewBag.AppName = "MotiveExplorer";
        //    ViewBag.XapPath = Url.Content("~/AppsBin/WebChemistry.PatternQuery.Silverlight.xap");
        //    return View("SilverlightAppHost");
        //}
    }
}
