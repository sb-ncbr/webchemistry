using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using WebChemistry.Framework.Core;
using WebChemistry.MotiveValidator.DataModel;
using WebChemistry.Platform.Computation;
using WebChemistry.Platform.MoleculeDatabase;
using WebChemistry.Platform.Server;
using WebChemistry.Web.Filters;
using WebChemistry.Web.Helpers;

namespace WebChemistry.Web.Controllers
{
    [Compress]
    //[UnderMaintenance]
    public class MotiveValidatorController : Controller
    {
        //
        // GET: /MotiveValidator/
        
        static DatabaseInfo GetWwPdbCcd()
        {
            return ServerManager.MasterServer.PublicDatabases.TryGetByShortId("wwpdbccd_cif");
        }

        static DatabaseInfo GetPdb()
        {
            return ServerManager.MasterServer.PublicDatabases.TryGetByShortId("pdb_cif");
        }

        static MotiveValidatorApp GetApp()
        {
            return ServerHelper.Atlas.GetOrCreateApp("MotiveValidator", server =>
                {
                    return MotiveValidatorApp.Create("MotiveValidator", GetWwPdbCcd(), server);
                });
        }
        
        public ActionResult Index()
        {
            var app = GetApp();
            ViewBag.RecentComputations = app.RecentComputations;
            if (ViewBag.RecentComputations == null) ViewBag.RecentComputations = new string[0];
            var db = GetWwPdbCcd().GetStatistics();
            ViewBag.DbLastUpdated = db.LastUpdated.ToShortDateString();
            ViewBag.DbStructureCount = db.MoleculeCount;

            var pdb = GetPdb();
            if (pdb == null)
            {
                ViewBag.PdbLastUpdated = "Unknown";
                ViewBag.PdbStructureCount = "Unknown";
            }
            else
            {
                var stats = pdb.GetStatistics();
                ViewBag.PdbLastUpdated = stats.LastUpdated.ToShortDateString();
                ViewBag.PdbStructureCount = stats.MoleculeCount;
            }

            var svc = ServerManager.MasterServer.Services.GetService("MotiveValidator");
            ViewBag.ServiceVersion = svc.CurrentVersion;
            ViewBag.AllVersions = svc.Versions.Keys.OrderByDescending(k => k).ToArray();
            return View();
        }

        void Schedule(ComputationInfo comp)
        {
            comp.Schedule();
            var scheduler = ServerManager.MasterServer.ComputationScheduler;
            scheduler.Update();
            var state = comp.GetStatus().State;
            if (state == ComputationState.New)
            {
                throw new InvalidOperationException("Concurrent computation limit reached. Please try again later.");
            }
        }

        static string FindAndWriteWwPdbCcd(string modelsText, int maxCount, bool zipped, string context)
        {
            if (!zipped && maxCount != 1)
            {
                throw new InvalidOperationException("More than one model must be zipped.");
            }

            if (modelsText == null) throw new ArgumentException(string.Format("Missing data. ({0})", context));

            var fields = modelsText.Split(", ;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Distinct(f => f, StringComparer.OrdinalIgnoreCase).ToArray();
            if (fields.Length == 0) throw new ArgumentException(string.Format("Missing data. ({0})", context));
            if (fields.Length > maxCount) throw new ArgumentException(string.Format("Too many names ({0}). Max {1}, got {2}.", context, maxCount, fields.Length));

            var app = GetApp();
            var ligands = DatabaseView.Load(app.AllModelsView).Snapshot()
                .ToDictionary(l =>
                    l.FilenameId.Substring(0, l.FilenameId.LastIndexOf('_') > 0 ? l.FilenameId.LastIndexOf('_') : l.FilenameId.Length), 
                    StringComparer.OrdinalIgnoreCase);
            
            var models = fields                
                .Select(f => new
                {
                    Name = f.ToUpperInvariant(),
                    Model = ligands.ContainsKey(f) ? ligands[f] : null
                })
                .ToArray();

            var missing = models.Where(m => m.Model == null).ToArray();

            if (missing.Length > 0)
            {
                throw new InvalidOperationException(string.Format("Structure(s) with ID '{0}' were not found in our wwPDB Chemical Component Dictionary mirror.", string.Join(", ", missing.Select(m => m.Name))));
            }

            var temp = Path.GetTempFileName();
            try
            {
                if (zipped)
                {
                    using (var stream = System.IO.File.OpenWrite(temp))
                    using (var zip = new ZipOutputStream(stream))
                    using (var writer = new StreamWriter(zip))
                    {
                        foreach (var m in models)
                        {
                            zip.PutNextEntry(new ZipEntry(m.Model.FilenameId + m.Model.Extension));
                            writer.Write(m.Model.ReadSource());
                            writer.Flush();
                            zip.CloseEntry();

                            if (ligands.ContainsKey(m.Model.FilenameId))
                            {
                                var mol = ligands[m.Model.FilenameId];
                                zip.PutNextEntry(new ZipEntry(mol.FilenameId + mol.Extension));
                                writer.Write(mol.ReadSource());
                                writer.Flush();
                                zip.CloseEntry();
                            }
                        }
                    }
                }
                else
                {
                    using (var stream = new StreamWriter(System.IO.File.OpenWrite(temp)))
                    {
                        stream.Write(models[0].Model.ReadSource());
                    }
                }
            }
            catch
            {
                if (System.IO.File.Exists(temp)) System.IO.File.Delete(temp);
                throw;
            }

            return temp;
        }

        static string FindAndWritePdb(string idsText, int maxCount, string context)
        {
            if (idsText == null) throw new ArgumentException(string.Format("Missing data. ({0})", context));

            var fields = idsText.Split(", ;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Distinct(f => f, StringComparer.OrdinalIgnoreCase).ToArray();
            if (fields.Length == 0) throw new ArgumentException(string.Format("Missing data. ({0})", context));
            if (fields.Length > maxCount) throw new ArgumentException(string.Format("Too many names ({0}). Max {1}, got {2}.", context, maxCount, fields.Length));

            var pdb = ServerManager.MasterServer.PublicDatabases.GetAll().FirstOrDefault(db => db.ShortId.EqualOrdinalIgnoreCase("pdb_cif"));
            if (pdb == null) throw new InvalidOperationException("PDB Database not found.");
            var pdbs = pdb.DefaultView.Snapshot().ToDictionary(e => e.FilenameId, StringComparer.OrdinalIgnoreCase);
            
            var missing = fields.Where(m => !pdbs.ContainsKey(m)).ToArray();

            if (missing.Length > 0)
            {
                var message = string.Format("Structure(s) with ID <b>'{0}'</b> were not found in our PDBe.org mirror.<br/><br/>" +
                    "Possible reasons:" +
                    "<ol>" +
                    "<li>There is no such PDB ID. </li>" +
                    "<li>The molecule has one PDB ID, but several PDB files associated with this ID. To verify such structures, either type in the list of all PDB IDs the structure is composed of, or manually load the structure in PDBx/mmCIF format.</li>" +
                    "<li>Structure(s) are obsolete, i.e., they are no longer accessible in the main PDB repository. To verify obsolete structures, you need to upload them manually.</li>" +
                    "</ol>", 
                    string.Join(", ", missing));

                var present = fields.Where(m => pdbs.ContainsKey(m)).ToArray();
                if (present.Length > 0)
                {
                    message += "<br/>You may restart your calculation using only the PDB IDs that were retrieved correctly:<br/>" + string.Join(", ", present);
                }

                throw new InvalidOperationException(message);
            }

            var temp = Path.GetTempFileName();
            try
            {
                using (var stream = System.IO.File.OpenWrite(temp))
                using (var zip = new ZipOutputStream(stream))
                using (var writer = new StreamWriter(zip))
                {
                    foreach (var m in fields)
                    {
                        var e = pdbs[m];
                        zip.PutNextEntry(new ZipEntry(e.FilenameId + e.Extension));
                        writer.Write(e.ReadSource());
                        writer.Flush();
                        zip.CloseEntry();
                    }
                }
            }
            catch
            {
                if (System.IO.File.Exists(temp)) System.IO.File.Delete(temp);
                throw;
            }

            return temp;
        }

        
        static string VerifyAndWriteTemp(HttpPostedFileBase file, bool zipped, string context)
        {
            if (file == null) throw new ArgumentException(string.Format("Missing data. ({0})", context));

            var extension = new FileInfo(file.FileName).Extension.ToLowerInvariant();

            bool isZip = file.FileName.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase);
            if (!zipped && !StructureReader.IsStructureFilename(file.FileName))
            {
                //throw new ArgumentException(string.Format("Expected ZIP file. ({0})", context));
                throw new ArgumentException(string.Format("[{0}] {1} file format is not supported. Supported formats are .zip, .pdb, .pdb? (assembly), .cif, .mmcif., .sdf, .mol.", context, extension));
            }
            else if (!StructureReader.IsStructureFilename(file.FileName) && !isZip)
            {
                throw new ArgumentException(string.Format("[{0}] {1} file format is not supported. Supported formats are .zip, .pdb, .pdb? (assembly), .cif, .mmcif., .sdf, .mol.", context, extension));
            }
            
            var temp = Path.GetTempFileName();

            try
            {
                if (!isZip && zipped)
                {
                    using (var stream = System.IO.File.OpenWrite(temp))
                    using (var zip = new ZipOutputStream(stream))
                    {
                        zip.PutNextEntry(new ZipEntry(file.FileName));
                        file.InputStream.CopyTo(zip);
                        zip.CloseEntry();
                    }
                }
                else 
                {
                    using (var stream = System.IO.File.OpenWrite(temp))
                    {                    
                        file.InputStream.CopyTo(stream);
                    }
                }

                if (zipped)
                {
                    ZipFile zf = null;
                    try
                    {
                        zf = new ZipFile(temp);
                        if (!zf.TestArchive(false))
                        {
                            zf.Close();
                            throw new InvalidDataException(string.Format("Invalid ZIP file. ({0})", context));
                        }
                        zf.Close();
                    }
                    catch
                    {
                        if (zf != null)
                        {
                            try
                            {
                                zf.Close();
                            }
                            catch { }
                        }
                        throw new InvalidDataException(string.Format("Invalid ZIP file. ({0})", context));
                    }
                }
            }
            catch
            {
                if (System.IO.File.Exists(temp)) System.IO.File.Delete(temp);
                throw;
            }

            return temp;
        }

        [HttpPost]
        public ActionResult UploadSugars(HttpPostedFileBase structures, string structureNames)
        {
            string inputFile = null;
            ComputationInfo comp = null;

            try
            {
                inputFile = structures != null
                    ? VerifyAndWriteTemp(structures, true, "Structures")
                    : FindAndWritePdb(structureNames, 250, "Structure Names");

                var app = GetApp();
                comp = app.CreateComputation(UserHelper.GetUserIP(Request), MotiveValidationType.Sugars);
                System.IO.File.Move(inputFile, System.IO.Path.Combine(comp.MakeInputDirectory(), "motives.zip"));
                Schedule(comp);

                return new { ok = true, id = comp.ShortId }.AsJsonResult();
            }
            catch (Exception e)
            {
                if (inputFile != null && System.IO.File.Exists(inputFile)) System.IO.File.Delete(inputFile);
                if (comp != null) comp.Fail(e.Message);

                return new { ok = false, message = e.Message }.AsJsonResult();
            }
        }

        static string GetModelName(string path)
        {
            try
            {
                var s = StructureReader.Read("model.pdb", () => System.IO.File.OpenRead(path));
                var r = s.Structure.PdbResidues();
                if (r.Count != 1) throw new InvalidOperationException(string.Format("The model contains too many residues. Got {0}, expected 1.", r.Count));
                return r[0].Name;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Error reading model: " + e.Message, e);
            }
        }

        [HttpPost]
        public ActionResult UploadModel(HttpPostedFileBase model, HttpPostedFileBase motifs, string modelName)
        {
            string motifsFile = null, modelFile = null;
            ComputationInfo comp = null;
            try
            {
                modelFile = model != null 
                    ? VerifyAndWriteTemp(model, true, "Model")
                    : FindAndWriteWwPdbCcd(modelName, 1, true, "wwPDB CCD Name");
                motifsFile = VerifyAndWriteTemp(motifs, true, "Motifs");
                var app = GetApp();
                comp = app.CreateComputation(UserHelper.GetUserIP(Request), MotiveValidationType.Model);
                var inputDir = comp.MakeInputDirectory();
                System.IO.File.Move(motifsFile, System.IO.Path.Combine(inputDir, "motives.zip"));
                System.IO.File.Move(modelFile, System.IO.Path.Combine(inputDir, "models.zip"));
                Schedule(comp);

                return new { ok = true, id = comp.ShortId }.AsJsonResult();
            }
            catch (Exception e)
            {
                if (motifsFile != null && System.IO.File.Exists(motifsFile)) System.IO.File.Delete(motifsFile);
                if (modelFile != null && System.IO.File.Exists(modelFile)) System.IO.File.Delete(modelFile);
                if (comp != null) comp.Fail(e.Message);

                return new { ok = false, message = e.Message }.AsJsonResult();
            }
        }

        [HttpPost]
        public ActionResult UploadCustomModels(HttpPostedFileBase models, HttpPostedFileBase structures, string modelNames, string structureNames)
        {
            string motifsFile = null, modelFile = null;
            ComputationInfo comp = null;

            try
            {
                modelFile = models != null
                    ? VerifyAndWriteTemp(models, true, "Models")
                    : FindAndWriteWwPdbCcd(modelNames, 250, true, "wwPDB CCD Names");

                motifsFile = structures != null
                    ? VerifyAndWriteTemp(structures, true, "Structures")
                    : FindAndWritePdb(structureNames, 250, "Structure Names");
                
                var app = GetApp();
                comp = app.CreateComputation(UserHelper.GetUserIP(Request), MotiveValidationType.CustomModels);
                var inputDir = comp.MakeInputDirectory();
                System.IO.File.Move(motifsFile, System.IO.Path.Combine(inputDir, "motives.zip"));
                System.IO.File.Move(modelFile, System.IO.Path.Combine(inputDir, "models.zip"));
                Schedule(comp);

                return new { ok = true, id = comp.ShortId }.AsJsonResult();
            }
            catch (Exception e)
            {
                if (motifsFile != null && System.IO.File.Exists(motifsFile)) System.IO.File.Delete(motifsFile);
                if (modelFile != null && System.IO.File.Exists(modelFile)) System.IO.File.Delete(modelFile);
                if (comp != null) comp.Fail(e.Message);

                return new { ok = false, message = e.Message }.AsJsonResult();
            }
        }

        public ActionResult Result(string id)
        {
            var prefix = GetApp().ComputationsId;
            var comp = ComputationInfo.TryLoad(prefix.GetChildId(id));

            ViewBag.IsNotValidatorDb = true;
            ViewBag.IsFinished = false;
            if (comp != null)
            {
                ViewBag.FullComputationId = comp.Id.ToString();
                bool isFinished = comp.GetStatus().State == ComputationState.Success;
                ViewBag.IsFinished = isFinished;
            }
            else
            {
                return HttpNotFound();
            }

            ViewBag.Id = id;
            ViewBag.Date = comp.DateCreated.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ViewBag.OnlineUntil = comp.DateCreated.AddMonths(1).ToLongDateString();
            return View();
        }

        public ActionResult IsRunning(string id)
        {
            var prefix = GetApp().ComputationsId;
            var comp = ComputationInfo.TryLoad(prefix.GetChildId(id));
            if (comp == null) return new { Exists = false, IsRunning = false }.AsJsonResult();
            return new { Exists = true, IsRunning = comp.IsRunning() }.AsJsonResult();
        }

        public ActionResult Status(string id)
        {
            var prefix = GetApp().ComputationsId;
            var comp = ComputationInfo.TryLoad(prefix.GetChildId(id));
            if (comp == null) return new { Exists = false, IsRunning = false }.AsJsonResult();
            return ComputationHelper.GetStatus(comp).AsJsonResult();
        }

        public ActionResult Data(string id)
        {
            var prefix = GetApp().ComputationsId;
            var comp = ComputationInfo.TryLoad(prefix.GetChildId(id));

            if (comp == null)
            {
                return new { _error = true, _message = "Computation not found." }.AsJsonResult();
            }

            return ActionHelper.FileJsonContent(Path.Combine(comp.GetResultFolderId().GetEntityPath(), "result.json"));
        }

        static string ReadZipEntry(string zipName, string entryName)
        {
            using (var zip = new ZipFile(Path.Combine(zipName)))
            {
                var index = zip.FindEntry(entryName, true);
                if (index >= 0)
                {
                    using (var reader = new StreamReader(zip.GetInputStream(index)))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            return null;
        }

        ActionResult StructureForVisualization(string id, string modelId, string motiveId)
        {
            var comp = ComputationInfo.TryLoad(GetApp().ComputationsId.GetChildId(id));
            var folder = comp.GetResultFolderId().GetEntityPath();

            //var validated = ReadZipEntry(Path.Combine(folder, modelId, "mols.zip"), motiveId + ".mol");
            //var input = ReadZipEntry(Path.Combine(folder, modelId, "motivesmol.zip"), motiveId + ".mol");
            //var model = System.IO.File.ReadAllText(Path.Combine(folder, modelId, modelId + "_model.mol"));

            var motif = ReadZipEntry(Path.Combine(folder, modelId, "json.zip"), motiveId + ".json");
            var model = System.IO.File.ReadAllText(Path.Combine(folder, modelId, modelId + "_model.json"));


            if (string.IsNullOrEmpty(motif) || string.IsNullOrEmpty(model)) return HttpNotFound();

            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine("{");
            sb.Append("  \"model\": "); sb.Append(model); sb.Append(","); sb.Append(Environment.NewLine);
            sb.Append("  \"motif\": "); sb.Append(motif); sb.Append(Environment.NewLine);            
            sb.Append("}");
            return ActionHelper.JsonContent(sb.ToString());

            //var ret = new
            //{
            //    mol = validated,
            //    motifmol = input,
            //    modelmol = model
            //};

            //if (string.IsNullOrEmpty(ret.mol) || string.IsNullOrEmpty(ret.motifmol) || string.IsNullOrEmpty(ret.modelmol)) return HttpNotFound();

            //return ret.AsJsonResult();
        }

        public ActionResult Structure(string id)
        {
            try
            {
                var model = Request["model"] ?? "";
                var motive = Request["sid"] ?? "";
                var type = Request["type"] ?? "";
                var action = Request["action"] ?? "";

                bool isModel = false;
                string filename, ext;
                if (type.Equals("visualization", StringComparison.OrdinalIgnoreCase))
                {
                    return StructureForVisualization(id, model, motive);
                }
                //else if (type.Equals("mol", StringComparison.OrdinalIgnoreCase))
                //{
                //    filename = "mols.zip";
                //    ext = ".mol";
                //}
                else if (type.Equals("motif", StringComparison.OrdinalIgnoreCase))
                {
                    filename = "motives.zip";
                    ext = ".pdb";
                }
                //else if (type.Equals("motifmol", StringComparison.OrdinalIgnoreCase))
                //{
                //    filename = "motivesmol.zip";
                //    ext = ".mol";
                //}
                else if (type.Equals("matched", StringComparison.OrdinalIgnoreCase))
                {
                    filename = "matched.zip";
                    ext = ".pdb";
                }
                else if (type.Equals("notanalyzedpdb", StringComparison.OrdinalIgnoreCase))
                {
                    filename = "notanalyzed.zip";
                    ext = ".pdb";
                }
                //else if (type.Equals("notanalyzedmol", StringComparison.OrdinalIgnoreCase))
                //{
                //    filename = "notanalyzed.zip";
                //    ext = ".mol";
                //}
                else if (type.Equals("notanalyzedjson", StringComparison.OrdinalIgnoreCase))
                {
                    filename = "notanalyzed.zip";
                    ext = ".json";
                }
                else if (type.Equals("notvalidatedpdb", StringComparison.OrdinalIgnoreCase))
                {
                    filename = "notvalidated.zip";
                    ext = ".pdb";
                }
                //else if (type.Equals("notvalidatedmol", StringComparison.OrdinalIgnoreCase))
                //{
                //    filename = "notvalidated.zip";
                //    ext = ".mol";
                //}
                else if (type.Equals("notvalidatedjson", StringComparison.OrdinalIgnoreCase))
                {
                    filename = "notvalidated.zip";
                    ext = ".json";
                }
                else if (type.Equals("modelpdb", StringComparison.OrdinalIgnoreCase))
                {
                    isModel = true;
                    filename = model + "_model.pdb";
                    ext = ".pdb";
                    motive = model + "_model";
                }
                //else if (type.Equals("modelmol", StringComparison.OrdinalIgnoreCase))
                //{
                //    isModel = true;
                //    filename = model + "_model.mol";
                //    ext = ".mol";
                //    motive = model + "_model";
                //}
                else
                {
                    return HttpNotFound(); //ActionHelper.TextContent("Not found.");
                }

                var source = "";
                var comp = ComputationInfo.TryLoad(GetApp().ComputationsId.GetChildId(id));

                if (isModel)
                {
                    source = System.IO.File.ReadAllText(Path.Combine(comp.GetResultFolderId().GetEntityPath(), model, filename));
                }
                else
                {
                    using (var zip = new ZipFile(Path.Combine(comp.GetResultFolderId().GetEntityPath(), model, filename)))
                    {
                        var index = zip.FindEntry(motive + ext, true);
                        if (index >= 0)
                        {
                            using (var reader = new StreamReader(zip.GetInputStream(index)))
                            {
                                source = reader.ReadToEnd();
                            }
                        }
                    }
                }

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
                return HttpNotFound(); //ActionHelper.TextContent("Not found.");
            }
        }
        
        public ActionResult Download(string id, string type)
        {
            try
            {
                var prefix = GetApp().ComputationsId;
                var comp = ComputationInfo.TryLoad(prefix.GetChildId(id));

                string file = "", name = "";

                if (string.IsNullOrEmpty(type) || type.EqualOrdinalIgnoreCase("Result"))
                {
                    file = Path.Combine(comp.GetResultFolderId().GetEntityPath(), "result.zip");
                    name = "result.zip";
                }
                else if (type.EqualOrdinalIgnoreCase("InputModels"))
                {
                    file = Path.Combine(comp.GetInputDirectory(), "models.zip");
                    name = "models.zip";
                }
                else if (type.EqualOrdinalIgnoreCase("InputStructures"))
                {
                    file = Path.Combine(comp.GetInputDirectory(), "motives.zip");
                    name = "structures.zip";
                }
                else return HttpNotFound();
                
                return File(file, "application/zip,application/octet-stream", name);
            }
            catch
            {
                return HttpNotFound();
            }
        }
        
        public ActionResult ListModels(string id)
        {
            string ret = "";
            if (id.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var view = DatabaseView.Load(GetApp().AllModelsView);
                ret = string.Join("\n", view.Snapshot().Select(e => e.FilenameId.Replace("_model", "")));
            }
            else if (id.Equals("Sugars", StringComparison.OrdinalIgnoreCase))
            {
                var view = DatabaseView.Load(GetApp().SugarModelsView);
                ret = string.Join("\n", view.Snapshot().Select(e => e.FilenameId.Replace("_model", "")));
            }
            return Content(ret, "text/plain", Encoding.Default);
        }

        public ActionResult ChangeLog()
        {
            var svc = ServerManager.MasterServer.Services.GetService("MotiveValidator");
            return ActionHelper.TextContent(svc.GetChangeLog());
        }

        public ActionResult DownloadService(string id)
        {
            try
            {
                var svc = ServerManager.MasterServer.Services.GetService("MotiveValidator");

                AppVersion version = string.IsNullOrEmpty(id) ? svc.CurrentVersion : AppVersion.Parse(id);
                var path = svc.GetVersionPath(version);

                if (!Directory.Exists(path))
                {
                    return HttpNotFound(string.Format("Version '{0}' not found.", id));
                }

                byte[] bytes = null;
                using (var byteStream = new MemoryStream())
                using (var zip = new ZipOutputStream(byteStream))
                {
                    byte[] buffer = new byte[4096];
                    foreach (var f in Directory.GetFiles(path))
                    {
                        zip.PutNextEntry(new ZipEntry(Path.GetFileName(f)));
                        using (FileStream fs = System.IO.File.OpenRead(f))
                        {
                            int sourceBytes;
                            do
                            {
                                sourceBytes = fs.Read(buffer, 0, buffer.Length);
                                zip.Write(buffer, 0, sourceBytes);
                            }
                            while (sourceBytes > 0);
                        }
                        zip.CloseEntry();
                    }
                    zip.Flush();
                    zip.Finish();

                    bytes = new byte[byteStream.Length];
                    Array.Copy(byteStream.GetBuffer(), bytes, byteStream.Length);
                }
                return File(bytes, "application/octet-stream", "MotiveValidator_" + version.ToString() + ".zip");
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Error creating the service package: " + e.Message);
            }
        }

        public ActionResult __Computations(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !id.EqualOrdinalIgnoreCase("csv"))
            {
                var user = ServerHelper.Atlas.Users.GetUserByName("MotiveValidator");
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
                var user = ServerHelper.Atlas.Users.GetUserByName("MotiveValidator");
                var csv = user.Computations.GetAll().OrderByDescending(c => c.DateCreated).GetExporter()
                    .AddStringColumn(c => c.DateCreated, "Date")
                    .AddStringColumn(c => c.Source, "Source")
                    .AddStringColumn(c => c.ShortId, "Id")
                    .ToCsvString();
                return ActionHelper.TextContent(csv);
            }
        }
    }
}
