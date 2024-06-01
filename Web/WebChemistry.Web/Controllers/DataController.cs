using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using WebChemistry.Platform;
using WebChemistry.Platform.MoleculeDatabase;
using WebChemistry.Platform.MoleculeDatabase.Filtering;
using WebChemistry.Platform.Server;
using WebChemistry.Web.Filters;
using WebChemistry.Web.Helpers;
using WebChemistry.Web.Models;
using WebChemistry.Framework.Core;

namespace WebChemistry.Web.Controllers
{
    [MustBeAuthorized]
    [Compress]
    public class DataController : Controller
    {
        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);
            ViewBag.SectionName = "Data";
        }

        public ActionResult GetAvailableViewList()
        {
            var views = DatabaseHelper.GetAvailableViews(UserHelper.GetUserInfo(HttpContext));
            return views.AsJsonResult();
        }

        //
        // GET: /Data/

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Database(string operation)
        {
            switch (operation.ToLowerInvariant())
            {
                case "create":
                    return View("CreateDatabase");
                case "public":
                    {
                        var list = ServerManager.MasterServer
                            .PublicDatabases
                            .GetAll()
                            .Select(db => Tuple.Create(db, db.GetStatistics()))
                            .OrderBy(db => db.Item1.Name)
                            .ToArray();

                        return View(new DatabaseListModel { Databases = list });
                    }
                case "list":
                default:
                    {
                        var list = UserHelper.GetUserInfo(HttpContext)
                            .Databases.GetAll()
                            .Select(db => Tuple.Create(db, db.GetStatistics()))
                            .OrderBy(db => db.Item1.Name)
                            .ToArray();

                        return View(new DatabaseListModel { Databases = list });
                    }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Database(string operation, CreateOrUpdateDatabaseModel model)
        {
            var dbs = UserHelper.GetUserInfo(HttpContext).Databases;
            model.Name = model.Name.Trim();

            if (dbs.Exists(model.Name))
            {
                ModelState.AddModelError("Name", "A database with this name already exists.");
                return View("CreateDatabase", model);
            }

            dbs.CreateDatabase(model.Name, model.Description);
            return RedirectToAction("Database", new { operation = (string)null });
        }
        
        static string GetComparisonTypeString(FilterComparisonType type)
        {
            switch (type)
            {
                case FilterComparisonType.NumberEqual: return "=";
                case FilterComparisonType.NumberLess: return "<";
                case FilterComparisonType.NumberLessEqual: return "<=";
                case FilterComparisonType.NumberGreater: return ">";
                case FilterComparisonType.NumberGreaterEqual: return ">=";
                case FilterComparisonType.StringContainsWord: return "contains";
                case FilterComparisonType.StringRegex: return "matches";
                case FilterComparisonType.StringEqual: return "=";
                default: return "";
            }
        }

        [HttpGet]
        public ActionResult DbView(string operation)
        {
            switch (operation.ToLowerInvariant())
            {
                case "delete":
                    {
                        var id = Request["id"];
                        var user = UserHelper.GetUserInfo(HttpContext);
                        var views = user.DatabaseViews;
                        views.Delete(views.Id.GetChildId(id));
                        return RedirectToAction("DbView", new { operation = (string)null });
                    }
                case "moleculelist":
                    {
                        var id = Request["id"];
                        var view = DatabaseView.Load(EntityId.Parse(id));
                        var ret = string.Join("\n", view.Snapshot().Select(e => e.FilenameId));
                        return Content(ret, "text/plain", Encoding.Default);
                    }
                case "moleculemetadata":
                    {
                        var id = Request["id"];
                        var view = DatabaseView.Load(EntityId.Parse(id));
                        var database = DatabaseInfo.Load(view.DatabaseId);
                        var index = database.GetIndex().Snapshot().ToDictionary(e => e.FilenameId, StringComparer.Ordinal);

                        var ret = DatabaseIndexEntry.GetDefaultExporter(view.Snapshot().Where(e => index.ContainsKey(e.FilenameId)).Select(e => index[e.FilenameId])).ToCsvString();
                                                
                        return File(System.Text.Encoding.UTF8.GetBytes(ret), "text/csv; charset=\"utf-8\"", database.ShortId + "_"
                            + new string(view.Name.Replace(' ', '_').Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()) + "_metadata.csv");
                        //return Content(ret, "text/plain", Encoding.UTF8);
                    }
                case "create":
                    {
                        var user = UserHelper.GetUserInfo(HttpContext);
                        var publicDbs = ServerManager.MasterServer.PublicDatabases;
                        var dbs = user.Databases;
                        var list = publicDbs.GetAll()
                            .OrderBy(db => db.Name)
                            .Select(db => Tuple.Create("Public: " + db.Name, db.Id.ToString()))
                            .Concat(dbs.GetAll().OrderBy(db => db.Name)
                                .Select(db => Tuple.Create(db.Name, db.Id.ToString())))
                            .ToArray();
                        ViewBag.Databases = list;

                        return View("CreateView", new CreateDatabaseViewModel { });
                    }
                case "list":
                default:
                    {
                        var user = UserHelper.GetUserInfo(HttpContext);
                        var dbs = user.Databases;
                        var list = user
                            .DatabaseViews.GetAll().Select(v => new { IsDefault = false, View = v })
                            .Concat(ServerManager.MasterServer.PublicDatabases.GetAll().Select(db => new { IsDefault = true, View = db.DefaultView }))
                            .Select(v => new DatabaseViewListModel.Entry {
                                IsDefault = v.IsDefault,
                                View = v.View,
                                Database = DatabaseInfo.Load(v.View.DatabaseId),
                                Stats = v.View.GetStatistics(),
                                FilterCount = v.View.Filters.Length,
                                FilterStringHtml = string.Join("; ", v.View.Filters.Select(f => string.Format("{0} {1} {2}", f.PropertyName, GetComparisonTypeString(f.ComparisonType), f.Value)))
                            })
                            .OrderBy(v => v.IsDefault ? 0 : 1)
                            .ThenBy(v => v.View.Name)
                            .ToArray();

                        return View(new DatabaseViewListModel { Entries = list });
                    }
            }
        }
                
        public ActionResult CreateView(string filters)
        {
            try
            {
                var model = JsonHelper.FromJsonString<CreateDatabaseViewModel>(filters);
                foreach (var f in model.Filters)
                {
                    var err = f.CheckError();
                    if (err != null)
                    {
                        return new { ok = false, message = "Error in '" + f.PropertyName + "': " + err }.AsJsonResult();
                    }
                }

                var user = UserHelper.GetUserInfo(HttpContext);
                var dbViews = user.DatabaseViews;
                var database = DatabaseInfo.Load(model.DatabaseId);
                dbViews.CreateView(database, model.ViewName.Trim(),
                    description: model.Description.Trim(),
                    filters: model.Filters.Length > 0 ? model.Filters : null);

                return new { ok = true }.AsJsonResult();
            }
            catch (Exception e)
            {
                return new { ok = false, message = e.Message }.AsJsonResult();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DbView(string operation, CreateDatabaseViewModel model)
        {
            return null;
            //if (string.IsNullOrEmpty(model.Database)) ModelState.AddModelError("Database", "Please select a database.");

            //if (!string.IsNullOrEmpty(model.AtomFilter))
            //{
            //    model.AtomFilter = model.AtomFilter.Trim();

            //    try
            //    {
            //        var filter = new StringFilter(model.AtomFilter);
            //    }
            //    catch (Exception e)
            //    {
            //        ModelState.AddModelError("AtomFilter", e.Message);
            //    }
            //}

            //if (!string.IsNullOrEmpty(model.RingsFilter))
            //{
            //    model.RingsFilter = model.RingsFilter.Trim();

            //    try
            //    {
            //        var filter = new StringFilter(model.RingsFilter);
            //    }
            //    catch (Exception e)
            //    {
            //        ModelState.AddModelError("RingsFilter", e.Message);
            //    }
            //}

            //if (!string.IsNullOrEmpty(model.ResidueFilter))
            //{
            //    model.ResidueFilter = model.ResidueFilter.Trim();

            //    try
            //    {
            //        var filter = new StringFilter(model.ResidueFilter);
            //    }
            //    catch (Exception e)
            //    {
            //        ModelState.AddModelError("ResidueFilter", e.Message);
            //    }
            //}

            //var user = UserHelper.GetUserInfo(HttpContext);
            //model.Name = model.Name.Trim();
            //model.Description = string.IsNullOrEmpty(model.Description) ? "" : model.Description.Trim();

            //if (ModelState.IsValid)
            //{
            //    var views = user.DatabaseViews;
            //    if (views.Exists(model.Name))
            //    {
            //        ModelState.AddModelError("Name", "A view with this name already exists.");
            //    }
            //}

            //if (!ModelState.IsValid)
            //{
            //    var publicDbs = ServerManager.MasterServer.PublicDatabases;
            //    var dbs = user.Databases;
            //    var list = publicDbs.GetAll()
            //        .OrderBy(db => db.Name)
            //        .Select(db => Tuple.Create("Public: " + db.Name, "Public:" + db.Id))
            //        .Concat(dbs.GetAll().OrderBy(db => db.Name)
            //            .Select(db => Tuple.Create(db.Name, "User:" + db.Id)))
            //        .ToArray();
            //    ViewBag.Databases = list;

            //    return View("CreateView", model);
            //}

            ////throw new NotImplementedException();

            //var filters = new List<EntryFilter>();

            ////if (model.MaxAtomCount > 0) filters.Add(EntryFilter.Create(FilterType.AtomCount, model.MaxAtomCount.ToString()));
            ////if (!string.IsNullOrWhiteSpace(model.AtomFilter)) filters.Add(EntryFilter.Create(FilterType.Atom, model.AtomFilter));
            ////if (!string.IsNullOrWhiteSpace(model.ResidueFilter)) filters.Add(EntryFilter.Create(FilterType.Residue, model.ResidueFilter));
            ////if (!string.IsNullOrWhiteSpace(model.RingsFilter)) filters.Add(EntryFilter.Create(FilterType.Ring, model.RingsFilter));

            //var dbViews = user.DatabaseViews;
            ////var database = model.Database.StartsWith("Public:")
            ////    ? MoleculeDatabaseInfo.Load(new EntityId(model.Database.Substring(7)))
            ////    : MoleculeDatabaseInfo.Load(new EntityId(model.Database.Substring(5)));
            //var dbId = EntityId.Parse(model.Database);
            //var database = DatabaseInfo.Load(dbId);
            //dbViews.CreateView(database, model.Name,
            //    description: model.Description,
            //    filters: filters.Count > 0 ? filters : null);
            
            //return RedirectToAction("DbView", new { operation = (string)null });
        }
    }
}
