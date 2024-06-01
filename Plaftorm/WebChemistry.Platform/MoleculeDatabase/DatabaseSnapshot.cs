namespace WebChemistry.Platform.MoleculeDatabase
{
    using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using WebChemistry.Framework.Core;

    /// <summary>
    /// Snapshot of structures given by multiple views.
    /// </summary>
    public class DatabaseSnapshot : PersistentObjectBase<DatabaseSnapshot>
    {
        /// <summary>
        /// Snapshot info.
        /// </summary>
        public class ViewSnapshotInfo
        {
            /// <summary>
            /// This can be used if the view still exists.
            /// </summary>
            public EntityId Id { get; set; }

            /// <summary>
            /// Database id.
            /// </summary>
            public EntityId DatabaseId { get; set; }

            /// <summary>
            /// Name of the view.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// View description.
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// Number of molecules in the view.
            /// </summary>
            public int MoleculeCount { get; set; }
        }

        /// <summary>
        /// Database info.
        /// </summary>
        public class DatabaseSnapshotInfo
        {
            /// <summary>
            /// Database id.
            /// </summary>
            public EntityId Id { get; set; }

            /// <summary>
            /// Molecule count.
            /// </summary>
            public int MoleculeCount { get; set; }
        }

        /// <summary>
        /// Check if all the underlaying databases still exist.
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            return Databases.All(db => DatabaseInfo.Exists(db.Id));
        }

        /// <summary>
        /// Views info snapshots.
        /// </summary>
        public ViewSnapshotInfo[] Views { get; set; }

        /// <summary>
        /// Underlaying databases.
        /// </summary>
        public DatabaseSnapshotInfo[] Databases { get; set; }

        /// <summary>
        /// Total number of molecules.
        /// </summary>
        public int TotalMoleculeCount { get; set; }

        /// <summary>
        /// Get the snapshot entries.
        /// </summary>
        /// <returns></returns>
        public IList<DatabaseEntry> Snapshot()
        {
            if (!IsValid()) throw new InvalidOperationException("One or more of the underlaying databases no longer exist.");

            //var dbProvider = new Func<EntityId, DatabaseInfo>(id => DatabaseInfo.Load(id)).Memoize();

            var snapshotFilename = Id.GetChildId("snapshot.xml").GetEntityPath();
            var xml = XElement.Load(snapshotFilename);

            var ret = xml.Elements("Database")
                .SelectMany(x =>
                {
                    var db = DatabaseInfo.Load(EntityId.Parse(x.Attribute("Id").Value));
                    return x.Elements().Select(e => DatabaseEntry.FromEntryXml(e, db));
                })
                .ToArray();

            return ret;
        }

        /// <summary>
        /// Creates the snapshot from entries.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="databaseId"></param>
        /// <param name="entries"></param>
        /// <returns></returns>
        public static DatabaseSnapshot Create(EntityId id, EntityId databaseId, IEnumerable<DatabaseEntry> entries)
        {
            entries = entries.AsList();

            var xml = new XElement("Snapshot",
                    new XElement("Database",
                        new XAttribute("Id", databaseId),
                        entries.Select(e => e.ToEntryXml())));

            var snapshot = CreateAndSave(id, s =>
            {
                s.Views = new ViewSnapshotInfo[0];
                s.Databases = new[] 
                { 
                    new DatabaseSnapshotInfo
                    {
                        Id = databaseId,
                        MoleculeCount = entries.Count()
                    }
                };
                s.TotalMoleculeCount = entries.Count();
            });

            var snapshotFilename = id.GetChildId("snapshot.xml").GetEntityPath();
            using (var w = XmlWriter.Create(snapshotFilename, new XmlWriterSettings() { Indent = true }))
            {
                xml.WriteTo(w);
            }

            return snapshot;
        }

        /// <summary>
        /// Creates the snapshot.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="views"></param>
        /// <returns></returns>
        public static DatabaseSnapshot Create(EntityId id, IList<DatabaseView> views)
        {
            var snapshots = views.GroupBy(v => v.DatabaseId)
                .Select(g => new
                {
                    DatabaseId =g.Key,
                    Entries = g.SelectMany(v => v.Snapshot()).Distinct(e => e.FilenameId, StringComparer.OrdinalIgnoreCase).ToArray()
                })
                .ToArray();

            var xml = new XElement("Snapshot", 
                snapshots.Select(s =>
                    new XElement("Database", 
                        new XAttribute("Id", s.DatabaseId),
                        s.Entries.Select(e => e.ToEntryXml()))));
            
            //var databases = views.Select(v => v.DatabaseId).Distinct().ToArray();
            var viewsInfo = views.Select(v => new ViewSnapshotInfo
                {
                    Id = v.Id,
                    DatabaseId = v.DatabaseId,
                    Name = v.Name,
                    Description = v.Description,
                    MoleculeCount = v.GetStatistics().MoleculeCount
                })
                .ToArray();

            var snapshot = CreateAndSave(id, s =>
                {
                    s.Views = viewsInfo;
                    s.Databases = snapshots.Select(x => new DatabaseSnapshotInfo 
                    { 
                        Id = x.DatabaseId, 
                        MoleculeCount = x.Entries.Length 
                    }).ToArray();
                    s.TotalMoleculeCount = snapshots.Sum(x => x.Entries.Length);
                });

            var snapshotFilename = id.GetChildId("snapshot.xml").GetEntityPath();
            using (var w = XmlWriter.Create(snapshotFilename, new XmlWriterSettings() { Indent = true }))
            {
                xml.WriteTo(w);
            }

            return snapshot;
        }
    }
}
