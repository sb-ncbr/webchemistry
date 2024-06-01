using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebChemistry.MotiveAtlas.DataModel;
//using WebChemistry.MotiveAtlas.DataModel;
using WebChemistry.Platform;
using WebChemistry.Web.Filters;
using WebChemistry.Web.Helpers;

namespace WebChemistry.Web.Controllers
{
    [Compress]
    public class MotiveAtlasController : Controller
    {
        readonly string atlasAppName = "MotiveAtlas";

        MotiveAtlasObject GetAtlas()
        {
            return ServerHelper.Atlas.GetApp<MotiveAtlasApp>(atlasAppName).GetAtlas();
        }

        //
        // GET: /MotiveAtlas/

        public ActionResult Index()
        {
            ViewBag.IsApp = true;
            return View();
        }
        
        public ActionResult Categories()
        {
            //return ActionHelper.JsonContent(System.IO.File.ReadAllText(GetAtlas().GetDescriptorFilename()));
            return ActionHelper.FileJsonContent(GetAtlas().GetDescriptorFilename());
        }

        public ActionResult Summary(string categoryId, string subcategoryId, string motiveId)
        {
            try
            {
                if (!string.IsNullOrEmpty(motiveId)) return ActionHelper.FileJsonContent(GetAtlas().GetMotiveSummaryFilename(categoryId, subcategoryId, motiveId));
                if (!string.IsNullOrEmpty(subcategoryId)) return ActionHelper.FileJsonContent(GetAtlas().GetSubCategorySummaryFilename(categoryId, subcategoryId));
                if (!string.IsNullOrEmpty(categoryId)) return ActionHelper.FileJsonContent(GetAtlas().GetCategorySummaryFilename(categoryId));
                return ActionHelper.FileJsonContent(GetAtlas().GetAtlasSummaryFilename());
            }
            catch
            {
                return "not found".AsJsonResult();
            }
        }

        public ActionResult DownloadMotives(string categoryId, string subcategoryId, string motiveId)
        {
            try
            {
                return File(GetAtlas().GetMotivesFilename(categoryId, subcategoryId, motiveId), "application/zip, application/octet-stream", MotiveAtlasObject.MotivesFilename);
            }
            catch
            {
                return "not found".AsJsonResult();
            }
        }

        public ActionResult DownloadStructureIndex(string categoryId, string subcategoryId, string motiveId)
        {
            try
            {
                string filename = null;
                if (!string.IsNullOrEmpty(motiveId)) filename = GetAtlas().GetStructureIndexFilename(categoryId, subcategoryId, motiveId);
                else if (!string.IsNullOrEmpty(subcategoryId)) filename = GetAtlas().GetStructureIndexFilename(categoryId, subcategoryId);
                else if (!string.IsNullOrEmpty(categoryId)) filename = GetAtlas().GetStructureIndexFilename(categoryId);
                if (filename == null) return "not found".AsJsonResult();
                filename += ".zip";
                return File(filename, "application/zip, application/octet-stream", MotiveAtlasObject.StructureIndexCompressedFilename);
            }
            catch
            {
                return "not found".AsJsonResult();
            }
        }

        public ActionResult StructureList(string categoryId, string subcategoryId, string motiveId)
        {
            try
            {
                string filename = null;
                if (!string.IsNullOrEmpty(motiveId)) filename = GetAtlas().GetStructureListFilename(categoryId, subcategoryId, motiveId);
                else if (!string.IsNullOrEmpty(subcategoryId)) filename = GetAtlas().GetStructureListFilename(categoryId, subcategoryId);
                else if (!string.IsNullOrEmpty(categoryId)) filename = GetAtlas().GetStructureListFilename(categoryId);
                if (filename == null) return "not found".AsJsonResult();
                return ActionHelper.TextContent(System.IO.File.ReadAllText(filename));                
            }
            catch
            {
                return "not found".AsJsonResult();
            }
        }
    }
}
