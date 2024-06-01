using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Core.Pdb;
using WebChemistry.MotiveAtlas.DataModel;
using WebChemistry.Queries.Core;
using WebChemistry.Platform;
using WebChemistry.Platform.MoleculeDatabase;
using WebChemistry.Platform.Server;
using WebChemistry.Platform.Services;

namespace WebChemistry.MotiveAtlas.Analyzer
{
    class AtlasAnalyzer : ServiceBase<AtlasAnalyzer, object, AtlasConfig, object>
    {
        MotiveAtlasApp App;
        MotiveAtlasObject Atlas;
        DatabaseInfo Database;
        AtlasAnalysisDescriptor Analysis;
        MotiveAnalysisDescriptor[] FlatMotives;
        string AtlasDataFolder;

        const int CacheSize = 1000;
        BlockingCollection<ProcessEntry> Entries;

        protected override void RunHostedInternal()
        {
            throw new NotSupportedException();
        }

        class ProcessEntry
        {
            public string StructureId { get; set; }

            public MotiveAnalysisDescriptor Motive { get; set; }
            public List<IStructure> BaseMotives { get; set; }
            public KeyValuePair<string, List<IStructure>>[] MotiveGroups { get; set; }

            public List<MotiveAnalysisEntry> Entries { get; set; }

            public Dictionary<string, int> MotiveAmbientResidueCounts { get; set; }
        }

        void Process()
        {
            bool continueProcess = true;
            while (continueProcess)
            {
                ProcessEntry entry = null;
                try
                {
                    entry = Entries.Take();
                   
                    var motive = entry.Motive;

                    // Add the structure.
                    motive.AffectedStructures.Add(entry.StructureId);
                    
                    // Update counts
                    int count = 0;
                    motive.StructureCounts.TryGetValue(entry.StructureId, out count);
                    motive.StructureCounts[entry.StructureId] = count + entry.BaseMotives.Count;
                    motive.MotiveCount += entry.BaseMotives.Count;

                    // Update residue counts.
                    FrequencyCounter.AddIntoA(motive.MotiveAmbientResidueCounts, entry.MotiveAmbientResidueCounts);

                    // Add entries.
                    motive.Motives.AddRange(entry.Entries);

                    // Write the PDB data.
                    using (var stream = new FileStream(Path.Combine(motive.GetFolder(AtlasDataFolder), MotiveAnalysisDescriptor.MotivesSourceFilename), FileMode.Append))
                    using (var writer = new StreamWriter(stream))
                    {
                        foreach (var m in entry.BaseMotives)
                        {
                            // Update pivot if necessary
                            if (motive.Pivot == null || motive.Pivot.Atoms.Count < m.Atoms.Count)
                            {
                                motive.Pivot = m;
                            }

                            writer.WriteLine(string.Format("$base\\{0}.pdb", m.Id));
                            m.WritePdb(writer);
                        }
                        foreach (var group in entry.MotiveGroups)
                        {
                            foreach (var m in group.Value)
                            {
                                writer.WriteLine(string.Format("${0}\\{1}.pdb", group.Key, m.Id));
                                m.WritePdb(writer);
                            }
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    continueProcess = false;
                }
                catch (Exception e)
                {
                    Log("Error: Motive = {0}, Structure = {1}, Message = {2}", entry.Motive.Id, entry.StructureId, e.Message);
                }
            }
        }
        
        void ComputeAndExportSummary()
        {
            Log("Computing frequency summaries.");
            // Update total residue counts.
            Analysis.StructureResidueTotalCounts = FrequencyCounter.Sum(Analysis.StructureResidueCounts.Values);
            Analysis.NumberOfResiduesPerStructure = Analysis.StructureResidueCounts.ToDictionary(c => c.Key, c => c.Value.Sum(x => PdbResidue.IsWaterName(x.Key) ? 0 : x.Value), StringComparer.Ordinal);
            Analysis.TotalResidueCount = Analysis.NumberOfResiduesPerStructure.Values.Sum();

            Log("Computing and exporting motive summaries.");
            // compute aggregates for motifs
            Parallel.ForEach(FlatMotives, new ParallelOptions { MaxDegreeOfParallelism = 8 }, desc =>
            {
                desc.ComputeSummary();
                var folder = desc.GetFolder(AtlasDataFolder);
                JsonHelper.WriteJsonFile(Path.Combine(folder, MotiveAtlasObject.SummaryFilename), desc.Summary);

                ZipUtils.CreateZip(Path.Combine(folder, MotiveAtlasObject.StructureIndexCompressedFilename), ctx =>
                {
                    ctx.BeginEntry(MotiveAtlasObject.StructureIndexFilename);
                    MotiveAnalysisEntry.GetExporter(desc.Motives, ",").WriteCsvString(ctx.TextWriter);
                });

                File.WriteAllLines(Path.Combine(folder, MotiveAtlasObject.StructureListFilename), desc.AffectedStructures.OrderBy(c => c));
            });

            Log("Computing and exporting subcategory summaries.");
            // flatten subcategories and compute aggregates
            var flatCategories = Analysis.Categories.SelectMany(c => c.SubCategories).ToArray();
            Parallel.ForEach(flatCategories, new ParallelOptions { MaxDegreeOfParallelism = 8 }, desc =>
            {
                desc.ComputeSummary();
                var folder = desc.GetFolder(AtlasDataFolder);
                JsonHelper.WriteJsonFile(Path.Combine(folder, MotiveAtlasObject.SummaryFilename), desc.Summary);

                ZipUtils.CreateZip(Path.Combine(folder, MotiveAtlasObject.StructureIndexCompressedFilename), ctx =>
                {
                    ctx.BeginEntry(MotiveAtlasObject.StructureIndexFilename);
                    MotiveAnalysisEntry.GetExporter(desc.Motives.SelectMany(m => m.Motives), ",").WriteCsvString(ctx.TextWriter);
                });

                File.WriteAllLines(Path.Combine(folder, MotiveAtlasObject.StructureListFilename), desc.AffectedStructures.OrderBy(c => c));
            });

            Log("Computing and exporting category summaries.");
            // compute aggregates for categories
            Parallel.ForEach(Analysis.Categories, new ParallelOptions { MaxDegreeOfParallelism = 8 }, desc =>
            {
                desc.ComputeSummary();
                var folder = desc.GetFolder(AtlasDataFolder);
                JsonHelper.WriteJsonFile(Path.Combine(folder, MotiveAtlasObject.SummaryFilename), desc.Summary);

                ZipUtils.CreateZip(Path.Combine(folder, MotiveAtlasObject.StructureIndexCompressedFilename), ctx =>
                {
                    ctx.BeginEntry(MotiveAtlasObject.StructureIndexFilename);
                    MotiveAnalysisEntry.GetExporter(desc.SubCategories.SelectMany(c => c.Motives.SelectMany(m => m.Motives)), ",").WriteCsvString(ctx.TextWriter);
                });

                File.WriteAllLines(Path.Combine(folder, MotiveAtlasObject.StructureListFilename), desc.AffectedStructures.OrderBy(c => c));
            });

            Log("Computing and exporting atlas summary.");
            Analysis.ComputeSummary();
            JsonHelper.WriteJsonFile(Path.Combine(AtlasDataFolder, MotiveAtlasObject.SummaryFilename), Analysis.Summary);

            Log("Creating and exporting descriptors.");
            // get and save the descriptor            
            JsonHelper.WriteJsonFile(Path.Combine(AtlasDataFolder, MotiveAtlasObject.DescriptorFilename), Analysis.GetDescriptor());
        }
        
        void Compress()
        {
            int numVisited = 0;
            object sync = new object();

            Parallel.ForEach(FlatMotives, new ParallelOptions { MaxDegreeOfParallelism = 8 }, desc =>
            {
                var folder = desc.GetFolder(AtlasDataFolder);
                
                ZipUtils.CreateZip(Path.Combine(folder, "motifs.zip"), ctx =>
                {
                    using (var reader = new StreamReader(Path.Combine(folder, MotiveAnalysisDescriptor.MotivesSourceFilename)))
                    {
                        ZipEntry entry = null;
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            if (line.StartsWith("$", StringComparison.Ordinal))
                            {
                                if (entry != null)
                                {
                                    ctx.EndEntry();
                                }

                                entry = new ZipEntry(line.Substring(1));
                                ctx.BeginEntry(line.Substring(1));
                            }
                            else
                            {
                                ctx.TextWriter.WriteLine(line);
                            }
                        }
                    }
                });

                File.Delete(Path.Combine(folder, MotiveAnalysisDescriptor.MotivesSourceFilename));

                lock (sync)
                {
                    numVisited++;
                    if (numVisited % 10 == 0) Log("Compressed {0}/{1} entries.", numVisited, FlatMotives.Length);
                }
            });
        }

        void UpdateApp()
        {
            App.SetCurrentAtlas(Atlas);
        }

        static string GetAtomString(IEnumerable<IAtom> atoms)
        {
            return PdbResidueCollection.GetExplicitlyCountedResidueString(atoms
               .GroupBy(a => a.ElementSymbol)
               .ToDictionary(g => g.Key.ToString(), g => g.Count(), StringComparer.Ordinal));
        }

        static string GetResidueIdentifierString(IEnumerable<PdbResidue> rs)
        {
            return string.Join("-", rs
                .OrderBy(r => r.ChainIdentifier)
                .ThenBy(r => r.Number)
                .Select(r => !string.IsNullOrWhiteSpace(r.ChainIdentifier)
                    ? string.Format("{0} {1} {2}", r.Name, r.Number, r.ChainIdentifier)
                    : string.Format("{0} {1}", r.Name, r.Number)));
        }

        static string GetUniqResiduesString(IEnumerable<string> rs)
        {
            return string.Join("-", rs
                .OrderBy(r => PdbResidue.IsAminoName(r) ? 1 : 0)
                .ThenBy(r => PdbResidue.GetShortAminoName(r))
                .Select(r => PdbResidue.GetShortAminoName(r)));
        }


        object motiveDescSync = new object();
        void CalcAmbientMotiveEntry(ProcessEntry entry)
        {
            List<MotiveAnalysisEntry> analyzed = new List<MotiveAnalysisEntry>(entry.BaseMotives.Count);
            var ambResidueCounts = new Dictionary<string, int>(StringComparer.Ordinal);

            var ambMotives = entry.MotiveGroups.First(g => g.Key == MotiveAnalysisDescriptor.AmbientQueryGroup).Value;

            int count = entry.BaseMotives.Count;
            for (int i = 0; i < count; i++)
            {
                var bm = entry.BaseMotives[i];
                var amb = ambMotives[i];
                
                var bmResidues = bm.PdbResidues();

                var tCounts = FrequencyCounter.Subtract(amb.PdbResidues().ResidueCounts, bmResidues.ResidueCounts);
                FrequencyCounter.AddIntoA(ambResidueCounts, tCounts);

                var e = new MotiveAnalysisEntry
                {
                    Id = bm.Id,
                    ParentId = entry.StructureId,

                    MotiveId = entry.Motive.Id,
                    SubCategoryId = entry.Motive.SubCategory.Id,
                    CategoryId = entry.Motive.SubCategory.Category.Id,
                                        
                    BaseAtomCount = bm.Atoms.Count,
                    BaseAtomString = GetAtomString(bm.Atoms),
                    BaseCountedResidueString = bmResidues.CountedShortAminoNamesString,
                    BaseUniqueResidueString = GetUniqResiduesString(bmResidues.ResidueCounts.Keys),
                    BaseResidueIndentifiers = GetResidueIdentifierString(bmResidues),

                    AmbientAtomCount = amb.Atoms.Count - bm.Atoms.Count,
                    AmbientAtomString = GetAtomString(amb.Atoms.Where(a => !bm.Atoms.Contains(a))),
                    AmbientCountedResidueString = PdbResidueCollection.GetCountedShortAminoNamesString(tCounts),
                    AmbientUniqueResidueString = GetUniqResiduesString(tCounts.Keys),
                    AmbientResidueIndentifiers = GetResidueIdentifierString(amb.PdbResidues().Where(r => bmResidues.FromIdentifier(r.Identifier) == null)),
                };

                analyzed.Add(e);
            }

            entry.MotiveAmbientResidueCounts = ambResidueCounts;
            entry.Entries = analyzed;
        }

        void Execute()
        {
            Log("Started analysis.");            
            var structures = Database.DefaultView.Snapshot().AsList();

            int numVisited = 0;
            object sync = new object();
            object logSync = new object();
            object counterSync = new object();
            object residueCounterSync = new object();

            using (Entries = new BlockingCollection<ProcessEntry>(CacheSize))
            using (var processor = Task.Factory.StartNew(Process))
            {
                try
                {
                    Parallel.ForEach(structures, new ParallelOptions { MaxDegreeOfParallelism = 4 }, s =>
                    {
                        LogFile("Analyzing {0}.", s.FilenameId);

                        try
                        {
                            var structureResult = s.ReadStructure();
                            var structure = structureResult.Structure;

                            lock (residueCounterSync)
                            {
                                Analysis.StructureResidueCounts.Add(structure.Id, structure.PdbResidues().ResidueCounts);
                            }

                            var context = ExecutionContext.Create(structure);

                            HashSet<string> warnedFormat = new HashSet<string>(StringComparer.Ordinal);
                            
                            foreach (var desc in FlatMotives)
                            {
                                if (!desc.ShouldVisitStructure(structure)) continue;

                                var baseMotives = desc.BaseQuery.Matches(context).Select(m => m.ToStructure("", true, true)).ToList();

                                if (baseMotives.Count > 0)
                                {
                                    ProcessEntry entry = null;

                                    try
                                    {
                                        entry = new ProcessEntry
                                        {
                                            StructureId = structure.Id,
                                            Motive = desc,
                                            BaseMotives = baseMotives,
                                            MotiveGroups = desc.QueryGroups.Select(g => new KeyValuePair<string, List<IStructure>>(g.Key, g.Value.Matches(context).Select(m => m.ToStructure("", true, true)).ToList())).ToArray()
                                        };
                                    }
                                    catch (Exception e)
                                    {
                                        Log("Error(MQ, {0}, '{1}'): {2}", desc.Id, structure.Id, e.Message);
                                        continue;
                                    }

                                    if (desc.QueryGroups.ContainsKey(MotiveAnalysisDescriptor.AmbientQueryGroup))
                                    {

                                        if (baseMotives.Count == entry.MotiveGroups.First(g => g.Key == MotiveAnalysisDescriptor.AmbientQueryGroup).Value.Count)
                                        {
                                            try
                                            {
                                                CalcAmbientMotiveEntry(entry);
                                                Entries.Add(entry);
                                            }
                                            catch (Exception e)
                                            {
                                                Log("Error({0}, '{1}'): {2}", desc.Id, structure.Id, e.Message);
                                            }
                                        }
                                        else
                                        {
                                            // else the structure is in bad format.
                                            lock (warnedFormat)
                                            {
                                                if (warnedFormat.Add(structure.Id)) Log("Warning({0}): '{1}' most likely in bad format.", desc.Id, structure.Id);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log("Error: {0}", e.Message);
                        }

                        lock (sync)
                        {
                            numVisited++;
                            if (numVisited % 25 == 0) Log("Analyzed {0}/{1} structures.", numVisited, structures.Count);
                        }
                    });
                }
                finally
                {
                    Entries.CompleteAdding();
                }

                try
                {
                    Log("Finished structure analysis.");
                    Log("Waiting for pending writes.");
                    processor.Wait();

                    ComputeAndExportSummary();

                    Log("Compressing data.");
                    Compress();

                    Log("Updating atlas app.");
                    UpdateApp();

                    Log("Done.");
                }
                catch (Exception e)
                {
                    Log("Error(post-processing): {0}", e.Message);
                }
            }
        }

        protected override void RunStandaloneInternal()
        {
            ServerManager.Init(StandaloneSettings.ServersConfigPath);

            var server = ServerManager.GetAppServer("Atlas");
            App = server.GetOrCreateApp<MotiveAtlasApp>("MotiveAtlas", _ => MotiveAtlasApp.Create(server.GetServerEntityId("MotiveAtlas")));
            Atlas = App.CreateNewAtlas();
            AtlasDataFolder = Atlas.Id.GetEntityPath();
            Log("Created atlas object, id = {0}.", Atlas.Id);

            Database = DatabaseInfo.Load(StandaloneSettings.DatabaseId);
            Log("Loaded database.");

            Analysis = AnalysisDescriptors.GetAtlasDescriptor(Database.Name);
            FlatMotives = Analysis.Categories.SelectMany(c => c.SubCategories.SelectMany(s => s.Motives)).ToArray();
            Log("Loaded descriptors.");

            Analysis.CreateDirectoryStructure(AtlasDataFolder);
            Log("Created directory structure.");

            WebChemistry.Queries.Core.Utils.CatalyticSiteAtlas.Init("CSA.dat");
            Log("Loaded CSA ({0} entries).", WebChemistry.Queries.Core.Utils.CatalyticSiteAtlas.GetSize());

            Execute();
        }

        public override AppVersion GetVersion()
        {
            return new AppVersion(0, 1, 23, 12, 27, 'b');
        }

        public override string GetName()
        {
            return "MotiveAtlas Analyzer";
        }

        protected override AtlasConfig SampleStandaloneSettings()
        {
            return new AtlasConfig
            {
                DatabaseId = EntityId.Parse("master:databases/pdb"),
                ServersConfigPath = @"c:\webchemservers.json"
            };
        }
    }
}
