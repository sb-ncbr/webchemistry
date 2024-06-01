namespace WebChemistry.Platform.MoleculeDatabase
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using WebChemistry.Platform.MoleculeDatabase.Filtering;
    
    /// <summary>
    /// View statistics.
    /// </summary>
    public class DatabaseViewStatistics
    {
        /// <summary>
        /// Each update increments the version.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Number of molecules in the database.
        /// </summary>
        public int MoleculeCount { get; set; }
        
        /// <summary>
        /// Average number of atoms per structure.
        /// </summary>
        public int AverageAtomCount { get; set; }
    }

    /// <summary>
    /// Filtered database view.
    /// </summary>
    public class DatabaseView : ManagedPersistentObjectBase<DatabaseView, DatabaseView.Index, DatabaseView.Update>
    {
        internal const string DefaultViewId = "view";

        /// <summary>
        /// Index entry.
        /// </summary>
        public class Index
        {
            public string Name { get; set; }
            public EntityId DatabaseId { get; set; }
        }

        public class Update
        {
            public string Name { get; set; }
            public string Description { get; set; }
            //public bool IncludeObsolete { get; set; }
            public EntryFilter[] Filters { get; set; }
        }

        string GetStatisticsPath() { return Path.Combine(CurrentDirectory, "stats.json"); }
        string GetViewPath() { return Path.Combine(CurrentDirectory, "view.xml"); }
        
        /// <summary>
        /// Path of the parent database.
        /// </summary>
        public EntityId DatabaseId { get; set; }
                
        /// <summary>
        /// Name of the view.
        /// </summary>
        public string Name { get; set; }

        ///// <summary>
        ///// Include obsolete. Default = false.
        ///// </summary>
        //public bool IncludeObsolete { get; set; }

        /// <summary>
        /// Index entry.
        /// </summary>
        internal override DatabaseView.Index IndexEntry
        {
            get { return new Index { Name = Name, DatabaseId = DatabaseId }; }
        }

        /// <summary>
        /// View description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Filter array.
        /// </summary>
        public EntryFilter[] Filters { get; set; }

        internal override void UpdateAndSaveInternal(DatabaseView.Update model)
        {
            this.Name = model.Name ?? this.Name;
            this.Description = model.Description ?? this.Description;

            bool changed = false;

            ////if (this.IncludeObsolete != model.IncludeObsolete)
            ////{
            ////    this.IncludeObsolete = model.IncludeObsolete;                
            ////    changed = true;
            ////}

            if (model.Filters == null) model.Filters = new EntryFilter[0];

            model.Filters.ForEach(f => f.CheckValid());

            var filtersUpdated = this.Filters.Union(model.Filters).Count() != this.Filters.Count();
            if (filtersUpdated)
            {
                this.Filters = model.Filters.OrderBy(f => f.PropertyName).ToArray();
                changed = true;
            }

            if (changed) SaveStatistics(new DatabaseViewStatistics { });
        }

        /// <summary>
        /// Get the statistics.
        /// </summary>
        /// <returns></returns>
        public DatabaseViewStatistics GetStatistics()
        {
            DatabaseViewStatistics stats;
            UpdateView(DatabaseInfo.Load(DatabaseId), out stats);
            return stats;
        }
        
        /// <summary>
        /// Enumerate...
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DatabaseEntry> Snapshot()
        {
            var db = DatabaseInfo.Load(DatabaseId);
            DatabaseViewStatistics stats;

            UpdateView(db, out stats);
            if (stats.Version == 0) return new DatabaseEntry[0];

            return GetSnapshotXml()
                .Elements()
                .Select(e => DatabaseIndexEntry.FromEntryXml(e, db))
                .ToList();
        }

        void SaveStatistics(DatabaseViewStatistics stats)
        {
            JsonHelper.WriteJsonFile(GetStatisticsPath(), stats);
        }

        XElement GetSnapshotXml()
        {
            var fn = GetViewPath();;
            if (File.Exists(fn)) return XElement.Load(GetViewPath());
            return new XElement("View");
        }

        bool UpdateView(DatabaseInfo db, out DatabaseViewStatistics stats)
        {
            stats = JsonHelper.ReadJsonFile<DatabaseViewStatistics>(GetStatisticsPath());
            var dbVersion = db.GetStatistics().Version;
            if (stats.Version == dbVersion) return false;

            var localStats = stats;
            IEnumerable<DatabaseIndexEntry> entries = db.GetIndex().Snapshot();

            ////if (!IncludeObsolete)
            ////{
            ////    entries = entries.Where(e => !e.IsObsolete);
            ////}

            if (Filters != null)
            {
                foreach (var f in Filters)
                {
                    var capture = f;
                    entries = entries.Where(e => capture.Passes(e));
                }
            }

            var entryList = entries.ToList();

            var xml = new XElement("View", entryList.Select(e => e.ToEntryXml()));
            using (var w = XmlWriter.Create(GetViewPath(), new XmlWriterSettings() { Indent = true }))
            {
                xml.WriteTo(w);
            }

            stats.Version = dbVersion;
            stats.MoleculeCount = entryList.Count;
            stats.AverageAtomCount = entryList.Count > 0 ? (int)Math.Ceiling(entryList.Average(e => e.AtomCount)) : 0;
            SaveStatistics(stats);

            return true;
        }

        protected override void OnObjectLoaded()
        {
            if (!DatabaseInfo.Exists(DatabaseId))
            {
                throw new InvalidDataException("The underlying database for this view no longer exists. The view will be automatically deleted later.");
            }
        }

        /// <summary>
        /// Filters the view using additional filters and returns the resulting list.
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        public IEnumerable<DatabaseEntry> Filter(EntryFilter[] filters)
        {
            var db = DatabaseInfo.Load(DatabaseId);            
            IEnumerable<DatabaseIndexEntry> entries = db.GetIndex().Snapshot();
            if (Filters != null)
            {
                foreach (var f in Filters)
                {
                    var capture = f;
                    entries = entries.Where(e => capture.Passes(e));
                }
            }
            foreach (var f in filters)
            {
                var capture = f;
                entries = entries.Where(e => capture.Passes(e));
            }
            return entries.ToList();
        }

        /// <summary>
        /// Create the view.
        /// </summary>
        /// <param name="database"></param>
        /// <param name="manager"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="filters"></param>
        /// <returns></returns>
        internal static DatabaseView Create(DatabaseInfo database, DatabaseViewManager manager, string name, 
            string description = "", /*bool includeObsolete = false,*/ IEnumerable<EntryFilter> filters = null)
        {
            if (database.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Custom views cannot have the same name as the underlying database.");

            // validate filters

            var fltrs = filters == null ? new EntryFilter[0] : filters.OrderBy(f => f.PropertyName).ToArray();
            
            var view = CreateAndSave(manager.Id, Guid.NewGuid().ToString(), newView =>
            {
                newView.DatabaseId = database.Id;
                newView.Name = name;
                newView.Description = description ?? "";
                newView.Filters = fltrs;
                //newView.IncludeObsolete = false;
            });
            view.Save();
            view.SaveStatistics(new DatabaseViewStatistics { });
            
            return view;
        }

        /// <summary>
        /// Creates a default view for a database.
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        internal static DatabaseView CreateDefault(DatabaseInfo database)
        {
            var view = CreateAndSave(database.Id.GetChildId(DefaultViewId), newView =>
            {
                newView.DatabaseId = database.Id;
                newView.Name = database.Name;
                newView.Description = "Entire database.";
                newView.Filters = new EntryFilter[0];
            });
            view.Save();
            view.SaveStatistics(new DatabaseViewStatistics { });

            return view;
        }

        /// <summary>
        /// Creates a default view for a database.
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static DatabaseView CreateCustom(DatabaseInfo database, EntityId entityId, string name,
            string description = "", /*bool includeObsolete = false, */IEnumerable<EntryFilter> filters = null)
        {
            if (database.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Custom views cannot have the same name as the underlying database.");

            // validate filters

            var fltrs = filters == null ? new EntryFilter[0] : filters.OrderBy(f => f.PropertyName).ToArray();

            var view = CreateAndSave(entityId, newView =>
            {
                newView.DatabaseId = database.Id;
                newView.Name = name;
                newView.Description = description ?? "";
                newView.Filters = fltrs;
                //newView.IncludeObsolete = false;
            });
            view.Save();
            view.SaveStatistics(new DatabaseViewStatistics { });

            return view;
        }
    }
}
