namespace WebChemistry.MotiveValidator.Service
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using WebChemistry.Framework.Core;
    using WebChemistry.Queries.Core;
    using WebChemistry.Queries.Core.Queries;
    using WebChemistry.MotiveValidator.DataModel;
    using WebChemistry.Platform;
    using WebChemistry.Platform.Services;

    partial class ValidatorService : ServiceBase<ValidatorService, MotiveValidatorConfig, MotiveValidatorStandaloneConfig, object>
    {
        class DatabaseEntryInfo
        {
            public string Id { get; set; }
            public string ModelNames { get; set; }
            public int MotiveCount { get; set; }
            public string ErrorMessage { get; set; }
            public long TimingMs { get; set; }
        }

        void RunDatabase()
        {
            UpdateProgress("Loading models...");
            Log("Loading models...");
            ReadModels();
            Log("Found {0} models.", Models.Count);

            if (Models.Count == 0)
            {
                LogError("General", "Could not load any models.");
                return;
            }

            UpdateProgress("Computing...");

            ExecuteDatabase();
        }

        BlockingCollection<ExportInfo> DatabaseExportEntries;

        void DatabaseZipWriter()
        {
            try
            {
                while (true)
                {
                    ExportDatabaseResult(DatabaseExportEntries.Take());
                }
            }
            catch (InvalidOperationException)
            {

            }
            catch (Exception e)
            {
                Log("Unexpected writer error: {0}", e.Message);
            } 
        }

        void ExecuteDatabase()
        {
            var structures = Directory
                .EnumerateFiles(StandaloneSettings.InputFolder)
                .Where(f => StructureReader.IsStructureFilename(f))
                .ToArray();

            Log("Found {0} structures to process.", structures.Length);

            var reports = new List<DatabaseEntryInfo>(structures.Length);

            var ignoreList = StandaloneSettings.DatabaseModeIgnoreNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var includedModels = Models.Values
                .Where(m => !ignoreList.Contains(m.Name))
                .Where(m => m.Structure.Atoms.Count >= StandaloneSettings.DatabaseModeMinModelAtomCount)
                .ToArray();

            var queryModelNames = includedModels
                .Select(m => m.Name)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();

            if (queryModelNames.Length == 0) queryModelNames = new[] { "#none#" };
            var query = QueryBuilder.Residues(queryModelNames).Named().ConnectedAtoms(2, YieldNamedDuplicates: true).ToMetaQuery().Compile() as QueryMotive;

            SuppressProgressUpdate = true;

            Log("Initializing...");
            var aggregator = DatabaseAggregator.Create(includedModels.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase), this);
            
            using (var zip = ZipUtils.CreateZip(Path.Combine(ResultFolder, "data.zip")))
            using (DatabaseExportEntries = new BlockingCollection<ExportInfo>(2500))
            using (var processor = Task.Factory.StartNew(DatabaseZipWriter))
            {
                Log("Processing...");
                int progress = 0;

                Parallel.ForEach(structures, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, s =>
                {
                    var id = StructureReader.GetStructureIdFromFilename(s);
                    var timer = Stopwatch.StartNew();

                    try
                    {

                        StructureReaderResult structure = null;
                        KeyValuePair<MotiveModel, MotiveEntry[]>[] entries;
                        string errorMsg = "";

                        try
                        {
                            structure = StructureReader.Read(s);
                            entries = GetModelsAndMotives(structure, query);
                        }
                        catch (Exception e)
                        {
                            errorMsg = e.Message;
                            Log("Error reading '{0}': {1}", id, e.Message);
                            entries = new KeyValuePair<MotiveModel, MotiveEntry[]>[0];
                        }

                        var groups = entries.Select(e => new GroupAnalysis
                            {
                                Model = e.Key,
                                Service = this,
                                Entries = e.Value
                            }).ToArray();
                        groups.ForEach(g => g.Run(maxParallelism: 1, showLog: false));

                        var analyzedGroups = groups.Where(g => g.IsAnalyzed).ToArray();
                        var allEntries = analyzedGroups.SelectMany(g => g.AnalyzedEntries).ToArray();
                        var results = allEntries.Select(r => r.Result).ToArray();
                        var result = MakeResult(analyzedGroups, allEntries, ignoreErrors: true);
                        var resultJson = result.ToJsonString();

                        DatabaseExportEntries.Add(new ExportInfo
                        {
                            ResultZip = zip,
                            GeneratePerModelData = true,
                            ZipFolder = Path.Combine("by_structure", id),
                            AllEntries = allEntries,
                            AnalyzedGroups = analyzedGroups,
                            Results = results,
                            ResultJson = resultJson
                        });

                        var report = new DatabaseEntryInfo
                        {
                            Id = id,
                            ModelNames = entries.Select(m => m.Key.Name).JoinBy("; "),
                            MotiveCount = entries.Sum(e => e.Value.Length),
                            ErrorMessage = errorMsg
                        };
                        
                        aggregator.ProcessResult(id, structure != null ? structure.Structure.Atoms.Count : 0, result);
                        
                        lock (reports)
                        {
                            timer.Stop();
                            report.TimingMs = timer.ElapsedMilliseconds;
                            reports.Add(report);
                        }
                    }
                    catch (Exception ex)
                    {
                        timer.Stop();
                        lock (reports)
                        {
                            reports.Add(new DatabaseEntryInfo
                            {
                                Id = id,
                                ModelNames = "",
                                MotiveCount = 0,
                                TimingMs = timer.ElapsedMilliseconds,
                                ErrorMessage = ex.Message
                            });
                        }
                    }

                    Interlocked.Increment(ref progress);
                    if (progress % 500 == 0) Log("Finished {0} / {1}.", progress, structures.Length);
                });
                
                DatabaseExportEntries.CompleteAdding();
                Log("Waiting for export task to finish...");
                Task.WaitAll(processor);

                Log("Exporting aggregates...");
                foreach (var model in aggregator.Models)
                {
                    ExportAggregateResult(zip, "by_model", model.Value);
                }

                Log("Computing and exporting model summary...");
                aggregator.ExportModelSummary(ResultFolder);
                Log("Exporting structure summary...");
                aggregator.ExportStructureSummary(ResultFolder);
            }

            SuppressProgressUpdate = false;

            var totalMotives = reports.Sum(r => r.MotiveCount);
            Log("Found total of {0} motifs in {1} structures.", totalMotives, structures.Length);

            // Export the report.
            File.WriteAllText(Path.Combine(ResultFolder, "report.csv"), reports.OrderBy(r => r.Id).GetExporter().AddPropertyColumns().ToCsvString());
            File.WriteAllText(Path.Combine(ResultFolder, "all_errors.csv"), ErrorsToCsv(Errors));
            File.WriteAllLines(Path.Combine(ResultFolder, "analyzed_names.txt"), queryModelNames);
            File.WriteAllText(Path.Combine(ResultFolder, "stats.json"), new
                {
                    MotiveCount = totalMotives,
                    StructureCount = structures.Length,
                    ModelCount = queryModelNames.Length
                }.ToJsonString());
        }

        KeyValuePair<MotiveModel, MotiveEntry[]>[] GetModelsAndMotives(StructureReaderResult sr, QueryMotive query)
        {
            Dictionary<string, MotiveModel> localModels = new Dictionary<string, MotiveModel>(StringComparer.OrdinalIgnoreCase);

            List<MotiveEntry> ret = new List<MotiveEntry>();
            FileInfo fi = new FileInfo(sr.Filename);
            try
            {
                var s = FilterStructureAtoms(sr.Structure);
                var residues = s.PdbResidues();
                var matches = query.Matches(Queries.Core.ExecutionContext.Create(s));

                HashSet<MotiveModel> includedModels = new HashSet<MotiveModel>();
                int index = 0;
                foreach (var match in matches)
                {
                    var residue = residues.FromAtom(s.Atoms.GetById(match.Name.Value));

                    MotiveModel model;
                    if (!localModels.TryGetValue(residue.Name, out model))
                    {
                        model = Models[residue.Name].Clone();
                        localModels.Add(residue.Name, model);
                    }
                    if (includedModels.Add(model)) HandleParentWarnings(sr, model);

                    var motive = FilterStructureAtoms(match.ToStructure(index.ToString(), addBonds: true, asPdb: true));
                    int alternateLocationCount;
                    HandleChildWarnings(sr, model, motive, out alternateLocationCount);
                    index++;

                    var entry = new MotiveEntry
                    {
                        Model = model,
                        Motive = motive,
                        AlternateLocationCount = alternateLocationCount
                    };

                    ret.Add(entry);
                }
                lock (AnalyzedProteinIds)
                {
                    AnalyzedProteinIds.Add(s.Id);
                }
            }
            catch (Exception e)
            {
                LogError(fi.Name, "Extracting motifs: {0}", e.Message);
            }

            return ret
                .GroupBy(m => m.Model.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => new KeyValuePair<MotiveModel, MotiveEntry[]>(localModels[g.Key], g.ToArray()))
                .ToArray();
        }
    }
}
