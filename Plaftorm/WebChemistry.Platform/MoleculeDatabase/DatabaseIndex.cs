// -----------------------------------------------------------------------
// <copyright file="DatabaseIndex.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace WebChemistry.Platform.MoleculeDatabase
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;
    using WebChemistry.Framework.Core;

    /// <summary>
    /// Molecule database index.
    /// </summary>
    public class DatabaseIndex
    {
        internal string DataFolder { get; private set; }
        internal DatabaseInfo Info { get; private set; }
        
        object syncRoot = new object();

        public IEnumerable<DatabaseIndexEntry> Snapshot()
        {
            return Read();
        }

        void Save(List<DatabaseIndexEntry> entries, int version)
        {
            var indexFilename = Path.Combine(Info.GetIndexFolderPath(), "index_" + version + ".xml");
            var root = new XElement("Index", 
                new XAttribute("Version", version),
                entries.OrderBy(e => e.Version).ThenBy(e => e.FilenameId).Select(e => e.ToIndexXml()));
            using (var w = XmlWriter.Create(indexFilename, new XmlWriterSettings() { Indent = true }))
            {
                root.WriteTo(w);
            }
        }

        List<DatabaseIndexEntry> Read()
        {
            var version = Info.GetStatistics().Version;

            var indexFilename = Path.Combine(Info.GetIndexFolderPath(), "index_" + version + ".xml"); ;

            if (File.Exists(indexFilename))
            {
                var xml = XElement.Load(indexFilename);
                return xml.Elements().Select(e => DatabaseIndexEntry.FromIndexXml(e, this.Info)).ToList();
            }
            else if (version == 0)
            {
                return new List<DatabaseIndexEntry>();
            }
            else
            {
                throw new InvalidOperationException(string.Format("Missing index file for DB version {0}.", version));
            }
        }

        /// <summary>
        /// Returns the number of added files.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="visitedCallback"></param>
        /// <returns></returns>
        internal DatabaseUpdateResult UpdateIndexAndDatabase(string path, Action<string> visitedCallback)
        {
            var stats = Info.GetStatistics();

            int nextVersion = stats.Version + 1;
            int updateIndex = stats.UpdateCount + 1;

            var result = DatabaseIndexer.Update(this, path, visitedCallback);

            if (!result.IsDatabaseModified) nextVersion = stats.Version;

            stats.MoleculeCount = result.AllEntries.Count;
            stats.LastUpdated = DateTimeService.GetCurrentTime();
            stats.AverageAtomCount = result.AllEntries.Count == 0 ? 0 : (int)Math.Ceiling(result.AllEntries.Average(e => e.AtomCount));
            stats.Version = nextVersion;
            stats.UpdateCount = updateIndex;
            stats.SizeInBytes = result.TotalSize;
            Info.WriteStatistics(stats);

            if (result.IsDatabaseModified) this.Save(result.AllEntries, nextVersion);

            return result.MakeResult();
        }


        /// <summary>
        /// Returns the number of updated files.
        /// </summary>
        /// <param name="visitedCallback"></param>
        /// <returns></returns>
        internal DatabaseUpdateResult UpdateIndex(Action<string> visitedCallback)
        {
            var stats = Info.GetStatistics();

            int nextVersion = stats.Version + 1;
            int updateIndex = stats.UpdateCount + 1;

            var result = DatabaseIndexer.UpdateEntryIndex(this, visitedCallback);

            if (!result.IsDatabaseModified) nextVersion = stats.Version;

            stats.MoleculeCount = result.AllEntries.Count;
            stats.LastUpdated = DateTimeService.GetCurrentTime();
            stats.AverageAtomCount = result.AllEntries.Count == 0 ? 0 : (int)Math.Ceiling(result.AllEntries.Average(e => e.AtomCount));
            stats.Version = nextVersion;
            stats.UpdateCount = updateIndex;
            stats.SizeInBytes = result.TotalSize;
            Info.WriteStatistics(stats);

            if (result.IsDatabaseModified) this.Save(result.AllEntries, nextVersion);

            return result.MakeResult();
        }
        
        internal void RebuildIndex()
        {
            throw new NotImplementedException();
        }

        internal static DatabaseIndex Open(DatabaseInfo info)
        {
            var indexFolder = info.GetIndexFolderPath();
            var dataFolder = info.GetDataFolderPath();

            var index = new DatabaseIndex
            {
                DataFolder = info.GetDataFolderPath(),
                Info = info
            };
            
            return index;
        }

        private DatabaseIndex()
        {

        }
    }
}
