// -----------------------------------------------------------------------
// <copyright file="DatabaseInfo.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace WebChemistry.Platform.MoleculeDatabase
{
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.IO;
    using WebChemistry.Platform.MoleculeDatabase.Filtering;

    /// <summary>
    /// DB Statistics.
    /// </summary>
    public class DatabaseStatistics
    {
        /// <summary>
        /// Last update date.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Each update increments the version.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// How many times was the database updated.
        /// This is used to keep the log file up to date.
        /// </summary>
        public int UpdateCount { get; set; }

        /// <summary>
        /// Number of molecules in the database.
        /// </summary>
        public int MoleculeCount { get; set; }

        /// <summary>
        /// Size of the molecule data (not including indices) in bytes.
        /// </summary>
        public long SizeInBytes { get; set; }

        /// <summary>
        /// Average number of atoms per structure.
        /// </summary>
        public int AverageAtomCount { get; set; }
    }
    
    /// <summary>
    /// Result of database update procedure.
    /// </summary>
    public class DatabaseUpdateResult
    {
        /// <summary>
        /// Was the DB modified.
        /// </summary>
        public bool IsModified { get; set; }

        /// <summary>
        /// Number of added structures.
        /// </summary>
        public int NumAdded { get; set; }

        /// <summary>
        /// Number of removed structures.
        /// </summary>
        public int NumRemoved { get; set; }

        /// <summary>
        /// Number of modified structures.
        /// </summary>
        public int NumModified { get; set; }

        /// <summary>
        /// Number of updated entries.
        /// </summary>
        public int NumUpdated { get; set; }

        /// <summary>
        /// Number of errors.
        /// </summary>
        public int NumError { get; set; }
    }

    /// <summary>
    /// Database info.
    /// </summary>
    public class DatabaseInfo : ManagedPersistentObjectBase<DatabaseInfo, DatabaseInfo.Index, DatabaseInfo.Update>
    {
        /// <summary>
        /// Index entry.
        /// </summary>
        public class Index
        {
            public string Name { get; set; }
        }

        /// <summary>
        /// Update model.
        /// </summary>
        public class Update
        {
            public string Name { get; set; }
            public string Description { get; set; }
        }

        class LockInfo
        {
            public bool IsLocked { get; set; }
        }

        class ChildViews
        {
            List<EntityId> views;
            public List<EntityId> Views
            {
                get { return (views = views ?? new List<EntityId>()); }
                set
                {
                    views = value;
                }
            }
        }

        //string directory;

        internal string GetDataFolderPath() { return Path.Combine(CurrentDirectory, "data"); }
        internal string GetLockPath() { return Path.Combine(CurrentDirectory, "lock.json"); }
        internal string GetStatisticsPath() { return Path.Combine(CurrentDirectory, "stats.json"); }
        internal string GetIndexFolderPath() { return Path.Combine(CurrentDirectory, "index"); }

        /// <summary>
        /// Filters the database and returns the resulting list.
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        public IEnumerable<DatabaseEntry> Filter(EntryFilter[] filters)
        {
            IEnumerable<DatabaseIndexEntry> entries = GetIndex().Snapshot();
            foreach (var f in filters)
            {
                var capture = f;
                entries = entries.Where(e => capture.Passes(e));
            }
            return entries.ToList();
        }
        
        /// <summary>
        /// Default database view.
        /// </summary>
        [JsonIgnore]
        public DatabaseView DefaultView 
        {
            get
            {
                return DatabaseView.Load(Id.GetChildId(DatabaseView.DefaultViewId));
            }
        }
        
        /// <summary>
        /// Database name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Index entry.
        /// </summary>
        internal override DatabaseInfo.Index IndexEntry
        {
            get { return new Index { Name = Name }; }
        }

        /// <summary>
        /// Database description.
        /// </summary>
        public string Description { get; set; }

        internal override void UpdateAndSaveInternal(DatabaseInfo.Update model)
        {
            this.Name = model.Name ?? this.Name;
            this.Description = model.Description ?? this.Description;
        }
                        
        /// <summary>
        /// Read the database statistics.
        /// </summary>
        /// <returns></returns>
        public DatabaseStatistics GetStatistics()
        {
            return JsonHelper.ReadJsonFile<DatabaseStatistics>(GetStatisticsPath());
        }

        internal void WriteStatistics(DatabaseStatistics stats)
        {
            JsonHelper.WriteJsonFile(GetStatisticsPath(), stats);
        }

        /// <summary>
        /// Check if the database is locked (ie. updating etc).
        /// </summary>
        /// <returns></returns>
        public bool IsLocked()
        {
            var fn = GetLockPath();
            if (!File.Exists(fn)) return false;
            return JsonConvert.DeserializeObject<LockInfo>(File.ReadAllText(GetLockPath())).IsLocked;
        }

        void SetLocked(bool locked)
        {
            File.WriteAllText(GetLockPath(), JsonConvert.SerializeObject(new LockInfo { IsLocked = locked }));
        }

        /// <summary>
        /// Read the database index.
        /// </summary>
        /// <returns></returns>
        public DatabaseIndex GetIndex()
        {
            return DatabaseIndex.Open(this);
        }
        
        /// <summary>
        /// Update the database and rebuild index from a given folder or zip file.
        /// Zip files in the folder are automatically explored.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="visitedCallback"></param>
        /// <returns>The number of added structures.</returns>
        public DatabaseUpdateResult UpdateDatabase(string path, Action<string> visitedCallback = null)
        {
            if (IsLocked()) throw new InvalidOperationException("There is currently another update in progress.");
            SetLocked(true);

            if (visitedCallback == null) visitedCallback = _ => { };

            DatabaseUpdateResult result;
            try
            {
                var index = GetIndex();
                result = index.UpdateIndexAndDatabase(path, visitedCallback);
            }
            finally
            {
                SetLocked(false);
            }
            return result;
        }

        /// <summary>
        /// Updates the database index.
        /// </summary>
        /// <param name="visitedCallback"></param>
        /// <returns>The number of added structures.</returns>
        public DatabaseUpdateResult UpdateDatabaseIndex(Action<string> visitedCallback = null)
        {
            if (IsLocked()) throw new InvalidOperationException("There is currently another update in progress.");
            SetLocked(true);

            if (visitedCallback == null) visitedCallback = _ => { };

            DatabaseUpdateResult result;
            try
            {
                var index = GetIndex();
                result = index.UpdateIndex(visitedCallback);
            }
            finally
            {
                SetLocked(false);
            }
            return result;
        }
                        
        /// <summary>
        /// Creates a new database.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="description"></param>
        /// <param name="customId"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static DatabaseInfo Create(DatabaseManager manager, string name, string customId = null, string description = null)
        {
            var db = CreateAndSave(manager.Id, string.IsNullOrEmpty(customId) ? Guid.NewGuid().ToString() : customId, newDb =>
            {
                newDb.Name = name;
                newDb.Description = description;
            });            
            Directory.CreateDirectory(db.GetDataFolderPath());
            Directory.CreateDirectory(db.GetIndexFolderPath());

            DatabaseView.CreateDefault(db);
            db.WriteStatistics(new DatabaseStatistics { LastUpdated = db.DateCreated });
            return db;
        }
    }
}
