namespace WebChemistry.Queries.Service.Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.Queries.Core.Queries;
    using WebChemistry.Platform;

    public partial class QueriesExplorerInstance : PersistentObjectBase<QueriesExplorerInstance>
    {
        QueryResult QueryInternal(string queryString, QueryMotive query)
        {
            Log("Executing '{0}'...", queryString);

            var timer = Stopwatch.StartNew();

            var result = new QueryResult
            {
                Query = queryString,
                PatternLimit = PatternLimit,
                PatternAtomLimit = PatternAtomLimit
            };

            int patternCount = 0, atomCount = 0, structureCount = 0;
            bool patternLimitReached = false, atomLimitReached = false;
            
            Dictionary<string, string> pdbSources = new Dictionary<string, string>(StringComparer.Ordinal);

            Parallel.ForEach(Structures, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, MaxDegreeOfParallelism) }, sr =>
            {
                if (patternLimitReached || atomLimitReached) return;

                int matchCounter = 0;
                try
                {
                    var structure = sr.Value.Structure;
                    var ctx = Queries.Core.ExecutionContext.Create(structure);
                    var matches = query.Matches(ctx);

                    if (matches.Count > 0) Interlocked.Increment(ref structureCount);

                    foreach (var m in matches)
                    {
                        var mc = Interlocked.Increment(ref patternCount);
                        if (mc > PatternLimit)
                        {
                            patternLimitReached = true;
                            return;
                        }

                        var ac = Interlocked.Add(ref atomCount, m.Atoms.Count);
                        if (ac > PatternAtomLimit)
                        {
                            atomLimitReached = true;
                            return;
                        }

                        var serial = matchCounter++;
                        var match = m.ToStructure(serial.ToString(), addBonds: true, asPdb: true);
                        var rs = match.PdbResidues();

                        var entry = new PatternEntry
                        {
                            Id = match.Id,
                            ParentId = structure.Id,
                            Serial = serial,
                            AtomCount = match.Atoms.Count,
                            Signature = PdbResidueCollection.GetExplicitlyCountedResidueString(rs.ResidueCounts),
                            Atoms = PdbResidueCollection.GetExplicitlyCountedResidueString(match.Atoms
                                .GroupBy(a => a.ElementSymbol)
                                .ToDictionary(g => g.Key.ToString(), g => g.Count(), StringComparer.Ordinal)),
                            Residues = PdbResidueCollection.GetIdentifierString(rs),
                            ResidueCount = rs.Count,
                            SourceJson = match.ToJsonString(prettyPrint: false)
                        };
                        var pdb = match.ToPdbString();

                        lock (result)
                        {
                            pdbSources[entry.Id] = pdb;
                            result.Patterns.Add(entry);
                        }
                    }
                }
                catch (Exception e)
                {
                    lock (result)
                    {
                        result.Errors.Add(Log("'{0}': {1}", sr.Key, e.Message));
                    }
                }
            });

            if (patternLimitReached) result.Warnings.Add(string.Format("The pattern limit ({0}) was reached. Not all structures were queried.", PatternLimit));
            if (atomLimitReached) result.Warnings.Add(string.Format("The atom limit ({0}) was reached. Not all structures were queried.", StructureAtomLimit));

            result.StructureCount = structureCount;
            result.Patterns = result.Patterns.OrderBy(m => m.ParentId, StringComparer.Ordinal).ThenBy(m => m.Serial).ToList();

            try
            {
                var zipFn = Path.Combine(CurrentDirectory, "result.zip");
                if (File.Exists(zipFn)) File.Delete(zipFn);

                var csv = result.Patterns.GetExporter()
                    .AddStringColumn(m => m.Id, "Id")
                    .AddStringColumn(m => m.ParentId, "ParentId")
                    .AddNumericColumn(m => m.ResidueCount, "ResidueCount")
                    .AddStringColumn(m => m.Signature, "Signature")
                    .AddStringColumn(m => m.Residues, "Residues")
                    .AddNumericColumn(m => m.AtomCount, "AtomCount")
                    .AddStringColumn(m => m.Atoms, "Atoms")
                    .ToCsvString();
                
                using (var zip = ZipUtils.CreateZip(zipFn))
                {
                    foreach (var pdb in pdbSources)
                    {
                        zip.AddEntry(Path.Combine("patterns", pdb.Key + ".pdb"), pdb.Value);
                    }
                    zip.AddEntry("patterns.csv", csv);
                }
                result.ZipSizeInBytes = (int)new FileInfo(zipFn).Length;
                result.IsZipAvailable = true;
            }
            catch (Exception e)
            {
                Log("Failed to create 'result.zip' file: {0}", e.Message);
                result.Errors.Add("Failed to create result archive. It won't be possible to download the result.");
                result.IsZipAvailable = false;
            }

            result.PatternAtomLimitReached = atomLimitReached;
            result.PatternLimitReached = patternLimitReached;

            result.Structures = GetStructuresInfo();

            timer.Stop();
            result.QueryTimeMs = (int)timer.ElapsedMilliseconds;
            File.WriteAllText(Path.Combine(CurrentDirectory, "latest.json"), result.ToJsonString());
            Log("Found {0} patterns.", patternCount);
            QueryHistory.Add(queryString);
            Save();
            return result;
        }

        public QueryResult Query(string queryString, QueryMotive query)
        {
            return Guarded(() => QueryInternal(queryString, query));
        }
    }
}