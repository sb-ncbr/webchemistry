using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using WebChemistry.MotiveValidator.Database;
using WebChemistry.MotiveValidator.DataModel;
using WebChemistry.Platform.Computation;
using WebChemistry.Platform.Users;
using WebChemistry.Web.Filters;
using WebChemistry.Web.Helpers;
using WebChemistry.Framework.Core;

namespace WebChemistry.Web.Controllers
{
    [Compress]
    public class ValidatorDbController : Controller
    {
        public static MotiveValidatorDatabaseInterfaceProvider DbInterfaceProvider = new MotiveValidatorDatabaseInterfaceProvider(false);

        public static MotiveValidatorDatabaseApp GetApp()
        {
            return ServerHelper.Atlas.GetOrCreateApp("ValidatorDb", server =>
            {
                return MotiveValidatorDatabaseApp.Create("ValidatorDb", server);
            });
        }

        static UserInfo GetAppUser()
        {
            return UserInfo.Load(GetApp().UserId);
        }

        static ComputationInfo GetCustomComputation(string id)
        {
            return GetAppUser().Computations.TryGetByShortId(id);
        }

        public ActionResult Index()
        {
            var app = GetApp();
            ViewBag.LastUpdated = app.LastUpdated.ToString("d/M/yyyy", CultureInfo.InvariantCulture);

            var stats = JsonConvert.DeserializeAnonymousType(System.IO.File.ReadAllText(Path.Combine(app.GetCurrentDatabasePath(), "stats.json")), new { StructureCount = 1, ModelCount = 1 });
            ViewBag.AnalyzedCount = stats.ModelCount;
            ViewBag.StructureCount = stats.StructureCount;

            ViewBag.SummaryJson = string.Concat(System.IO.File.ReadAllLines(Path.Combine(app.GetCurrentDatabasePath(), "summary.json")).Select(l => l.Trim()));
            return View();
        }

        public ActionResult Custom(string id)
        {
            var comp = GetCustomComputation(id);
            if (comp == null) return HttpNotFound();

            ViewBag.ComputedDate = comp.DateCreated.ToString("d/M/yyyy", CultureInfo.InvariantCulture);
            ViewBag.Id = id;
            ViewBag.IsFinished = comp.GetStatus().State == ComputationState.Success;
            ViewBag.IsCustomSearch = true;
            return View("Custom");
        }

        #region customs
        public ActionResult CustomResult(string id)
        {
            var comp = GetCustomComputation(id);
            if (comp == null) return HttpNotFound();
            var path = comp.GetResultFolderId().GetEntityPath();
            
            string stats = "", summary = "", missing = "";

            if (System.IO.File.Exists(Path.Combine(path, "stats.json"))) stats = string.Concat(System.IO.File.ReadAllLines(Path.Combine(path, "stats.json")));
            if (System.IO.File.Exists(Path.Combine(path, "summary.json"))) summary = string.Concat(System.IO.File.ReadAllLines(Path.Combine(path, "summary.json")));
            if (System.IO.File.Exists(Path.Combine(path, "missing_data.json"))) missing = string.Concat(System.IO.File.ReadAllLines(Path.Combine(path, "missing_data.json")));

            return new
            {
                statsJson = stats,
                summaryJson = summary,
                missingJson = missing
            }.AsJsonResult();
        }

        public ActionResult CustomStatus(string id)
        {
            var comp = GetCustomComputation(id);
            if (comp == null) return new { Exists = false, IsRunning = false }.AsJsonResult();
            return ComputationHelper.GetStatus(comp).AsJsonResult();
        }

        public ActionResult CustomDownload(string id, string what)
        {
            try
            {
                what = what.Trim();
                var comp = GetCustomComputation(id);
                if (comp == null) return HttpNotFound();
                var path = comp.GetResultFolderId().GetEntityPath();

                switch (what.ToLowerInvariant())
                {
                    case "csvmodelsummary":
                        return File(Path.Combine(path, "model_summary.csv"), "text/csv", "model_summary.csv");
                    case "csvstructuresummary":
                        return File(Path.Combine(path, "structure_summary.csv"), "text/csv", "structure_summary.csv");
                    case "modelsummary":
                        return ActionHelper.CsvContent(System.IO.File.ReadAllText(Path.Combine(path, "model_summary.csv")));
                    case "structuresummary":
                        return ActionHelper.CsvContent(System.IO.File.ReadAllText(Path.Combine(path, "structure_summary.csv")));
                    default:
                        return HttpNotFound();
                }
            }
            catch
            {
                return HttpNotFound();
            }
        }

        public ActionResult CustomByStructure(string id, string what)
        {
            what = what.Trim();
            CustomInitResult(id, what, true);
            return View("Result");
        }

        public ActionResult CustomByModel(string id, string what)
        {
            what = what.Trim();
            CustomInitResult(id, what, false);
            return View("Result");
        }

        void CustomInitResult(string id, string what, bool isStructure)
        {
            what = what.Trim();

            ViewBag.IsCustom = true;
            ViewBag.PresentNames = what;
            ViewBag.MissingNames = "";
            ViewBag.AllNames = what;
            ViewBag.DataId = what;

            if (isStructure)
            {
                ViewBag.Header = "PDB Entry";
                ViewBag.DataAction = Url.Action("CustomData", new { id = id, what = what, source = "ByStructure" });
            }
            else
            {
                ViewBag.Header = "Molecule";
                ViewBag.DataAction = Url.Action("CustomData", new { id = id, what = what, source = "ByModel" });
            }
        }

        ActionResult CustomGetDataInternal(string id, string what, bool isStructure)
        {
            try 
            {
                var comp = GetCustomComputation(id);
                using (var zip = new ZipFile(Path.Combine(comp.GetResultFolderId().GetEntityPath(), "data.zip")))
                {
                    var index = zip.FindEntry(Path.Combine(isStructure ? "by_structure" : "by_model", what.Trim(), "result.json").Replace('\\', '/'), true);
                    if (index >= 0)
                    {
                        using (var reader = new StreamReader(zip.GetInputStream(index)))
                        {
                            var source = reader.ReadToEnd();
                            return ActionHelper.JsonContent(source);
                        }
                    }
                }
            }
            catch { }
            
            return new ValidationResult
            {
                Version = "n/a",
                ValidationType = MotiveValidationType.Database
            }.AsJsonResult();
        }

        public ActionResult CustomData(string id, string what, string source)
        {
            if (source.Equals("ByStructure", StringComparison.OrdinalIgnoreCase)) return CustomGetDataInternal(id, what, true);
            if (source.Equals("ByModel", StringComparison.OrdinalIgnoreCase)) return CustomGetDataInternal(id, what, false);
            return HttpNotFound();
        }

        public ActionResult __CustomComputations(string type)
        {
            var user = GetAppUser();

            if (string.IsNullOrWhiteSpace(type) || !type.EqualOrdinalIgnoreCase("csv"))
            {
                var xs = user.Computations.GetAll()
                    .GroupBy(c => new DateTime(c.DateCreated.Year, c.DateCreated.Month, c.DateCreated.Day))
                    .OrderByDescending(g => g.Key)
                    .Select(g => Tuple.Create(g.Key, g.OrderByDescending(c => c.DateCreated).ToArray()))
                    .ToArray();
                ViewBag.ComputationsByDay = xs;
                ViewBag.TotalCount = xs.Sum(x => x.Item2.Length);
                return View("ComputationsList");
            }
            else
            {
                var csv = user.Computations.GetAll()
                    .OrderByDescending(c => c.DateCreated).GetExporter()
                    .AddStringColumn(c => c.DateCreated, "Date")
                    .AddStringColumn(c => c.Source, "Source")
                    .AddStringColumn(c => c.ShortId, "Id")
                    .ToCsvString();
                return ActionHelper.TextContent(csv);
            }
        }
        #endregion

        ////public ActionResult _Audit()
        ////{
        ////    var app = GetApp();
        ////    ViewBag.LastUpdated = app.LastUpdated.ToString("d/M/yyyy", CultureInfo.InvariantCulture);
        ////    ViewBag.AnalyzedCount = 0; // app.AnalyzedModelCount;
        ////    ViewBag.StructureCount = 0; // app.AnalyzedStructureCount;
        ////    ViewBag.SummaryJson = string.Concat(System.IO.File.ReadAllLines(Path.Combine(app.GetCurrentDatabasePath(), "summary.json")).Select(l => l.Trim()));
        ////    return View("IndexWithAudit");
        ////}

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
                        return ActionHelper.CsvContent(System.IO.File.ReadAllText(Path.Combine(GetApp().GetCurrentDatabasePath(), "model_summary.csv")));
                    case "structuresummary":
                        return ActionHelper.CsvContent(System.IO.File.ReadAllText(Path.Combine(GetApp().GetCurrentDatabasePath(), "structure_summary.csv")));
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
                ViewBag.DataAction = Url.Action("Data", new { id = ViewBag.DataId, source = "ByStructure" });
            }
            else
            {
                ViewBag.Header = presentNames.Length == 1 ? "Molecule" : "Molecules";
                ViewBag.DataAction = Url.Action("Data", new { id = ViewBag.DataId, source = "ByModel" });
            }
        }

        static bool IsSmallSearch(int structureCount, int modelCount)
        {
            int maxSize = 10;

            return (structureCount > 0 && structureCount <= maxSize) || (modelCount > 0 && modelCount <= maxSize && structureCount <= maxSize);
        }

        static string JoinNames(string[] xs, string[] ys)
        {
            if (xs.Length > 0 && ys.Length > 0)
            {
                return xs.JoinBy() + " | " + ys.JoinBy();
            }
            if (xs.Length > 0) return xs.JoinBy();
            return ys.JoinBy();
        }

        public ActionResult Search(string structures, string models) 
        {
            try 
            {
                if (structures == null && models == null)
                {
                    return RedirectToAction("Index");
                }

                structures = structures ?? "";
                models = models ?? "";


                var criteria = MotiveValidatorDatabaseApp.GetSearchCriteria(structures, models);
                var structureIds = criteria.StructuresAndResidues.Keys.OrderBy(n => n, StringComparer.Ordinal);

                if (!IsSmallSearch(criteria.StructuresAndResidues.Count, criteria.Models.Length))
                {
                    throw new InvalidOperationException("The search criteria are too large.");
                }
            
                var db = GetApp().GetInterface(DbInterfaceProvider);

                var presentStructureNames = structureIds.Where(n => db.HasEntry(Path.Combine("by_structure", n, "result.json"))).ToArray();
                var missingStructureNames = structureIds.Where(n => !db.HasEntry(Path.Combine("by_structure", n, "result.json"))).ToArray();
                var presentModelNames = criteria.Models.Where(n => db.HasEntry(Path.Combine("by_model", n, "result.json"))).ToArray();
                var missingModelNames = criteria.Models.Where(n => !db.HasEntry(Path.Combine("by_model", n, "result.json"))).ToArray();

                var prettyStructures = criteria.StructuresAndResidues
                    .OrderBy(n => n.Key, StringComparer.Ordinal)
                    .Select(p => p.Value.Length == 0 ? p.Key : string.Format("{0} ({1})", p.Key, p.Value.JoinBy()))
                    .ToArray();

                ViewBag.PresentNames = JoinNames(presentStructureNames, presentModelNames);
                ViewBag.MissingNames = JoinNames(missingStructureNames, missingModelNames);
                ViewBag.AllNames = JoinNames(prettyStructures, criteria.Models) + " [Search]";

                ViewBag.IsSearch = true;
                ViewBag.SearchCriteria = JoinNames(prettyStructures, criteria.Models);
                //ViewBag.SearchStructures = prettyStructures.JoinBy();
                //ViewBag.SearchModels = criteria.Models.JoinBy();

                ViewBag.Header = "Search";
                ViewBag.DataAction = Url.Action("SearchData", new { structures = structures, models = models });

                return View("Result");
            } 
            catch 
            {
                return HttpNotFound();
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

        ActionResult GetDataInternal(string id, bool isStructure)
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
            catch (Exception e)
            {
                return HttpNotFound(e.Message);
            }
        }

        [CORS]
        public ActionResult Data(string id, string source)
        {
            if (source.Equals("ByStructure", StringComparison.OrdinalIgnoreCase)) return GetDataInternal(id, true);
            if (source.Equals("ByModel", StringComparison.OrdinalIgnoreCase)) return GetDataInternal(id, false);
            return HttpNotFound();
        }

        public ActionResult SearchData(string structures, string models)
        {
            try
            {
                var criteria = MotiveValidatorDatabaseApp.GetSearchCriteria(structures, models);
                var json = GetApp().GetSearchValidationJson(criteria, DbInterfaceProvider);
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

        public ActionResult MoleculeData(string id, string what)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(what)) return new { error = true, message = "Invalid entry or molecule id." }.AsJsonResult();
                var s = id.Trim() + MotiveValidatorDatabaseApp.SpecificResidueSeparator + what.Trim();
                var criteria = MotiveValidatorDatabaseApp.GetSearchCriteria(s, "");
                if (criteria.StructuresAndResidues.Count != 1) return new { error = true, message = "Invalid entry or molecule id." }.AsJsonResult();

                var json = GetApp().GetSearchValidationJson(criteria, DbInterfaceProvider);
                if (json == null) return new { error = true, message = "Data not found." }.AsJsonResult();
                return ActionHelper.JsonContent(json);
            }
            catch (InvalidOperationException e)
            {
                return new { error = true, message = e.Message }.AsJsonResult();
            }
            catch
            {
                return new { error = true, message = "Data not found." }.AsJsonResult();
            }
        }

        public ActionResult Molecule(string id, string what)
        {
            Func<string, ActionResult> error = msg =>
            {
                var errorData = new { _error = true, _message = msg }.ToJsonString(false);
                ViewBag.ValidationData = errorData;
                ViewBag.StructureData = "";
                return View();
            };

            try
            {
                id = (id ?? "").Trim();
                what = (what ?? "").Trim();
                ViewBag.Label = string.Format("{0} ({1})", id.Trim(), what.Trim());

                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(what)) return error("Invalid entry or molecule id.");

                var s = id + MotiveValidatorDatabaseApp.SpecificResidueSeparator + what;
                var criteria = MotiveValidatorDatabaseApp.GetSearchCriteria(s, "");
                if (criteria.StructuresAndResidues.Count != 1) return error("Invalid entry or molecule id.");
                var data = GetApp().GetSearchValidation(criteria, DbInterfaceProvider);
                if (data == null) return error("Data not found.");
                if (data.Models.Length != 1 || data.Models[0].Entries.Length == 0) return error("Data not found.");
                if (data.Models[0].Entries.Length != 1) return error("Multiple entries match the search criteria. This is most likely caused by degenerate molecules close to each other.<br/><br/>" +
                    string.Format("See the full validation report for <a href='{2}'>{0}:{1}</a> for more details.", id, what, Url.Action("Search", new { structures = id + ":" + what })));
                
                var entryId = data.Models[0].Entries[0].Id;
                //var validated = Path.Combine("structures", "mols", entryId + ".mol");
                //var input = Path.Combine("structures", "motifmols", entryId + ".mol");
                //var model = Path.Combine("structures", "models", data.Models[0].ModelName + "_model.mol");
                //var notvalidated = Path.Combine("structures", "notvalidated", "mol", entryId + ".mol");

                //var molData = new
                //{
                //    mol = db.GetEntry(validated),
                //    motifmol = db.GetEntry(input),
                //    modelmol = db.GetEntry(model),
                //    notvalidated = db.GetEntry(notvalidated)
                //};

                var db = GetApp().GetInterface(DbInterfaceProvider);

                var motif = db.GetEntry(Path.Combine("structures", "json", entryId + ".json"));
                var model = db.GetEntry(Path.Combine("structures", "models", data.Models[0].ModelName + "_model.json"));
                var notvalidated = db.GetEntry(Path.Combine("structures", "notvalidated", "json", entryId + ".json"));

                if (string.IsNullOrEmpty(motif) || string.IsNullOrEmpty(model)) return HttpNotFound();

                var sb = new System.Text.StringBuilder();

                sb.AppendLine("{");
                if (model != null) { sb.Append("\"model\": "); sb.Append(model); sb.Append(","); }
                if (notvalidated != null) { sb.Append("\"notvalidated\": "); sb.Append(notvalidated); sb.Append(","); }
                if (motif != null) { sb.Append("\"motif\": "); sb.Append(motif); }
                sb.Append("}");
                
                ViewBag.ValidationData = data.ToJsonString(false);
                ViewBag.StructureData = sb.ToString().Replace(Environment.NewLine, ""); //molData.ToJsonString(false);
                ViewBag.DataAction = Url.Action("MoleculeData", new { id = id, what = what });
                ViewBag.HideFooter = true;

                return View();
            }
            catch (InvalidOperationException e)
            {
                return error(e.Message);
            }
            catch
            {
                return error("Data not found.");
            }
        }

        //public ActionResult DataByModel(string id)
        //{
        //    return GetDataInternal(id, false);
        //}

        //public ActionResult DataByStructure(string id)
        //{
        //    return GetDataInternal(id, true);
        //}
        
        ActionResult StructureForVisualization(string modelId, string motiveId)
        {

            var db = GetApp().GetInterface(DbInterfaceProvider);

            var motif = db.GetEntry(Path.Combine("structures", "json", motiveId + ".json"));
            var model = db.GetEntry(Path.Combine("structures", "models", modelId + "_model.json"));


            if (string.IsNullOrEmpty(motif) || string.IsNullOrEmpty(model)) return HttpNotFound();

            var sb = new System.Text.StringBuilder();

            sb.AppendLine("{");
            sb.Append("  \"model\": "); sb.Append(model); sb.Append(","); sb.Append(Environment.NewLine);
            sb.Append("  \"motif\": "); sb.Append(motif); sb.Append(Environment.NewLine);
            sb.Append("}");
            return ActionHelper.JsonContent(sb.ToString());


            //var validated = Path.Combine("structures", "mols", motiveId + ".mol");
            //var input = Path.Combine("structures", "motifmols", motiveId + ".mol");
            //var model = Path.Combine("structures", "models", modelId + "_model.mol");

            //var db = GetApp().GetInterface(DbInterfaceProvider);

            //var ret = new
            //{
            //    mol = db.GetEntry(validated),
            //    motifmol = db.GetEntry(input),
            //    modelmol = db.GetEntry(model)
            //};

            //if (string.IsNullOrEmpty(ret.mol) || string.IsNullOrEmpty(ret.motifmol) || string.IsNullOrEmpty(ret.modelmol)) return HttpNotFound();

            //return ret.AsJsonResult();
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
                bool isModel = false;
                if (type.Equals("visualization", StringComparison.OrdinalIgnoreCase))
                {
                    return StructureForVisualization(model, motive);
                }
                else if (type.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".json";
                    path = Path.Combine(prefix, "json", motive + ".json");
                }
                else if (type.Equals("motif", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".pdb";
                    path = Path.Combine(prefix, "motifs", motive + ".pdb");
                }
                else if (type.Equals("motifjson", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".json";
                    path = Path.Combine(prefix, "json", motive + ".json");
                }
                else if (type.Equals("matched", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".pdb";
                    path = Path.Combine(prefix, "matched", motive + ".pdb");
                }
                else if (type.Equals("modelpdb", StringComparison.OrdinalIgnoreCase))
                {
                    isModel = true;
                    ext = ".pdb";
                    path = Path.Combine(prefix, "models", model + "_model.pdb");
                }
                else if (type.Equals("modeljson", StringComparison.OrdinalIgnoreCase))
                {
                    isModel = true;
                    ext = ".json";
                    path = Path.Combine(prefix, "models", model + "_model.json");
                }
                else if (type.Equals("notanalyzedpdb", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".pdb";
                    path = Path.Combine(prefix, "notanalyzed", "pdb", motive + ".pdb");
                }
                else if (type.Equals("notanalyzedjson", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".json";
                    path = Path.Combine(prefix, "notanalyzed", "json", motive + ".json");
                }
                else if (type.Equals("notvalidatedpdb", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".pdb";
                    path = Path.Combine(prefix, "notvalidated", "pdb", motive + ".pdb");
                }
                else if (type.Equals("notvalidatedjson", StringComparison.OrdinalIgnoreCase))
                {
                    ext = ".json";
                    path = Path.Combine(prefix, "notvalidated", "json", motive + ".json");
                }
                else
                {
                    return HttpNotFound(); //ActionHelper.TextContent("Not found.");
                }

                var source = GetApp().GetInterface(DbInterfaceProvider).GetEntry(path);

                if (string.IsNullOrEmpty(source)) return HttpNotFound();

                if (action.Equals("download", StringComparison.OrdinalIgnoreCase))
                {
                    return File(System.Text.Encoding.UTF8.GetBytes(source), "text/plain; charset=\"utf-8\"", (isModel ? model + "_model" : motive) + ext);
                }
                else
                {
                    if (ext == ".json") return ActionHelper.JsonContent(source);
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

        [HttpPost]
        public ActionResult ComputeCustom(string[] structures, string[] models)
        {
            try 
            {
                if (IsSmallSearch(structures == null ? 0 : structures.Length, models == null ? 0 : models.Length))
                {
                    structures = structures ?? new string[0];
                    models = models ?? new string[0];
                    return RedirectToAction("Search", new { structures = structures.JoinBy(","), models = models.JoinBy(",") });
                }

                var app = GetApp();
                var comp = app.CreateAggregatorComputation(UserHelper.GetUserIP(Request), structures, models);
                comp.Schedule();
                WebChemistry.Platform.Server.ServerManager.MasterServer.ComputationScheduler.Update();

                var state = comp.GetStatus().State;
                if (state == ComputationState.New)
                {
                    return new { ok = false, error = "Concurrent computation limit reached. Please try again later." }.AsJsonResult();
                }

                return new { ok = true, id = comp.ShortId }.AsJsonResult();

            }
            catch (Exception)
            {
                return new { ok = false, error = "Unexpected error. Try again later." }.AsJsonResult();
            }
        }
    }
}
