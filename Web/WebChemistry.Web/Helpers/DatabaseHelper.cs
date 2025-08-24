using System;
using System.Collections.Generic;
using System.Linq;
using WebChemistry.Platform.MoleculeDatabase;
using WebChemistry.Platform.Users;
using WebChemistry.Web.Models;
using WebChemistry.Framework.Core;
using WebChemistry.Platform;
using WebChemistry.Platform.Server;

namespace WebChemistry.Web.Helpers
{
    public static class DatabaseHelper
    {
        static ViewListEntryModel GetViewModel(DatabaseView view, string dbName, bool isPublic, bool isDefault)
        {
            return new ViewListEntryModel
            {
                Id = view.Id.ToString(),
                IsPublic = isPublic,
                IsDefault = isDefault,
                Name = view.Name,
                DatabaseName = dbName,
                Description = view.Description,
                ShortDescription = new string(view.Description.TakeWhile(c => c != '\n' && c != '\r').ToArray()),
                Size = view.GetStatistics().MoleculeCount
            };
        }

        public static IEnumerable<ViewListEntryModel> GetAvailableViews(UserInfo user)
        {
            Func<EntityId, string> dbName = new Func<EntityId, string>(id => DatabaseInfo.Load(id).Name).Memoize();


            var publicViews = ServerManager.MasterServer.PublicDatabases.GetAll()
                .Select(db => GetViewModel(db.DefaultView, db.Name, true, true));

            var userDefaultViews = user.Databases.GetAll()
                .Select(db => GetViewModel(db.DefaultView, db.Name, false, true));

            var userViews = user.DatabaseViews.GetAll().Select(v => GetViewModel(v, dbName(v.DatabaseId), false, false));

            return publicViews.Concat(userDefaultViews).Concat(userViews).ToArray();
        }

        //public static ViewListEntryModel[] GetDatabaseViewModels(IEnumerable<EntityId> ids)
        //{
        //    Func<EntityId, string> dbName = new Func<EntityId, string>(id => DatabaseInfo.Load(id).Name).Memoize();

        //    return ids
        //        .Select(id => DatabaseView.Load(id))
        //        .Select(v => GetViewModel(v, dbName(v.DatabaseId), true))
        //        .ToArray();
        //}
    }
}