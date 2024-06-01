namespace WebChemistry.Queries.Service.App
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using WebChemistry.Framework.Core;
    using WebChemistry.Queries.Service.DataModel;
    using WebChemistry.Platform;
    using WebChemistry.Platform.Computation;
    using WebChemistry.Platform.MoleculeDatabase;
    using WebChemistry.Platform.MoleculeDatabase.Filtering;
    using WebChemistry.Platform.Server;
    using WebChemistry.Platform.Users;

    /// <summary>
    /// Queries app helper.
    /// </summary>
    public static class QueriesApp
    {
        /// <summary>
        /// Get the summary json string.
        /// </summary>
        /// <param name="comp"></param>
        /// <returns></returns>
        public static string GetSummaryString(ComputationInfo comp)
        {
            return File.ReadAllText(Path.Combine(comp.GetResultFolderId().GetEntityPath(), "summary.json"));
        }

        public static string GetInputConfigString(ComputationInfo comp)
        {
            return File.ReadAllText(Path.Combine(comp.GetInputDirectory(), "uiconfig.json"));
        }

        class SnapshotCache
        {
            public int DbVersion;
            public EntityId DbId;
            public IList<DatabaseIndexEntry> Snapshot;
            public Dictionary<string, DatabaseIndexEntry> SnapshotMap;
        }

        static SnapshotCache DbSnapshotCache = null;

        /// <summary>
        /// Filters the database data.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="filters"></param>
        /// <param name="entryIds"></param>
        /// <returns></returns>
        public static QueriesDatabaseFilterResult FilterDatabase(DatabaseInfo db, EntryFilter[] filters = null, IEnumerable<string> entryIds = null)
        {            
            if (entryIds != null) entryIds = entryIds.AsList();
            bool emptyFilters = filters == null || filters.Length == 0,
                 emptyIds = entryIds == null || entryIds.Count() == 0;


            if (filters != null)
            {
                var filterErrors = filters
                    .Select(f => new { Filter = f, Error = f.CheckError() })
                    .Where(f => f.Error != null)
                    .Select(f => string.Format("Error in '{0}' filter: {1}", f.Filter.PropertyName, f.Error))
                    .ToArray();

                if (filterErrors.Length != 0)
                {
                    return new QueriesDatabaseFilterResult
                    {
                        Errors = filterErrors
                    };
                }
            }

            IList<DatabaseIndexEntry> index;
            if (DbSnapshotCache == null)
            {
                index = db.GetIndex().Snapshot().AsList();
                DbSnapshotCache = new SnapshotCache
                {
                    DbVersion = db.GetStatistics().Version,
                    DbId = db.Id,
                    Snapshot = index,
                    SnapshotMap = index.ToDictionary(e => e.FilenameId, StringComparer.OrdinalIgnoreCase)
                };
            }
            else
            {
                if (DbSnapshotCache.DbId == db.Id && DbSnapshotCache.DbVersion == db.GetStatistics().Version)
                {
                    index = DbSnapshotCache.Snapshot;
                } 
                else
                {
                    DbSnapshotCache.DbVersion = db.GetStatistics().Version;
                    DbSnapshotCache.DbId = db.Id;
                    DbSnapshotCache.Snapshot = db.GetIndex().Snapshot().AsList();
                    index = DbSnapshotCache.Snapshot;
                    DbSnapshotCache.SnapshotMap = index.ToDictionary(e => e.FilenameId, StringComparer.OrdinalIgnoreCase);
                }
            }

            if (emptyFilters && emptyIds)
            {
                return new QueriesDatabaseFilterResult
                {
                    Entries = index.AsList()
                };
            }

            if (emptyFilters)
            {
                var entrySet = entryIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var result = entrySet.Where(e => DbSnapshotCache.SnapshotMap.ContainsKey(e)).Select(e => DbSnapshotCache.SnapshotMap[e]).ToList();// index.Where(e => entrySet.Contains(e.FilenameId)).ToList();
                foreach (var e in result) present.Add(e.FilenameId);

                return new QueriesDatabaseFilterResult
                {
                    Entries = result,
                    MissingEntries = entrySet.Where(e => !present.Contains(e)).OrderBy(e => e, StringComparer.Ordinal).ToArray()
                };
            }

            if (emptyIds)
            {
                var result = index.Where(e =>
                    {
                        for (int i = 0; i < filters.Length; i++)
                        {
                            if (!filters[i].Passes(e)) return false;
                        }
                        return true;
                    }).ToList();
                
                return new QueriesDatabaseFilterResult
                {
                    Entries = result
                };
            }

            {
                var entrySet = entryIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var result = entrySet.Where(e => DbSnapshotCache.SnapshotMap.ContainsKey(e)).Select(e => DbSnapshotCache.SnapshotMap[e]).ToList(); //var result = index.Where(e => entrySet.Contains(e.FilenameId)).ToList();
                foreach (var e in result) present.Add(e.FilenameId);

                var missing = entrySet.Where(e => !present.Contains(e)).OrderBy(e => e, StringComparer.Ordinal).ToArray();

                result = result.Where(e =>
                {
                    for (int i = 0; i < filters.Length; i++)
                    {
                        if (!filters[i].Passes(e)) return false;
                    }
                    return true;
                }).ToList();

                return new QueriesDatabaseFilterResult
                {
                    Entries = result,
                    MissingEntries = missing.ToArray()
                };
            }
        }

        /// <summary>
        /// Creates a new computation.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="user"></param>
        /// <param name="db"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static QueriesCreateComputationResult CreateComputation(string source, 
            UserInfo user, DatabaseInfo db, EntityId validatorId,
            QueriesComputationConfig config,
            int patternCountLimit, long atomCountLimit,
            UserComputationFinishedNotificationConfig notifyUserConfig = null)
        {
            var data = FilterDatabase(db, config.DataSource == QueriesComputationDataSource.Filtered ? config.Filters : null, config.DataSource == QueriesComputationDataSource.List ? config.EntryList : null);

            if (data.Errors.Length > 0)
            {
                return new QueriesCreateComputationResult
                {
                    Errors = data.Errors
                };
            }

            if (data.MissingEntries.Length > 0)
            {
                return new QueriesCreateComputationResult
                {
                    MissingEntries = data.MissingEntries
                };
            }

            if (data.Entries.Count == 0)
            {
                return new QueriesCreateComputationResult
                {
                    HasEmptyInput = true
                };
            }

            try
            {
                var comp = user.Computations.CreateComputation(
                    user,
                    ServerManager.MasterServer.Services.GetService("Queries"),
                    "QueriesComputation",
                    new QueriesServiceSettings
                    {
                        Queries = config.Queries,
                        MotiveCountLimit = patternCountLimit,
                        AtomCountLimit = atomCountLimit,
                        DoValidation = config.DoValidation,
                        ValidatorId = validatorId,
                        NotifyUser =  notifyUserConfig != null,
                        NotifyUserConfig = notifyUserConfig
                    },
                    source);

                DatabaseSnapshot.Create(comp.GetInputDirectoryId().GetChildId("data"), db.Id, data.Entries);
                System.IO.File.WriteAllText(Path.Combine(comp.GetInputDirectory(), "uiconfig.json"), config.ToJsonString());

                return new QueriesCreateComputationResult
                {
                    Computation = comp
                };
            }
            catch
            {
                return new QueriesCreateComputationResult
                {
                    Errors = new string[] { "Could not create a new computation. Please try again later." }
                };
            }
        }
    }
}
