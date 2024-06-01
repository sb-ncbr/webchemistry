// -----------------------------------------------------------------------
// <copyright file="DatabaseIndex.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace WebChemistry.Platform.MoleculeDatabase
{
    using ICSharpCode.SharpZipLib.Zip;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using WebChemistry.Framework.Core;

    /// <summary>
    /// Molecule database index.
    /// </summary>
    class DatabaseIndexer
    {
        enum EntryType
        {
            New,
            Modified,
            AlreadyPresent,
            Removed,
            Updated,
            Error
        }

        class IndexerEntry
        {
            public EntryType Type { get; set; }

            public string Filename { get; set; }
            public string ErrorMessage { get; set; }
            public DatabaseIndexEntry Entry { get; set; }
            public TimeSpan Timing { get; set; }

            public IndexerEntry()
            {
                ErrorMessage = "";
            }
        }

        string DataFolder;
        DatabaseInfo Info;
        DatabaseStatistics Stats;

        Dictionary<string, DatabaseIndexEntry> PreviouslyIndexed;
        int IndexUpdateVersion;

        public readonly List<DatabaseIndexEntry> AllEntries = new List<DatabaseIndexEntry>();
        public long TotalSize { get; private set; }
        public bool IsDatabaseModified { get; private set; }

        Action<string> VisitedCallback;

        /// <summary>
        /// added / size.
        /// </summary>
        BlockingCollection<IndexerEntry> Entries;
        TextWriter Log, UpdateIndex;

        int NumAdded, NumModified, NumRemoved, NumUpdated, NumError;

        void Process()
        {
            //IndexerEntry entry;
            while (true)
            {
                try
                {
                    var entry = Entries.Take();
                    try
                    {
                        if (entry.Type != EntryType.Error)
                        {
                            if (entry.Type == EntryType.New) NumAdded++;
                            else if (entry.Type == EntryType.Modified) NumModified++;
                            else if (entry.Type == EntryType.Removed) NumRemoved++;
                            else if (entry.Type == EntryType.Updated) NumUpdated++;

                            if (entry.Type != EntryType.Removed)
                            {
                                AllEntries.Add(entry.Entry);
                                TotalSize += entry.Entry.SizeInBytes;
                            }
                        }
                        else
                        {
                            NumError++;
                        }
                        UpdateIndex.WriteLine("\"{0}\",\"{1}\",\"{2}\",{3}", entry.Filename, entry.Type.ToString(), entry.ErrorMessage.Replace("\"", "\"\""), entry.Timing.TotalMilliseconds.ToStringInvariant("0"));
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("Unexpected error: {0}", e.Message);
                    }

                    try
                    {
                        VisitedCallback(entry.Filename);
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("Error handling callback: {0}", e.Message);
                    }
                }
                catch
                {
                    break;
                }
            }            
        }
        
        void CopyLocal(string filename, Func<Stream> streamProvider)
        {
            using (var stream = streamProvider())
            using (var target = File.OpenWrite(filename))
            {
                stream.CopyTo(target);
            }
        }

        bool IsModified(FileInfo newFile, DatabaseIndexEntry entry)
        {
            var localFile = new FileInfo(Path.Combine(DataFolder, newFile.Name));
            if (!localFile.Exists) return true;
            
            if (newFile.Length == localFile.Length)
            {
                var lastModified = entry.GetSourceTimestamp();
                if (lastModified.HasValue)
                {
                    return lastModified.Value != newFile.LastWriteTimeUtc.Ticks;
                }

                using (var f1 = newFile.OpenRead())
                using (var f2 = localFile.OpenRead())
                {
                    int bufferSize = 2 * 4096;
                    byte[] b1 = new byte[bufferSize], b2 = new byte[bufferSize];
                    while (true)
                    {
                        var read1 = f1.Read(b1, 0, bufferSize);
                        if (f1.Read(b1, 0, bufferSize) <= 0) break;
                        int read2 = f2.Read(b2, 0, bufferSize);
                        if (read1 != read2) return true;
                        for (int i = 0; i < read1; i++)
                        {
                            if (b1[i] != b2[i]) return true;
                        }
                    }
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        void VisitFile(string filename, Func<Stream> streamProvider)
        {
            Stopwatch timer = Stopwatch.StartNew();

            var fi = new FileInfo(filename);

            if (!StructureReader.IsStructureFilename(filename))
            {
                timer.Stop();
                Entries.Add(new IndexerEntry { Filename = fi.Name, Type = EntryType.Error, ErrorMessage = "Not supported", Timing = timer.Elapsed });
                return;
            }

            var filenameId = StructureReader.GetStructureIdFromFilename(filename);
            bool isModified = false;
            if (PreviouslyIndexed.ContainsKey(filenameId))
            {
                if (IsModified(fi, PreviouslyIndexed[filenameId]))
                {
                    isModified = true;
                }
                else
                {
                    timer.Stop();
                    Entries.Add(new IndexerEntry { Filename = fi.Name, Entry = PreviouslyIndexed[filenameId], Type = EntryType.AlreadyPresent, Timing = timer.Elapsed });
                    return;
                }
            }

            var localFilename = Path.Combine(DataFolder, fi.Name);

            if (File.Exists(localFilename))
            {
                File.Delete(localFilename);
                //if (File.Exists(localFilename + ".bnd")) File.Delete(localFilename + ".bnd");
                //if (File.Exists(localFilename + ".rgs")) File.Delete(localFilename + ".rgs");
            }

            try
            {
                CopyLocal(localFilename, streamProvider);
            }
            catch (Exception)
            {
                timer.Stop();
                Entries.Add(new IndexerEntry { Filename = fi.Name, Type = EntryType.Error, ErrorMessage = "Failed to copy the file to the local repository", Timing = timer.Elapsed });
                return;
            }

            try
            {
                var entry = DatabaseIndexEntry.CreateFromStructure(localFilename, fi, IndexUpdateVersion, this.Info);
                timer.Stop();
                Entries.Add(new IndexerEntry { Filename = fi.Name, Entry = entry, Timing = timer.Elapsed, Type = isModified ? EntryType.Modified : EntryType.New });
            }
            catch (Exception e)
            {
                timer.Stop();
                Entries.Add(new IndexerEntry { Filename = fi.Name, Type = EntryType.Error, ErrorMessage = e.Message, Timing = timer.Elapsed });
            }
        }

        void VisitUpdate(string localFilename)
        {
            Stopwatch timer = Stopwatch.StartNew();
            var fi = new FileInfo(localFilename);
            try
            {
                var entry = DatabaseIndexEntry.CreateFromStructure(localFilename, fi, IndexUpdateVersion, this.Info);
                timer.Stop();
                Entries.Add(new IndexerEntry { Filename = fi.Name, Entry = entry, Timing = timer.Elapsed, Type = EntryType.Updated });
            }
            catch (Exception e)
            {
                timer.Stop();
                Entries.Add(new IndexerEntry { Filename = fi.Name, Type = EntryType.Error, ErrorMessage = e.Message, Timing = timer.Elapsed });
            }
        }

        void DoUpdate(string path)
        {
            int index = Stats.UpdateCount + 1;
            
            using (Log = new StreamWriter(Path.Combine(Info.GetIndexFolderPath(), "log_" + index + ".txt")))
            using (UpdateIndex = new StreamWriter(Path.Combine(Info.GetIndexFolderPath(), "updateindex_" + index + ".csv")))
            using (Entries = new BlockingCollection<IndexerEntry>(1000))
            using (var processor = Task.Factory.StartNew(Process))
            {
                UpdateIndex.WriteLine("Filename,Action,Message,TimingMs");

                Stopwatch timer = Stopwatch.StartNew();

                try
                {
                    foreach (var e in PreviouslyIndexed)
                    {
                        var sn = e.Value.FilenameId + e.Value.Extension;
                        var newFilename = Path.Combine(path, sn);
                        if (!File.Exists(newFilename))
                        {
                            Entries.Add(new IndexerEntry { Filename = e.Value.FilenameId + e.Value.Extension, Type = EntryType.Removed, Timing = TimeSpan.FromSeconds(0) });
                        }
                    }

                    Parallel.ForEach(Directory.GetFiles(path), new ParallelOptions { MaxDegreeOfParallelism = 8 }, fn =>
                    {
                        VisitFile(fn, () => File.OpenRead(fn));
                    });                    
                }
                catch (Exception e)
                {
                    Log.WriteLine("Unexpected error: {0}.", e.Message);
                    throw e;
                }
                finally
                {
                    Entries.CompleteAdding();
                }
                
                Task.WaitAll(processor);
                
                timer.Stop();

                IsDatabaseModified = NumAdded > 0 || NumRemoved > 0 || NumModified > 0 || NumUpdated > 0;

                Log.WriteLine("Added {0}, modified {1}, removed {2}, error {4}, time {5}s.", 
                    NumAdded, NumModified, NumRemoved, NumUpdated, NumError, timer.Elapsed.TotalSeconds.ToStringInvariant("0.0"));
            }
        }

        void DoIndexUpdate()
        {
            int index = Stats.UpdateCount + 1;

            using (Log = new StreamWriter(Path.Combine(Info.GetIndexFolderPath(), "log_" + index + ".txt")))
            using (UpdateIndex = new StreamWriter(Path.Combine(Info.GetIndexFolderPath(), "updateindex_" + index + ".csv")))
            using (Entries = new BlockingCollection<IndexerEntry>(1000))
            using (var processor = Task.Factory.StartNew(Process))
            {
                UpdateIndex.WriteLine("Filename,Action,Message,TimingMs");

                Stopwatch timer = Stopwatch.StartNew();

                try
                {
                    Parallel.ForEach(PreviouslyIndexed.Values, new ParallelOptions { MaxDegreeOfParallelism = 8 }, e =>
                    {
                        var fn = e.FilenameId + e.Extension;
                        VisitUpdate(fn);
                    });
                }
                catch (Exception e)
                {
                    Log.WriteLine("Unexpected error: {0}.", e.Message);
                    throw e;
                }
                finally
                {
                    Entries.CompleteAdding();
                }

                Task.WaitAll(processor);

                timer.Stop();

                IsDatabaseModified = NumAdded > 0 || NumRemoved > 0 || NumModified > 0 || NumUpdated > 0;

                Log.WriteLine("Updated {0}, error {4}, time {5}s.",
                    NumAdded, NumModified, NumRemoved, NumUpdated, NumError, timer.Elapsed.TotalSeconds.ToStringInvariant("0.0"));
            }
        }
        
        public static DatabaseIndexer Update(DatabaseIndex index, string path, Action<string> visitedCallback)
        {
            var stats = index.Info.GetStatistics();

            var indexer = new DatabaseIndexer
            {
                IndexUpdateVersion = stats.Version + 1,
                DataFolder = index.Info.GetDataFolderPath(),
                Stats = stats,
                Info = index.Info,
                PreviouslyIndexed = index.Snapshot().ToDictionary(e => e.FilenameId, e => e, StringComparer.OrdinalIgnoreCase),
                VisitedCallback = visitedCallback ?? (_ => { })
            };

            indexer.DoUpdate(path);

            return indexer;
        }

        public static DatabaseIndexer UpdateEntryIndex(DatabaseIndex index, Action<string> visitedCallback)
        {
            var stats = index.Info.GetStatistics();

            var indexer = new DatabaseIndexer
            {
                IndexUpdateVersion = stats.Version + 1,
                DataFolder = index.Info.GetDataFolderPath(),
                Stats = stats,
                Info = index.Info,
                PreviouslyIndexed = index.Snapshot().ToDictionary(e => e.FilenameId, e => e, StringComparer.OrdinalIgnoreCase),
                VisitedCallback = visitedCallback ?? (_ => { })
            };

            indexer.DoIndexUpdate();

            return indexer;
        }

        public DatabaseUpdateResult MakeResult()
        {
            return new DatabaseUpdateResult
            {
                IsModified = IsDatabaseModified,
                NumAdded = NumAdded,
                NumModified = NumModified,
                NumRemoved = NumRemoved,
                NumUpdated = NumUpdated,
                NumError = NumError
            };
        }

        private DatabaseIndexer()
        {

        }
    }
}
