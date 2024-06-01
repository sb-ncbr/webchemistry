namespace WebChemistry.Queries.Service
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
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.Queries.Core;
    using WebChemistry.Queries.Service.DataModel;
    using WebChemistry.Platform;
    using WebChemistry.Platform.MoleculeDatabase;
    using WebChemistry.Platform.Services;
    using WebChemistry.Platform.Users;

    public partial class QueriesService : ServiceBase<QueriesService, QueriesServiceSettings, QueriesStandaloneServiceSettings, QueriesServiceState>
    {
        public override AppVersion GetVersion()
        {
            return new AppVersion(1, 1, 23, 12, 27, 'b');
        }
                
        public override string GetName()
        {
            return "Queries";
        }
        
        string GetMotiveName(string parentId, int index)
        {
            return parentId + "_" + index;
        }

        int MaxParallelism = 8;

        const int CacheSize = 10000;
        
        bool StatisticsOnly = false;

        int PatternCountLimit = int.MaxValue;
        long AtomCountLimit = long.MaxValue;

        int PatternsFoundTotal;
        long AtomsFoundTotal;

        int ErrorCount = 0;
        bool PatternLimitReached = false, AtomLimitReached = false;

        ZipUtils.ZipWrapper Zip;

        int TotalStructureCount;
        QueryWrap[] Queries;
        Dictionary<string, QueryWrap> QueryIndex;

        ValidationHelper Validator;

        List<ComputationStructureEntry> Structures = new List<ComputationStructureEntry>();
        BlockingCollection<QueryResult> ResultsQueue;
                
        class StructureEntry
        {
            public string FilenameId { get; set; }
            public Func<StructureReaderResult> Provider { get; set; }
        }

        void InitQueries(QueryInfo[] infos)
        {
            infos.ForEach(i => i.Id = i.Id.Trim());

            if (infos.Select(i => i.Id).Where(n => n.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Count() != infos.Length)
            {
                throw new InvalidOperationException("Each query must have an unique name.");
            }

            if (infos.Any(i => i.Id.Contains('.')))
            {
                throw new InvalidOperationException("Query id cannot contain '.'.");
            }

            var invalidNames = new List<string>();
            foreach (var i in infos)
            {
                if (string.IsNullOrEmpty(i.QueryString))
                {
                    throw new InvalidOperationException(string.Format("Query string for '{0}' is empty.", invalidNames.JoinBy()));
                }
                try
                {
                    new DirectoryInfo(i.Id);
                }
                catch
                {
                    invalidNames.Add(i.Id);
                }
            }

            if (invalidNames.Count > 0)
            {
                throw new InvalidOperationException(string.Format("'{0}' are not valid query names.", invalidNames.JoinBy()));
            }

            this.Queries = infos.Select(q => new QueryWrap
                {
                    Id = q.Id,
                    QueryString = q.QueryString,
                    Query = PythonEngine.GetQuery(q.QueryString, BasicTypes.PatternSeq)
                })
                .OrderBy(q => q.Id, StringComparer.Ordinal)
                .ToArray();
            
            this.QueryIndex = Queries.ToDictionary(q => q.Id, StringComparer.Ordinal);
        }

        void Execute(StructureEntry[] structures)
        {   
            var totalTime = Stopwatch.StartNew();
            TotalStructureCount = structures.Length;

            Status.Message = "Finding Patterns...";
            Status.CurrentProgress = 0;
            Status.IsIndeterminate = false;
            Status.MaxProgress = structures.Length;
            SaveStatus();

            int numVisited = 0;
            object sync = new object();
            
            using (Zip = ZipUtils.CreateZip(Path.Combine(ResultFolder, "result.zip")))
            using (ResultsQueue = new BlockingCollection<QueryResult>(CacheSize))
            using (var processor = Task.Factory.StartNew(Process))
            {
                bool error = false;

                try
                {
                    Parallel.ForEach(structures, new ParallelOptions { MaxDegreeOfParallelism = 8 },  s =>
                    {
                        var watch = Stopwatch.StartNew();
                        var localWatch = Stopwatch.StartNew();
                        int ind;

                        ComputationStructureEntry entry = new ComputationStructureEntry
                        {
                            Id = s.FilenameId
                        };
                        
                        IStructure structure = null;

                        try
                        {
                            if (PatternsFoundTotal > PatternCountLimit)
                            {
                                PatternLimitReached = true;
                                throw new InvalidOperationException(string.Format("Max. pattern count ({0}) reached. Structure skipped. Try a less general query or execute your query on a smaller input set.", PatternCountLimit));
                            }

                            if (AtomsFoundTotal > AtomCountLimit)
                            {
                                AtomLimitReached = true;
                                throw new InvalidOperationException(string.Format("Max. atom count ({0}) reached. Structure skipped. Try a less general query or execute your query on a smaller input set.", AtomCountLimit));
                            }

                            var structureResult = s.Provider();
                            entry.ReaderWarnings = structureResult.Warnings.Select(w => w.ToString()).ToArray();
                            structure = structureResult.Structure;
                            watch.Stop();
                            entry.LoadTimingMs = (int)watch.ElapsedMilliseconds;
                        }
                        catch (Exception e)
                        {
                            watch.Stop();
                            ErrorCount++;
                            ind = Interlocked.Increment(ref numVisited);
                            entry.ErrorType = ComputationStructureErrorType.Reader;
                            entry.ErrorMessage = e.Message;
                            entry.LoadTimingMs = (int)watch.ElapsedMilliseconds;

                            ProcessStructure(new StructureResultWrap
                            {
                                Structure = structure,
                                Entry = entry,
                                Results = new List<QueryResult>()
                            });

                            Log("[{0}/{1}] {2}", ind, structures.Length, entry.ToString());
                            return;
                        }

                        var results = new List<QueryResult>();

                        try
                        {
                            watch.Restart();
                            int motiveCount = 0;

                            var ctx = WebChemistry.Queries.Core.ExecutionContext.Create(structure);

                            foreach (var query in Queries)
                            {
                                int matchCounter = 0;
                                localWatch.Restart();
                                try
                                {
                                    var matches = query.Query.Matches(ctx);
                                    localWatch.Stop();
                                    motiveCount += matches.Count;

                                    var patterns = new List<Tuple<IStructure, ComputationPatternEntry>>(matches.Count);
                                    foreach (var m in matches)
                                    {
                                        var patternsFoundTotal = Interlocked.Increment(ref PatternsFoundTotal);
                                        if (patternsFoundTotal > PatternCountLimit)
                                        {
                                            PatternLimitReached = true;
                                            break;
                                        }

                                        var atomsFoundTotal = Interlocked.Add(ref AtomsFoundTotal, (long)m.Atoms.Count);
                                        if  (atomsFoundTotal > AtomCountLimit)
                                        {
                                            AtomLimitReached = true;
                                            break;
                                        }

                                        var serial = matchCounter++;
                                        var match = m.ToStructure(serial.ToString(), addBonds: true, asPdb: true);
                                        var rs = match.PdbResidues();
                                        patterns.Add(Tuple.Create(
                                            match,
                                            new ComputationPatternEntry
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
                                                ResidueCount = rs.Count
                                            }));
                                    }

                                    results.Add(new QueryResult
                                    {
                                        ParentId = structure.Id,
                                        Query = query,
                                        TimingMs = (int)localWatch.ElapsedMilliseconds,
                                        Motives = patterns
                                    });
                                }
                                catch (Exception innerEx)
                                {
                                    localWatch.Stop();
                                    results.Add(new QueryResult
                                    {
                                        ParentId = structure.Id,
                                        Query = query,
                                        TimingMs = (int)localWatch.ElapsedMilliseconds,
                                        Motives = new List<Tuple<IStructure,ComputationPatternEntry>>(),
                                        ErrorMessage = innerEx.Message
                                    });
                                }
                            }

                            entry.PatternCount = motiveCount;
                        }
                        catch (Exception e)
                        {
                            Interlocked.Increment(ref ErrorCount);
                            entry.ErrorType = ComputationStructureErrorType.Generic;
                            entry.ErrorMessage = e.Message;
                        }

                        watch.Stop();
                        entry.QueryTimingMs = (int)watch.ElapsedMilliseconds;
                        ind = Interlocked.Increment(ref numVisited);
                        Log("[{0}/{1}] {2}", ind, structures.Length, entry.ToString());

                        if (RequiresUpdate())
                        {
                            this.Status.CurrentProgress = numVisited;
                            this.CustomState.MotivesFound = PatternsFoundTotal;
                            this.CustomState.MotiveLimitReached = PatternLimitReached;
                            this.CustomState.ErrorCount = ErrorCount;
                            this.UpdateStatus();
                        }

                        ProcessStructure(new StructureResultWrap
                        {
                            Structure = structure,
                            Entry = entry,
                            Results = results
                        });
                    });

                    this.Status.CurrentProgress = numVisited;
                    this.CustomState.MotivesFound = PatternsFoundTotal;
                    this.CustomState.MotiveLimitReached = PatternLimitReached;
                    this.CustomState.ErrorCount = ErrorCount;
                    SaveStatus();
                }
                catch (AggregateException e)
                {
                    var message = string.Join(",", e.InnerExceptions.Select(x => x.Message).ToArray());
                    Log("Errors ({0}): {1}", e.InnerExceptions.Count, message);
                    Status.State = Platform.Computation.ComputationState.Failed;
                    Status.Message = message;
                    SaveStatus();
                    error = true;
                }
                catch (Exception e)
                {
                    Log("Error: {0}", e.Message);
                    Status.State = Platform.Computation.ComputationState.Failed;
                    Status.Message = e.Message;
                    SaveStatus();
                    error = true;
                }
                finally
                {
                    ResultsQueue.CompleteAdding();
                }
                
                Log("Waiting for query tasks to finish...");
                processor.Wait();

                this.Status.CurrentProgress = numVisited;
                this.CustomState.MotivesFound = PatternsFoundTotal;
                SaveStatus();
                
                if (!error)
                {                
                    Status.IsIndeterminate = true;

                    Status.Message = "Processing Metadata...";
                    SaveStatus();

                    Log("Processing Metadata...");
                    ProcessMetadata();

                    Status.Message = "Exporting...";
                    SaveStatus();
                    Log("Exporting...");
                    Export(totalTime);

                    Status.Message = "Done.";
                    SaveStatus();
                }

                Log("Found {0} patterns in {1}.", PatternsFoundTotal, totalTime.Elapsed);
            }
        }

        protected override void RunHostedInternal()
        {
            MaxParallelism = Settings.MaxParallelism;
            Validator = new ValidationHelper(
                Settings.DoValidation && Settings.ValidatorId.HasValue, 
                Settings.ValidatorId.HasValue ? Settings.ValidatorId.Value : new EntityId());
            Status.Message = "Initializing...";
            Status.IsIndeterminate = true;
            SaveStatus();

            if (Settings.MotiveCountLimit > 0) this.PatternCountLimit = Settings.MotiveCountLimit;
            if (Settings.AtomCountLimit > 0) this.AtomCountLimit = Settings.AtomCountLimit;

            InitQueries(Settings.Queries);
            var snapshot = DatabaseSnapshot.Load(Computation.GetInputDirectoryId().GetChildId("data"));
            var structures = snapshot.Snapshot().Select(e => new StructureEntry 
                {
                    Provider = () => e.ReadStructure(),
                    FilenameId = e.FilenameId
                })
                .ToArray();

            Execute(structures);

            if (Settings.NotifyUser)
            {
                Log("Notifying the user...");
                if (UserComputationFinishedNotification.TrySend(Settings.NotifyUserConfig, Computation.ShortId)) Log("User notified.");
                else Log("User notification failed.");
            }
        }

        protected override void RunStandaloneInternal()
        {
            StatisticsOnly = StandaloneSettings.StatisticsOnly;
            MaxParallelism = StandaloneSettings.MaxParallelism;
            Validator = new ValidationHelper(false, new EntityId());

            Status.Message = "Initializing...";
            Status.IsIndeterminate = true;
            SaveStatus();

            if (!string.IsNullOrEmpty(StandaloneSettings.CSAPath))
            {
                try
                {
                    WebChemistry.Queries.Core.Utils.CatalyticSiteAtlas.Init(StandaloneSettings.CSAPath);
                    Log("CSA Initialized.");
                }
                catch (Exception e)
                {
                    Log("Failed to initialize CSA: {0}", e.Message);
                }
            }

            InitQueries(StandaloneSettings.Queries);

            var structures = StandaloneSettings.InputFolders.Select(f => new
                {
                    Folder = f,
                    Name = new string(f.Replace('\\', '/').Where(c => EntityId.IsLegalChar(c)).ToArray()),
                    Files = Directory.GetFiles(f).Where(x => StructureReader.IsStructureFilename(x)).ToArray()
                })
                .SelectMany(d => d.Files.Select(f => new  StructureEntry
                { 
                    Provider = () => StructureReader.Read(f),
                    FilenameId = StructureReader.GetStructureIdFromFilename(f)
                }))
                .ToArray();
            Execute(structures);
        }

        protected override HelpOutputStructureDescription MakeOutputStructure()
        {
            return HelpFolder("result", "Folder with computation result",
                HelpFolder("query_id", "Folder for each query named by its unique id.",
                  HelpFolder("patterns", "Folder with patterns stored in PDB format.",
                    HelpFile("data.json", "Information about the result in JSON format."),
                    HelpFile("patterns.csv", "Information about the patterns in CSV format."),
                    HelpFile("metadata_summary.csv", "Information about the metadata (origin organism, EC number, etc.) in CSV format."),
                    HelpFile("structures.csv", "Information about structures that contain the given patterns in CSV format."))),
                HelpFile("structures.csv", "Information about all structures that were queried in CSV format."),
                HelpFile("structures.json", "Information about all structures that were queried in JSON format."),
                HelpFile("summary.json", "Summary information (number of found patterns, etc.) about the computation in JSON format."));
        }

        protected override QueriesStandaloneServiceSettings SampleStandaloneSettings()
        {
            return new QueriesStandaloneServiceSettings
            {
                InputFolders = new string[] { @"c:\TestData\PDB\SampleSet1" },
                Queries = new QueryInfo[] 
                { 
                    new QueryInfo { Id = "NAGs", QueryString = "Residues(\"NAG\")" } ,
                    new QueryInfo { Id = "MANs", QueryString = "Residues('MAN')" } 
                },
                StatisticsOnly = false,
                CSAPath = @"c:\data\csa.dat",
                MaxParallelism = 8
            };
        }
    }
}
