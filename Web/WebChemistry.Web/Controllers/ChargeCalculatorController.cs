using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebChemistry.Web.Helpers;
using WebChemistry.Platform;
using WebChemistry.Platform.Users;
using WebChemistry.Charges.Service.DataModel;
using WebChemistry.Platform.Computation;
using WebChemistry.Platform.Server;
using System.IO;
using WebChemistry.Framework.Core;
using ICSharpCode.SharpZipLib.Zip;
using WebChemistry.Platform.Services;
using System.Xml.Linq;
using WebChemistry.Web.Filters;

namespace WebChemistry.Web.Controllers
{
    [Compress]
    public class ChargeCalculatorController : AppControllerBase
    {
        public static readonly string AppName = "AtomicChargeCalculator";

        static UserInfo GetAppUser()
        {
            var user = ServerHelper.Atlas.Users.TryGetByShortId("ChargeCalculator");
            if (user == null)
            {
                user = ServerHelper.Atlas.Users.CreateUser("ChargeCalculator");
                var profile = user.GetProfile();
                profile.ConcurrentComputationLimit = 16;
                user.SaveProfile(profile);
            }
            return user;
        }

        public ServiceInfo GetChargesService()
        {
            return ServerManager.MasterServer.Services.GetService("Charges");
        }

        private ComputationInfo GetComputation(string id)
        {
            return GetAppUser().Computations.TryGetByShortId(id);
        }

        public ActionResult Index()
        {
            var svc = GetChargesService();
            ViewBag.ServiceVersion = svc.CurrentVersion;
            ViewBag.AllVersions = svc.Versions.Keys.OrderByDescending(k => k).ToArray();
            return View();
        }

        static string CreateTempInput(HttpPostedFileBase file, string context)
        {
            if (file == null) throw new ArgumentException(string.Format("Missing data.", context));

            var extension = new FileInfo(file.FileName).Extension.ToLowerInvariant();

            bool isZip = file.FileName.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase);
            if (!isZip && !StructureReader.IsStructureFilename(file.FileName))
            {
                throw new ArgumentException(string.Format("[{0}] {1} file format is not supported. Supported formats are {2}.", context, extension, String.Join(", ", StructureReader.SupportedExtensions)));
            }

            var temp = Path.GetTempFileName();

            try
            {
                if (!isZip)
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

                if (isZip)
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
        public ActionResult Upload(HttpPostedFileBase file)
        {
            string tmpInput = null;
            ZipFile zf = null;
            ChargeComputationWrapper computation = null;
            try
            {
                tmpInput = CreateTempInput(file, "Input");

                if ((file.FileName ?? "").EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    using (zf = new ZipFile(tmpInput))
                    {
                        if (!zf.TestArchive(false))
                        {
                            throw new InvalidDataException("Invalid ZIP file.");
                        }
                    }
                }

                computation = ChargeCalculatorApp.CreateComputation(GetAppUser(), UserHelper.GetUserIP(Request));
                System.IO.File.Move(tmpInput, Path.Combine(computation.AnalyzerInputDirectory, "input.zip"));
                tmpInput = null;
            }
            catch (Exception e)
            {
                return ActionHelper.JsonContent(new { ok = false, message = e.Message }.ToJsonString());
            }
            finally
            {
                if (!string.IsNullOrEmpty(tmpInput) && System.IO.File.Exists(tmpInput)) System.IO.File.Delete(tmpInput);
            }

            try
            {
                Schedule(computation.AnalyzerComputation);
                return ActionHelper.JsonContent(new { ok = true, id = computation.ChargeComputation.ShortId }.ToJsonString());
            }
            catch (Exception e)
            {
                return ActionHelper.JsonContent(new { ok = false, message = e.Message }.ToJsonString());
            }
        }
        
        [HttpPost]
        public ActionResult CreateExample(string id)
        {
            ChargeComputationWrapper computation = null;

            var exampleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Paracetamol", "Protegrin", "Proteasome" };
            id = id ?? "";
            if (string.IsNullOrWhiteSpace(id) || !exampleIds.Contains(id))
            {
                return new { ok = false, message = "Invalid example id." }.AsJsonResult();
            }

            try
            {
                var path = Server.MapPath("~/Content/ChargeCalculator/");
                computation = ChargeCalculatorApp.CreateComputation(GetAppUser(), UserHelper.GetUserIP(Request));
                System.IO.File.Copy(Path.Combine(Server.MapPath("~/Content/ChargeCalculator/"), id + ".smpl"), Path.Combine(computation.AnalyzerInputDirectory, "input.zip"));
            }
            catch
            {
                return ActionHelper.JsonContent(new { ok = false, message = "Could not initialize the example. Please try again later." }.ToJsonString());
            }

            try
            {
                Schedule(computation.AnalyzerComputation);
                return ActionHelper.JsonContent(new { ok = true, id = computation.ChargeComputation.ShortId }.ToJsonString());
            }
            catch (Exception e)
            {
                return ActionHelper.JsonContent(new { ok = false, message = e.Message }.ToJsonString());
            }
        }

        public ActionResult Kill(string id)
        {
            try
            {
                var comp = GetComputation(id);

                if (comp.GetStatus().State == ComputationState.Running)
                {
                    comp.KillIfRunning(true);
                }

                return RedirectToAction("Result", new { id = id });
            } 
            catch
            {
                return RedirectToAction("Result", new { id = id });
            }
        }

        public ActionResult AnalysisData(string id)
        {
            try
            {
                return ActionHelper.JsonContent(ChargeCalculatorApp.GetAnalysisDataString(GetComputation(id)));
            }
            catch
            {
                return HttpNotFound();
            }
        }

        public ActionResult Summary(string id)
        {
            try
            {
                return ActionHelper.JsonContent(ChargeCalculatorApp.GetSummaryString(GetComputation(id)));
            }
            catch
            {
                return HttpNotFound();
            }
        }

        ActionResult Configure(ComputationInfo chargesComputation, string example)
        {
            var analysisComputation = ComputationInfo.Load(chargesComputation.DependentObjectIds[0]);

            ViewBag.ComputationId = analysisComputation.ShortId;
            ViewBag.ChargeComputationId = chargesComputation.ShortId;
            ViewBag.IsFinished = analysisComputation.GetStatus().State == ComputationState.Success;
            ViewBag.Example = example ?? "";

            return View("Configure");
        }

        ActionResult ResultAction(ComputationInfo chargesComputation)
        {
            var comp = chargesComputation;

            ViewBag.IsFinished = false;
            if (comp != null)
            {
                ViewBag.DateCreated = comp.DateCreated.ToString(System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.LongDatePattern, System.Globalization.CultureInfo.InvariantCulture) +
                            ", " + comp.DateCreated.ToString(System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.LongTimePattern, System.Globalization.CultureInfo.InvariantCulture);
                ViewBag.FullComputationId = comp.Id.ToString();
                var status = comp.GetStatus();
                bool isFinished = status.State == ComputationState.Success;
                ViewBag.IsFinished = isFinished;
                ViewBag.ResultSize = GetResultSize(comp);
            }
            else
            {
                return HttpNotFound();
            }

            ViewBag.Id = chargesComputation.ShortId;
            ViewBag.Date = comp.DateCreated.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ViewBag.OnlineUntil = comp.DateCreated.AddMonths(1).ToLongDateString();
            return View("Result");
        }

        [HttpPost]
        public ActionResult ValidateSet(string xml)
        {
            if (string.IsNullOrEmpty(xml))
            {
                return new { ok = false, message = "Cannot be empty." }.AsJsonResult();
            }

            try 
            {
                WebChemistry.Charges.Core.EemParameterSet[] sets;
                
                var el = XElement.Parse(xml);

                if (el.Name.LocalName.EqualOrdinal("Sets") || el.Name.LocalName.EqualOrdinal("ParameterSets"))
                {
                    sets = el.Elements().Select(e => WebChemistry.Charges.Core.EemParameterSet.FromXml(e)).ToArray();
                }
                else
                {
                    sets = new[] { WebChemistry.Charges.Core.EemParameterSet.FromXml(el) };
                }

                //var set = WebChemistry.Charges.Core.EemParameterSet.FromXml(el);

                foreach (var set in sets)
                {
                    try
                    {
                        new FileInfo(set.Name + ".json");
                    }
                    catch
                    {
                        return new { ok = false, message = string.Format("{0} is not a valid set name.", set.Name) }.AsJsonResult();
                    }
                }

                var entries = sets.Select(set => new ChargeAnalyzerParameterSetEntry
                {
                    Name = set.Name,
                    Properties = set.Properties.Select(p => new[] { p.Item1, p.Item2 }).ToArray(),
                    AvailableAtoms = set.ParameterGroups.SelectMany(g => g.Parameters.Keys).Distinct().Select(e => e.ToString()).OrderBy(e => e).ToArray(),
                    Xml = set.ToXml().ToString()
                }).ToArray();
                return new { ok = true, sets = entries }.AsJsonResult();
            } 
            catch (Exception e)
            {
                return new { ok = false, message = e.Message }.AsJsonResult();
            }
        }

        [HttpPost]
        public ActionResult Compute(string id, string config)
        {
            var comp = GetComputation(id);
            if (comp == null) return HttpNotFound();
            
            if (config == null)
            {
                return new { ok = false, message = "Config required." }.AsJsonResult();
            }
            
            ChargesServiceConfig configObject = null;
            try
            {
                configObject = JsonHelper.FromJsonString<ChargesServiceConfig>(config);
                ChargeCalculatorApp.UpdateConfig(comp, configObject);
            }
            catch (Exception e)
            {
                return new { ok = false, message = e.Message }.AsJsonResult();
            }

            try
            {
                Schedule(comp);
                ChargeCalculatorApp.UpdateState(comp, ChangeComputationStates.Analyzed);
            }
            catch (Exception e)
            {
                return new { ok = false, message = e.Message }.AsJsonResult();
            }

            return new { ok = true }.AsJsonResult();
        }

        public ActionResult Download(string id, string type)
        {
            try
            {
                var comp = GetComputation(id);

                string file = "", name = "", rt = "application/zip,application/octet-stream";


                if (string.IsNullOrEmpty(type) || type.EqualOrdinalIgnoreCase("Result"))
                {
                    file = Path.Combine(comp.GetResultFolderId().GetEntityPath(), "result.zip");
                    name = string.Format("result_{0}.zip", comp.DateCreated.ToString("yyyy-M-dd_HH-mm", System.Globalization.CultureInfo.InvariantCulture));
                    //name = "result.zip";
                }
                else if (type.EqualOrdinalIgnoreCase("Config"))
                {
                    file = Path.Combine(comp.Id.GetEntityPath(), "settings.json");
                    return ActionHelper.JsonContent(System.IO.File.ReadAllText(file));
                }
                else if (type.EqualOrdinalIgnoreCase("Structures"))
                {
                    file = Path.Combine(comp.GetInputDirectory(), "input.zip");
                    name = "structures.zip";
                }
                else return HttpNotFound();

                return File(file, rt, name);
            }
            catch
            {
                return HttpNotFound();
            }
        }

        public ActionResult Source(string id, string structure, bool download = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(structure)) return HttpNotFound();
                structure = structure.Trim();

                var fn = Path.Combine(GetComputation(id).GetInputDirectory(), "input.zip");
                using (var zip = new ZipFile(fn))
                {
                    foreach (ZipEntry e in zip)
                    {
                        if (e.IsDirectory || !e.Name.StartsWith(structure, StringComparison.OrdinalIgnoreCase) || e.Name.Length <= structure.Length) continue;
                        if (e.Name[structure.Length] != '\\' && e.Name[structure.Length] != '/') continue;
                        if (!StructureReader.IsStructureFilename(e.Name)) continue;

                        using (var reader = new StreamReader(zip.GetInputStream(e)))
                        {
                            var src = reader.ReadToEnd();
                            if (download) return File(System.Text.Encoding.UTF8.GetBytes(src), "text/plain", Path.GetFileName(e.Name));
                            return ActionHelper.TextContent(src);
                        }
                    }
                }

                return HttpNotFound();
            }
            catch
            {
                return HttpNotFound();
            }
        }

        static string GetResultSize(ComputationInfo comp)
        {
            var file = Path.Combine(comp.GetResultFolderId().GetEntityPath(), "result.zip");
            if (System.IO.File.Exists(file)) return ComputationHelper.GetFileSizeString(new FileInfo(file).Length);
            return "";
        }

        public ActionResult Result(string id, string structure, string example)
        {
            if (string.IsNullOrEmpty(id)) return HttpNotFound();

            var comp = GetComputation(id);
            if (comp == null) return HttpNotFound();

            if (!string.IsNullOrEmpty(structure))
            {
                ViewBag.Id = id;
                ViewBag.Structure = structure;
                ViewBag.ResultSize = GetResultSize(comp);
                return View("Details");
            }


            ViewBag.Id = id;
            var state = comp.GetCustomState<ChangeComputationState>().State;
            if (state == ChangeComputationStates.New) return Configure(comp, example);
            return ResultAction(comp);
        }

        public ActionResult DetailsData(string id, string structure)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(structure)) return HttpNotFound();

            var comp = GetComputation(id);
            if (comp == null) return HttpNotFound();
            
            try
            {
                var ret = ZipUtils.GetEntryAsString(comp.GetResultFolderId().GetChildId("json_data.zip").GetEntityPath(), structure + ".json");
                if (ret == null) return HttpNotFound();
                return ActionHelper.JsonContent(ret);
            }
            catch
            {
                return HttpNotFound();
            }
        }

        public ActionResult Status(string id)
        {
            var comp = GetComputation(id);
            if (comp == null) return new { Exists = false, IsRunning = false }.AsJsonResult();
            return ComputationHelper.GetStatus(comp).AsJsonResult();
        }

        public ActionResult ChangeLog()
        {
            return ActionHelper.TextContent(GetChargesService().GetChangeLog());
        }

        public ActionResult DownloadService(string id)
        {
            return DownloadService(GetChargesService(), id, "ChargeCalculator");
        }

        public ActionResult __Computations(string id)
        {
            return ListComputations(id, GetAppUser(), "ChargesComputation");
        }
    }
}
