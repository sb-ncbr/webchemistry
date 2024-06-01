using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Caching;
using System.Web.Mvc;
using WebChemistry.Framework.Core;
using WebChemistry.Queries.Core;
using WebChemistry.Queries.Service.App;
using WebChemistry.Platform;
using WebChemistry.Platform.Computation;
using WebChemistry.Platform.MoleculeDatabase;
using WebChemistry.Platform.MoleculeDatabase.Filtering;
using WebChemistry.Platform.Server;
using WebChemistry.Platform.Services;
using WebChemistry.Platform.Users;
using WebChemistry.Web.Filters;
using WebChemistry.Web.Helpers;

namespace WebChemistry.Web.Controllers
{
    [Compress]
    //[ReviewMode]
    //[UnderMaintenance] // cant be used with review mode
    public partial class PatternQueryController : AppControllerBase
    {
        readonly int QueryPatternCountLimit = 1000000;
        readonly long QueryAtomCountLimit = 10000000;


        #region Helpers
        public ActionResult AutoCompletionInfo()
        {
            return ActionHelper.JavaScriptContent(QueriesAutoCompletion.GetScript());
        }

        static UserInfo GetAppUser()
        {
            var user = ServerHelper.Atlas.Users.TryGetByShortId("PatternQuery");
            if (user == null)
            {
                user = ServerHelper.Atlas.Users.CreateUser("PatternQuery");
                var profile = user.GetProfile();
                profile.ConcurrentComputationLimit = 12;
                profile.QueriesPatternLimit = 1000000;
                user.SaveProfile(profile);
            }
            return user;
        }

        public ServiceInfo GetQueryService()
        {
            return ServerManager.MasterServer.Services.GetService("Queries");
        }

        public DatabaseInfo GetDatabase()
        {
            return ServerManager.MasterServer.PublicDatabases.TryGetByShortId("pdb_cif");
        }

        private ComputationInfo GetComputation(string id)
        {
            return GetAppUser().Computations.TryGetByShortId(id);
        }

        // clear in global.asax
        public static Dictionary<string, QueriesResultInterface> ActivePatternQueryResults = new Dictionary<string, QueriesResultInterface>(StringComparer.OrdinalIgnoreCase);

        static void CacheCallback(string key, object value, CacheItemRemovedReason reason)
        {
            if (value is QueriesResultInterface)
            {
                ActivePatternQueryResults.Remove(key);
                (value as QueriesResultInterface).Dispose();
            }
        }

        private QueriesResultInterface GetResultInterface(string id)
        {
            id = (id ?? "").Trim();
            var key = "MQ::" + id;
            var cache = HttpContext.Cache;

            var rs = (QueriesResultInterface)cache.Get(key);
            if (rs == null)
            {
                var comp = GetComputation(id);
                if (comp == null) return null;
                rs = new QueriesResultInterface(comp.GetResultFolderId().GetEntityPath());
                cache.Add(key, rs, null, Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(5), CacheItemPriority.Default, CacheCallback);
            }
            ActivePatternQueryResults[key] = rs;
            return rs;
        }

        #endregion

        public ActionResult Index()
        {
            var svc = GetQueryService();
            ViewBag.ServiceVersion = svc.CurrentVersion;
            ViewBag.AllVersions = svc.Versions.Keys.OrderByDescending(k => k).ToArray();
            var dbStats = GetDatabase().GetStatistics();
            ViewBag.DbLastUpdated = dbStats.LastUpdated.ToShortDateString();
            ViewBag.DbStructureCount = dbStats.MoleculeCount;

            var answer = rnd.Next(30, 60);
            HttpContext.Session["MqSupportCaptchaAnswer"] = answer;
            ViewBag.SupportToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(answer.ToString()));

            return View();
        }
        
        public ActionResult ChangeLog()
        {
            return ActionHelper.TextContent(GetQueryService().GetChangeLog());
        }

        public ActionResult DownloadService(string id)
        {
            return DownloadService(GetQueryService(), id, "PatternQuery");
        }

        public ActionResult __Computations(string id)
        {
            return ListComputations(id, GetAppUser(), "QueriesComputation");
        }
        
        public ActionResult Database(string id, string filters, int page = 0)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id)) id = "List";
                id = id.Trim();

                EntryFilter[] filtersArray = null;

                try
                {
                    if (filters != null) filtersArray = JsonHelper.FromJsonString<EntryFilter[]>(Encoding.UTF8.GetString(Convert.FromBase64String(filters)));
                }
                catch
                {
                    return new { state = "error", errors = new[] { "Invalid filters format." } }.AsJsonResult();
                }

                var data = QueriesApp.FilterDatabase(GetDatabase(), filtersArray, null);

                if (data.Errors.Length > 0)
                {
                    return new { state = "error", errors = data.Errors }.AsJsonResult();
                }

                if (id.EqualOrdinalIgnoreCase("list"))
                {
                    return ActionHelper.TextContent(data.Entries.Select(e => e.FilenameId).JoinBy("\n"));
                }

                if (id.EqualOrdinalIgnoreCase("metadata"))
                {
                    var csv = DatabaseIndexEntry.GetDefaultExporter(data.Entries).ToCsvString();
                    return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv; charset=\"utf-8\"", "dbmetadata.csv");
                }

                if (id.EqualOrdinalIgnoreCase("preview"))
                {
                    var xs = data.Entries.Take(10);
                    return new
                    {
                        state = "ok",
                        resultSize = data.Entries.Count,
                        preview = xs.Select(e => new
                        {
                            id = e.FilenameId,
                            props = e.GetPropertiesAsDictionary()
                        }).ToArray()
                    }.AsJsonResult();
                }

                int pageSize = 15;
                if (id.EqualOrdinalIgnoreCase("paged15"))
                {
                    if (data.Entries.Count == 0)
                    {
                        return new
                        {
                            state = "ok",
                            resultSize = data.Entries.Count,
                            preview = new object[0]
                        }.AsJsonResult();
                    }

                    var pageCount = (data.Entries.Count / pageSize);
                    if (data.Entries.Count % pageSize > 0) pageCount++;
                    page = page % pageCount;
                    if (page < 0) page += pageCount;
                    var offset = (page * pageSize) % data.Entries.Count;                    
                    var xs = data.Entries.Skip(offset).Take(pageSize);
                    return new
                    {
                        state = "ok",
                        resultSize = data.Entries.Count,
                        preview = xs.Select(e => new
                        {
                            id = e.FilenameId,
                            props = e.GetPropertiesAsDictionary()
                        }).ToArray()
                    }.AsJsonResult();
                }

                if (id.EqualOrdinalIgnoreCase("previewrandom15"))
                {
                    var xs = data.Entries.RandomSample(15);
                    return new
                    {
                        state = "ok",
                        resultSize = data.Entries.Count,
                        preview = xs.Select(e => new
                        {
                            id = e.FilenameId,
                            props = e.GetPropertiesAsDictionary()
                        }).OrderBy(e => e.id, StringComparer.Ordinal).ToArray()
                    }.AsJsonResult();
                }

                return HttpNotFound();
            }
            catch
            {
                return HttpNotFound();
            }
        }

        public ActionResult Submit(string config)
        {
            if (string.IsNullOrWhiteSpace(config))
            {
                return new
                {
                    state = "error",
                    errors = new[] { "Invalid config." }

                }.AsJsonResult();
            }
            try
            {
                QueriesComputationConfig cfg;
                try
                {
                    cfg = JsonHelper.FromJsonString<QueriesComputationConfig>(Encoding.UTF8.GetString(Convert.FromBase64String(config)));
                } 
                catch
                {
                    return new
                    {
                        state = "error",
                        errors = new[] { "Invalid config." }

                    }.AsJsonResult();
                }

                if (cfg.NotifyUser && (string.IsNullOrWhiteSpace(cfg.UserEmail) || cfg.UserEmail.Length > 255))
                {
                    return new
                    {
                        state = "error",
                        errors = new[] { "Invalid email." }
                    }.AsJsonResult();
                }

                UserComputationFinishedNotificationConfig notifyCfg = null;
                if (cfg.NotifyUser)
                {
                    notifyCfg = new UserComputationFinishedNotificationConfig
                    {
                        SettingsPath = "c:/webchemsupportconfig.json",
                        Email = cfg.UserEmail,
                        ServiceName = "PatternQuery",
                        ResultUrlTemplate = Url.Action("Result", "PatternQuery", new { id = "-id-" }, Request.Url.Scheme)
                    };
                }

                var comp = QueriesApp.CreateComputation(
                    UserHelper.GetUserIP(Request), GetAppUser(), GetDatabase(), ValidatorDbController.GetApp().Id,
                    cfg, QueryPatternCountLimit, QueryAtomCountLimit, 
                    notifyCfg);

                if (comp.Errors.Length > 0)
                {
                    return new
                    {
                        state = "error",
                        errors = comp.Errors
                    }.AsJsonResult();
                }

                if (comp.MissingEntries.Length > 0)
                {
                    return new
                    {
                        state = "missing_entries",
                        entries = comp.MissingEntries
                    }.AsJsonResult();
                }

                if (comp.HasEmptyInput)
                {
                    return new
                    {
                        state = "empty_input"
                    }.AsJsonResult();
                }
                                
                Schedule(comp.Computation);

                return new
                {
                    state = "ok",
                    id = comp.Computation.ShortId
                }.AsJsonResult();
            }
            catch
            {
                return new
                {
                    state = "error",
                    errors = new[] { "Ooops, something went terribly wrong. Please try again later." }

                }.AsJsonResult();
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

        public ActionResult Status(string id)
        {
            var comp = GetComputation(id);
            if (comp == null) return new { Exists = false, IsRunning = false }.AsJsonResult();
            return ComputationHelper.GetStatus(comp).AsJsonResult();
        }

        public ActionResult __Log(string id)
        {
            var comp = GetComputation(id);
            return ActionHelper.FileTextContent(Path.Combine(comp.Id.GetEntityPath(), "log.txt"));
        }

        public ActionResult Result(string id)
        {
            var comp = GetComputation(id);
            if (comp == null) return HttpNotFound();

            ViewBag.IsFinished = false;
            ViewBag.DateCreated = comp.DateCreated.ToString(System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.LongDatePattern, System.Globalization.CultureInfo.InvariantCulture) +
                ", " + comp.DateCreated.ToString(System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.LongTimePattern, System.Globalization.CultureInfo.InvariantCulture);
            var status = comp.GetStatus();
            ViewBag.FullComputationId = comp.Id.ToString();
            bool isFinished = status.State == ComputationState.Success;
            ViewBag.IsFinished = isFinished;
            ViewBag.ResultSize = ComputationHelper.GetResultSizeString(status);

            ViewBag.Id = comp.ShortId;
            ViewBag.Date = comp.DateCreated.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ViewBag.OnlineUntil = comp.DateCreated.AddMonths(1).ToLongDateString();
            return View();
        }

        public ActionResult Summary(string id)
        {
            try
            {
                var comp = GetComputation(id);
                return new
                {
                    summary = JsonConvert.DeserializeObject(QueriesApp.GetSummaryString(comp)),
                    input = JsonConvert.DeserializeObject(QueriesApp.GetInputConfigString(comp))
                }.AsJsonResult();
            }
            catch
            {
                return HttpNotFound();
            }
        }

        public ActionResult Details(string id, string query)
        {
            try
            {
                var result = GetResultInterface(id);
                if (result == null) return HttpNotFound();
                var ret = result.GetQueryDataString(query);
                if (ret == null) return HttpNotFound();
                return ActionHelper.JsonContent(ret);
            }
            catch
            {
                return HttpNotFound();
            }
        }

        public ActionResult InputDetails(string id)
        {
            try
            {
                var result = GetResultInterface(id);
                if (result == null) return HttpNotFound();                
                return ActionHelper.JsonContent(result.GetInputInfoString());
            }
            catch
            {
                return HttpNotFound();
            }
        }

        public ActionResult PatternSource(string id, string query, string pattern, string format, string type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(pattern)) return HttpNotFound();

                format = format ?? "";
                type = type ?? "";

                var result = GetResultInterface(id);
                if (result == null) return HttpNotFound();
                var src = result.GetPatternPdbSource(query, pattern);
                if (src == null) return HttpNotFound();

                if (format.EqualOrdinalIgnoreCase("mol"))
                {
                    var s = StructureReader.ReadString(pattern + ".pdb", src).Structure;
                    var ret = s.ToMolString();
                    return type.EqualOrdinalIgnoreCase("download")
                        ? File(System.Text.Encoding.UTF8.GetBytes(ret), "text/plain; charset=\"utf-8\"", pattern + ".mol")
                        : ActionHelper.TextContent(ret);
                }
                else if (format.EqualOrdinalIgnoreCase("json"))
                {
                    var s = StructureReader.ReadString(pattern + ".pdb", src).Structure;
                    var ret = s.ToJsonString();
                    return type.EqualOrdinalIgnoreCase("download")
                        ? File(System.Text.Encoding.UTF8.GetBytes(ret), "text/plain; charset=\"utf-8\"", pattern + ".json")
                        : ActionHelper.JsonContent(ret);
                }

                return type.EqualOrdinalIgnoreCase("download") 
                    ? File(System.Text.Encoding.UTF8.GetBytes(src), "text/plain; charset=\"utf-8\"", pattern + ".pdb")
                    : ActionHelper.TextContent(src);
            }
            catch
            {
                return HttpNotFound();
            }
        }
        
        public ActionResult Download(string id)
        {
            try
            {
                var comp = GetComputation(id);
                var name = string.Format("query_{0}.zip", comp.DateCreated.ToString("yyyy-M-dd_HH-mm", System.Globalization.CultureInfo.InvariantCulture));
                var path = Path.Combine(comp.GetResultFolderId().GetEntityPath(), "result.zip");
                return File(path, "application/zip,application/octet-stream", name);
            }
            catch
            {
                return HttpNotFound();
            }
        }

        [CORS]
        public ActionResult ValidateQuery(string query)
        {
            try
            {
                query = (query ?? "").Trim();
                if (query.Length == 0) return new { isOk = false, error = "Query cannot be empty."}.AsJsonResult();
                PatternQueryParser.Validate(query, BasicTypes.PatternSeq);
                return new { isOk = true }.AsJsonResult();
            }
            catch (Exception e)
            {
                return new { isOk = false, error = e.Message }.AsJsonResult();
            }
        }     
   
        public ActionResult CmdExample()
        {
            return File(Server.MapPath(Url.Content("~/Content/PatternQuery/PQ_examples.wzip")), "application/octet-stream", "PQ_cmd_example.zip");
        }

        public ActionResult PyMolVisualizationPlugin()
        {
            return File(Server.MapPath(Url.Content("~/Content/PatternQuery/PQ_pymol_plugin.py")), "text/plain", "PQ_pymol_plugin.py");
        }
    }
}
