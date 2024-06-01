using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using WebChemistry.Queries.Core;
using WebChemistry.Queries.Core.Queries;
using WebChemistry.Queries.Service.App;
using WebChemistry.Queries.Service.Explorer;
using WebChemistry.Platform;
using WebChemistry.Platform.Users;
using WebChemistry.Web.Helpers;

namespace WebChemistry.Web.Controllers
{
    public partial class PatternQueryController : AppControllerBase
    {
        readonly int StructureLimit = 100;

        static UserInfo GetExplorerUser()
        {
            var user = ServerHelper.Atlas.Users.TryGetByShortId("PatternQueryExplorer");
            if (user == null) user = ServerHelper.Atlas.Users.CreateUser("PatternQueryExplorer");
            return user;
        }

        // clear in global.asax
        public static Dictionary<string, QueriesExplorerInstance> ActiveExplorerInstances = new Dictionary<string, QueriesExplorerInstance>(StringComparer.OrdinalIgnoreCase);

        static void ExplorerCacheCallback(string key, object value, CacheItemRemovedReason reason)
        {
            if (value is QueriesExplorerInstance)
            {
                ActiveExplorerInstances.Remove(key);
                (value as QueriesExplorerInstance).Dispose();
            }
        }

        QueriesExplorerInstance GetExplorerInstance(string id)
        {
            id = (id ?? "").Trim();

            var key = "MQE::" + id;
            var cache = HttpContext.Cache;

            var exp = (QueriesExplorerInstance)cache.Get(key);
            if (exp == null)
            {
                var user = GetExplorerUser();
                var eid = user.Repository.GetChildEntityId(id);
                exp = QueriesExplorerInstance.TryRead(eid);

                if (exp == null) return null;
                cache.Add(key, exp, null, Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(5), CacheItemPriority.Default, ExplorerCacheCallback);
            }

            ActiveExplorerInstances[key] = exp;

            return exp;
        }

        public ActionResult ExplorerCreate(string id)
        {
            try
            {
                string name = id;
                if (string.IsNullOrWhiteSpace(name)) name = "Unnamed Session";
                name = name.Trim();
                if (name.Length > 25) name = name.Substring(0, 25);

                var user = GetExplorerUser();
                var guid = Guid.NewGuid().ToString();
                var eid = user.Repository.GetChildEntityId(guid);
                var inst = QueriesExplorerInstance.Create(eid, name, UserHelper.GetUserIP(Request), structureLimit: StructureLimit);
                return new { id = guid }.AsJsonResult();
            }
            catch (Exception e)
            {
                return new { error = true, message = e.Message }.AsJsonResult();
            }
        }

        public ActionResult Explorer(string id)
        {
            id = (id ?? "").Trim();

            try
            {
                var inst = GetExplorerInstance(id);
                if (inst == null) return HttpNotFound();
                ViewBag.Name = inst.Name;
                ViewBag.OnlineUntil = inst.DateCreated.AddMonths(1).ToLongDateString();
            }
            catch
            {
                return HttpNotFound();
            }

            ViewBag.UseContainer = false;
            ViewBag.Id = id;
            return View();
        }

        public ActionResult ExplorerUpload(string id)
        {
            try
            {
                var inst = GetExplorerInstance(id);
                if (inst == null) return new { error = true, message = "instance not found." }.AsJsonResult();

                List<AddStructureEntry> xs = new List<AddStructureEntry>();

                foreach (string name in Request.Files)
                {
                    var f = Request.Files.Get(name);
                    var stream = new MemoryStream();
                    f.InputStream.CopyTo(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    xs.Add(new AddStructureEntry
                    {
                        Filename = f.FileName,
                        Provider = () => stream
                    });
                }

                return inst.AddStructures(xs).AsJsonResult();
            }
            catch (Exception e)
            {
                return new { error = true, message = e.Message }.AsJsonResult();
            }
        }

        public ActionResult ExplorerAddFromPdb(string id, string structures)
        {
            try
            {
                var inst = GetExplorerInstance(id);
                if (inst == null) return new { error = true, errorType = "generic", message = "App instance not found." }.AsJsonResult();                
                var ids = JsonHelper.FromJsonString<string[]>(structures);

                if (ids.Length > StructureLimit) return new { error = true, errorType = "generic", message = "Trying to upload too many structures at the same time. The limit is " + StructureLimit + "." }.AsJsonResult();                

                var entries = QueriesApp.FilterDatabase(GetDatabase(), null, ids);

                if (entries.Errors.Length > 0) return new { error = true, errorType = "db", messages = entries.Errors }.AsJsonResult();
                if (entries.MissingEntries.Length > 0) return new { error = true, errorType = "missing", entries = entries.MissingEntries }.AsJsonResult();

                var xs = entries.Entries.Select(e => new AddStructureEntry { Filename = e.FilenameId + e.Extension, Provider = () => e.GetSourceStream() }).ToArray();
                return inst.AddStructures(xs).AsJsonResult();
            }
            catch (Exception e)
            {
                return new { error = true, errorType = "generic", message = "Failed. Please try again later." }.AsJsonResult();
            }
        }

        public ActionResult ExplorerState(string id)
        {
            try
            {
                var inst = GetExplorerInstance(id);
                if (inst == null) return HttpNotFound();                
                return inst.GetState().AsJsonResult();
            }
            catch
            {
                return HttpNotFound();
            }
        }
        // "application/zip,application/octet-stream"

        public ActionResult ExplorerDownloadResult(string id)
        {
            try
            {
                id = (id ?? "").Trim();
                var fn = Path.Combine(GetExplorerUser().Repository.GetChildEntityId(id).GetEntityPath(), "result.zip");
                if (!System.IO.File.Exists(fn)) return HttpNotFound();
                var name = string.Format("query_{0}.zip", DateTime.UtcNow.ToString("yyyy-M-dd_HH-mm", System.Globalization.CultureInfo.InvariantCulture));
                return File(fn, "application/zip,application/octet-stream", name);
            }
            catch
            {
                return HttpNotFound();
            }
        }

        public ActionResult ExplorerLog(string id)
        {
            try
            {
                id = (id ?? "").Trim();
                var eid = GetExplorerUser().Repository.GetChildEntityId(id);
                using (var file = System.IO.File.Open(Path.Combine(eid.GetEntityPath(), "log.txt"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(file))
                {
                    return ActionHelper.TextContent(reader.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                return HttpNotFound(e.Message);
            }
        }

        [WebChemistry.Web.Filters.ActionTimeOutFilter]
        public ActionResult ExplorerQuery(string id, string queryString)
        {
            try
            {
                queryString = queryString ?? "";
                queryString = queryString.Trim(); //Encoding.UTF8.GetString(Convert.FromBase64String(queryString)).Trim();
                var compiled = PatternQueryParser.Parse(queryString, BasicTypes.PatternSeq) as QueryMotive;
                var inst = GetExplorerInstance(id);
                if (inst == null) return HttpNotFound();
                return inst.Query(queryString, compiled).AsJsonResult();
            }
            catch (Exception e)
            {
                return new { error = true, message = e.Message }.AsJsonResult();
            }
        }

        public ActionResult ExplorerRemoveStructures(string id, string structures)
        {
            if (string.IsNullOrWhiteSpace(structures)) return new { error = true, message = "No id specified." }.AsJsonResult();
            try
            {
                var inst = GetExplorerInstance(id);
                if (inst == null) return new { error = true, message = "App instance not found." }.AsJsonResult();
                var ids = JsonHelper.FromJsonString<string[]>(structures);
                return inst.RemoveStructures(ids).AsJsonResult();
            }
            catch (Exception e)
            {
                return new { error = true, message = e.Message }.AsJsonResult();
            }
        }

        public ActionResult ExplorerSetStructures(string id, string structures)
        {
            try
            {
                var inst = GetExplorerInstance(id);
                if (inst == null) return new { error = true, errorType = "generic", message = "App instance not found." }.AsJsonResult();
                var ids = JsonHelper.FromJsonString<string[]>(structures);

                if (ids.Length > StructureLimit) return new { error = true, errorType = "generic", message = "Trying to upload too many structures at the same time. The limit is " + StructureLimit + "." }.AsJsonResult();

                inst.RemoveAllStructures();

                var entries = QueriesApp.FilterDatabase(GetDatabase(), null, ids);

                if (entries.Errors.Length > 0) return new { error = true, errorType = "db", messages = entries.Errors }.AsJsonResult();
                if (entries.MissingEntries.Length > 0) return new { error = true, errorType = "missing", entries = entries.MissingEntries }.AsJsonResult();

                var xs = entries.Entries.Select(e => new AddStructureEntry { Filename = e.FilenameId + e.Extension, Provider = () => e.GetSourceStream() }).ToArray();
                return inst.AddStructures(xs).AsJsonResult();
            }
            catch (Exception e)
            {
                return new { error = true, errorType = "generic", message = "Failed. Please try again later." }.AsJsonResult();
            }
        }
    }
}
