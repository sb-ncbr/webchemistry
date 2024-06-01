namespace WebChemistry.Queries.Service.Explorer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using WebChemistry.Framework.Core;
    using WebChemistry.Platform;

    public partial class QueriesExplorerInstance : PersistentObjectBase<QueriesExplorerInstance>
    {
        int AtomCount;
        Dictionary<string, StructureReaderResult> Structures = new Dictionary<string, StructureReaderResult>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> Filenames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        StructureEntry[] GetStructuresInfo()
        {
            return Structures.Select(s => new StructureEntry
            {
                Id = s.Key,
                Warnings = s.Value.Warnings.Select(w => w.ToString()).ToArray(),
                AtomCount = s.Value.Structure.Atoms.Count,
                ResidueCount = Math.Max(1, s.Value.Structure.PdbResidues().Count)
            }).OrderBy(s => s.Id, StringComparer.Ordinal).ToArray();
        }

        string GetStructuresDirectory()
        {
            var ret = Path.Combine(this.CurrentDirectory, "structures");
            if (!Directory.Exists(ret)) Directory.CreateDirectory(ret);
            return ret;
        }

        void ReadStructures()
        {
            Parallel.ForEach(Directory.GetFiles(GetStructuresDirectory()), new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, MaxDegreeOfParallelism) }, f =>
            {
                if (!StructureReader.IsStructureFilename(f)) return;

                try
                {
                    var s = StructureReader.Read(f.ToLowerInvariant());
                    lock (Structures)
                    {
                        Structures[s.Structure.Id] = s;
                        Filenames[s.Structure.Id] = f;
                        AtomCount += s.Structure.Atoms.Count;
                    }
                }
                catch (Exception e)
                {
                    Log("Failed to load '{0}': {1}", new FileInfo(f).Name, e.Message);
                }
            });

            Log("Read {0} structures.", Structures.Count);
        }

        AddStructuresResultInternal AddStructuresInternal(IEnumerable<AddStructureEntry> xs, int maxParallelism)
        {
            xs = xs.OrderBy(x => x.Filename, StringComparer.Ordinal).ToList();

            var ret = new AddStructuresResultInternal();
            
            int visited = 0;
            bool limitReached = false;
            Parallel.ForEach(xs, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, maxParallelism) }, s =>
            {
                if (limitReached) return;

                if (s.Filename.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var zipResult = AddStructuresFromZip(s.Provider);

                        lock (ret)
                        {
                            ret.Errors.AddRange(zipResult.Errors);
                            ret.Warnings.AddRange(zipResult.Warnings);
                            ret.Structures.AddRange(zipResult.Structures);
                        }
                    }
                    catch
                    {
                        lock (ret)
                        {
                            ret.Errors.Add(string.Format("Error processing zip file '{0}'.", s.Filename));
                        }
                    }
                    return;
                }
                if (Structures.Count >= StructureLimit)
                {
                    lock (ret)
                    {
                        ret.Warnings.Add(string.Format("Structure limit ({0}) exceeded. {1} file(s) were not added.", StructureLimit, xs.Count() - visited));
                        limitReached = true;
                    }
                    return;
                }

                Interlocked.Increment(ref visited);

                FileInfo fi;
                string localFn = null;
                try
                {
                    fi = new FileInfo(s.Filename);
                }
                catch
                {
                    lock (ret)
                    {
                        ret.Errors.Add(string.Format("'{0}' is not a supported file type ({1})", s.Filename, StructureReader.SupportedExtensions.JoinBy()));
                    }
                    return;
                }

                Log("Reading {0}...", s.Filename);
                try
                {
                    if (!StructureReader.IsStructureFilename(s.Filename))
                    {
                        lock (ret)
                        {
                            ret.Errors.Add(string.Format("'{0}' is not a supported file type ({1})", fi.Name, StructureReader.SupportedExtensions.JoinBy()));
                        }
                        return;
                    }
                    var id = StructureReader.GetStructureIdFromFileInfo(fi).ToLowerInvariant();
                    lock (ret)
                    {
                        if (Structures.ContainsKey(id))
                        {
                            ret.Warnings.Add(string.Format("'{0}': Structure with this id has already been loaded.", id));
                            return;
                        }
                    }

                    localFn = Path.Combine(GetStructuresDirectory(), fi.Name);
                    using (var file = File.OpenWrite(localFn))
                    using (var stream = s.Provider())
                    {
                        stream.CopyTo(file);
                        file.Flush();
                    }

                    StructureReaderResult sr = StructureReader.Read(localFn);

                    if (AtomCount + sr.Structure.Atoms.Count > StructureAtomLimit)
                    {
                        lock (ret)
                        {
                            ret.Warnings.Add(string.Format("Total atom count limit ({0}) exceeded. {1} file(s) were not added.", StructureAtomLimit, xs.Count() - visited + 1));
                            limitReached = true;
                        }
                        return;
                    }

                    lock (ret)
                    {
                        if (Structures.ContainsKey(id))
                        {
                            ret.Warnings.Add(string.Format("'{0}': Structure with this id has already been loaded, ignoring file '{1}'.", id, fi.Name));
                            return;
                        }

                        Filenames[id] = localFn;
                        Structures[id] = sr;
                        AtomCount += sr.Structure.Atoms.Count;

                        ret.Structures.Add(new StructureEntry
                        {
                            Id = id,
                            Warnings = sr.Warnings.Select(w => w.ToString()).ToArray(),
                            AtomCount = sr.Structure.Atoms.Count,
                            ResidueCount = Math.Max(1, sr.Structure.PdbResidues().Count)
                        });
                    }
                }
                catch (Exception e)
                {
                    if (localFn != null && File.Exists(localFn)) File.Delete(localFn);

                    lock (ret)
                    {
                        ret.Errors.Add(string.Format("'{0}': {1}", fi.Name, e.Message));
                    }
                }
            });
            
            Log("Added {0} structures ({1} errors, {2} warnings).", ret.Structures.Count, ret.Errors.Count, ret.Warnings.Count);
                

            return ret;
        }

        AddStructuresResultInternal AddStructuresFromZip(Func<Stream> stream)
        {
            var xs = new List<AddStructureEntry>();

            using (var archive = ZipArchiveInterface.FromStream(stream))
            {
                foreach (var e in archive.GetEntryNames().Where(e => StructureReader.IsStructureFilename(e)))
                {
                    var data = archive.GetEntryString(e);
                    xs.Add(new AddStructureEntry { Filename = e, Provider = () => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(data)) });
                }
            }
            return AddStructuresInternal(xs, 1);
        }

        public AddStructuresResult AddStructures(IEnumerable<AddStructureEntry> xs)
        {
            return Guarded(() => 
                {
                    var ret = AddStructuresInternal(xs, MaxDegreeOfParallelism);
                    return new AddStructuresResult
                    {
                        Errors = ret.Errors,
                        Warnings = ret.Warnings,
                        NewIdentifiers = ret.Structures.Select(s => s.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray(),
                        AllStructures = GetStructuresInfo()
                    };
                });
        }

        string[] RemoveStructuresInternal(IEnumerable<string> ids)
        {
            ids = ids.AsList();
            Log("Removing {0}.", ids.JoinBy());
            var removed = new List<string>();
            foreach (var id in ids)
            {
                StructureReaderResult s;
                if (!Structures.TryGetValue(id, out s)) continue;
                Structures.Remove(id);
                AtomCount -= s.Structure.Atoms.Count;
                removed.Add(id);
                try
                {
                    File.Delete(Filenames[id]);
                }
                catch
                {
                    Log("Failed to delete '{0}'.", id);
                }
                Filenames.Remove(id);
            }
            return removed.ToArray();
        }

        public string[] RemoveStructures(IEnumerable<string> ids)
        {
            return Guarded(() => RemoveStructuresInternal(ids));
        }

        public string[] RemoveAllStructures()
        {
            return Guarded(() => RemoveStructuresInternal(this.Structures.Keys.ToArray()));
        }
    }
}