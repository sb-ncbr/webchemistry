using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using WebChemistry.MotiveValidator.Database;
using WebChemistry.Web.Filters;
using WebChemistry.Web.Helpers;

namespace WebChemistry.Web.Controllers
{
    [Compress]
    public class MotiveValidatorDbOldController : Controller
    {
        public static MotiveValidatorDatabaseInterfaceProvider DbInterfaceProvider = new MotiveValidatorDatabaseInterfaceProvider();

        static MotiveValidatorDatabaseApp GetApp()
        {
            return ServerHelper.Atlas.GetOrCreateApp("MotiveValidatorDbOld", server =>
            {
                return MotiveValidatorDatabaseApp.Create("MotiveValidatorDbOld", server);
            });
        }


        //
        // GET: /MotiveValidatorDb/

        public ActionResult Index()
        {
            var app = GetApp();
            ViewBag.LastUpdated = app.LastUpdated.ToString("d/M/yyyy", CultureInfo.InvariantCulture);
            ViewBag.AnalyzedCount = 0; // app.AnalyzedModelCount;
            ViewBag.StructureCount = 0; // app.AnalyzedStructureCount;
            ViewBag.SummaryJson = string.Concat(System.IO.File.ReadAllLines(Path.Combine(app.GetCurrentDatabasePath(), "summary.json")).Select(l => l.Trim()));
            return View();
        }
        
        public ActionResult Download(string id)
        {
            try
            {
                switch (id.ToLowerInvariant())
                {
                    case "csvmodelsummary":
                        return File(Path.Combine(GetApp().GetCurrentDatabasePath(), "model_summary.csv"), "text/csv", "model_summary.csv");
                    case "csvstructuresummary":
                        return File(Path.Combine(GetApp().GetCurrentDatabasePath(), "structure_summary.csv"), "text/csv", "structure_summary.csv");
                    case "modelsummary":
                        return ActionHelper.JsonContent(System.IO.File.ReadAllText(Path.Combine(GetApp().GetCurrentDatabasePath(), "model_summary.csv")));
                    case "structuresummary":
                        return ActionHelper.JsonContent(System.IO.File.ReadAllText(Path.Combine(GetApp().GetCurrentDatabasePath(), "structure_summary.csv")));
                    case "csvwwpdbaudit":
                        return File(Path.Combine(GetApp().GetCurrentAuditPath(), "audit.csv"), "text/csv", "audit.csv");
                    default:
                        return HttpNotFound();
                }
            }
            catch
            {
                return HttpNotFound();
            }
        }

        static string[] GetNames(string ids)
        {
            return (ids ?? "")
                .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.ToUpperInvariant())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();
        }

        void InitResult(string ids, bool isStructure)
        {
            var names = GetNames(ids);

            var db = GetApp().GetInterface(DbInterfaceProvider);
            var presentNames = names.Where(n => db.HasEntry(Path.Combine(isStructure ? "by_structure" : "by_model", n, "result.json"))).ToArray();
            var missingNames = names.Where(n => !db.HasEntry(Path.Combine(isStructure ? "by_structure" : "by_model", n, "result.json"))).ToArray();

            ViewBag.PresentNames = string.Join(", ", presentNames);
            ViewBag.MissingNames = string.Join(", ", missingNames);
            ViewBag.AllNames = string.Join(",", names);
            ViewBag.DataId = string.Join(",", presentNames);

            if (isStructure)
            {
                ViewBag.Header = presentNames.Length == 1 ? "PDB Entry" : "PDB Entries";
                ViewBag.DataAction = "DataByStructure";
            }
            else
            {
                ViewBag.Header = presentNames.Length == 1 ? "Residue" : "Residues";
                ViewBag.DataAction = "DataByModel";
            }
        }

        public ActionResult ByStructure(string id)
        {
            InitResult(id, true);
            return View("Result");
        }

        public ActionResult ByModel(string id)
        {
            InitResult(id, false);
            return View("Result");
        }

        public ActionResult GetData(string id, bool isStructure)
        {
            try
            {
                var json = isStructure 
                    ? GetApp().GetValidationForStructuresJson(GetNames(id), DbInterfaceProvider)
                    : GetApp().GetValidationForModelsJson(GetNames(id), DbInterfaceProvider);
                if (json == null)
                {
                    return new { _error = true, _message = "Data not found." }.AsJsonResult();
                }
                return ActionHelper.JsonContent(json);
            }
            catch (InvalidOperationException e)
            {
                return HttpNotFound(e.Message);
            }
            catch
            {
                return HttpNotFound();
            }
        }

        public ActionResult DataByStructure(string id)
        {
            return GetData(id, true);
        }

        public ActionResult DataByModel(string id)
        {
            return GetData(id, false);
        }

        ActionResult StructureForVisualization(string modelId, string motiveId)
        {
            var validated = Path.Combine("structures", "mols", motiveId + ".mol");
            var input = Path.Combine("structures", "motifmols", motiveId + ".mol");
            var model = Path.Combine("structures", "models", modelId + "_model.mol");

            var db = GetApp().GetInterface(DbInterfaceProvider);

            var ret = new
            {
                mol = db.GetEntry(validated),
                motifmol = db.GetEntry(input),
                modelmol = db.GetEntry(model)
            };

            if (string.IsNullOrEmpty(ret.mol) || string.IsNullOrEmpty(ret.motifmol) || string.IsNullOrEmpty(ret.modelmol)) return HttpNotFound();

            return ret.AsJsonResult();
        }

        public ActionResult Structure()
        {
            try
            {
                var model = Request["model"] ?? "";
                var motive = Request["sid"] ?? "";
                var type = Request["type"] ?? "";
                var action = Request["action"] ?? "";

                string prefix = "structures";
                string path, ext;
                if (type.Equals("visualization", StringComparison.OrdinalIgnoreCase))
                {
                    return StructureForVisualization(model, motive);
                }
                else if (type.Equals("mol", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".mol";
                    path = Path.Combine(prefix, "mols", motive + ".mol");
                }
                else if (type.Equals("motif", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".pdb";
                    path = Path.Combine(prefix, "motifs", motive + ".pdb");
                }
                else if (type.Equals("motifmol", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".mol";
                    path = Path.Combine(prefix, "motifmols", motive + ".mol");
                }
                else if (type.Equals("matched", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".pdb";
                    path = Path.Combine(prefix, "matched", motive + ".pdb");
                }
                else if (type.Equals("modelpdb", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".pdb";
                    path = Path.Combine(prefix, "models", model + "_model.pdb");
                }
                else if (type.Equals("modelmol", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".mol";
                    path = Path.Combine(prefix, "models", model + "_model.mol");
                }
                else if (type.Equals("notanalyzedpdb", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".pdb";
                    path = Path.Combine(prefix, "notanalyzed", "pdb", motive + ".pdb");
                }
                else if (type.Equals("notanalyzedmol", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".mol";
                    path = Path.Combine(prefix, "notanalyzed", "mol", motive + ".mol");
                }
                else
                {
                    return HttpNotFound(); //ActionHelper.TextContent("Not found.");
                }

                var source = GetApp().GetInterface(DbInterfaceProvider).GetEntry(path);

                if (string.IsNullOrEmpty(source)) return HttpNotFound();

                if (action.Equals("download", StringComparison.OrdinalIgnoreCase))
                {
                    return File(System.Text.Encoding.UTF8.GetBytes(source), "text/plain; charset=\"utf-8\"", motive + ext);
                }
                else
                {
                    return ActionHelper.TextContent(source);
                }
            }
            catch
            {
                return HttpNotFound();
            }
        }

        public ActionResult AuditData()
        {
            try
            {
                var path = GetApp().GetCurrentAuditPath();
                return ActionHelper.JsonContent(System.IO.File.ReadAllText(Path.Combine(path, "audit.json")));
            }
            catch
            {
                return HttpNotFound();
            }
        }
    }
}
