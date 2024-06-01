namespace WebChemistry.MotiveValidator.Service
{
    using ICSharpCode.SharpZipLib.Zip;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Queries.Core;
    using WebChemistry.Queries.Core.Queries;
    using WebChemistry.MotiveValidator.DataModel;
    using WebChemistry.Platform.Services;

    partial class ValidatorService : ServiceBase<ValidatorService, MotiveValidatorConfig, MotiveValidatorStandaloneConfig, object>
    {
        public override AppVersion GetVersion()
        {
            return new AppVersion(1, 1, 23, 12, 27, 'b');
        }
        
        public int MaxDegreeOfParallelism = 8;

        MotiveValidationType ValidationType; 
        Dictionary<string, string> Errors = new Dictionary<string, string>();
        List<string> AnalyzedProteinIds = new List<string>();
        Dictionary<string, MotiveModel> Models = new Dictionary<string,MotiveModel>(StringComparer.Ordinal);
        string[] ModelNames = new string[0];
        bool SummaryOnly = false;

        Dictionary<string, ModelMetaInfo> ModelLongNames;

        void ReadLongNames()
        {
            if (ModelLongNames != null) return;

            var fn = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName, "ligandexpo.csv");
            try
            {
                ModelLongNames = WebChemistry.Framework.Core.Csv.CsvTable.ReadFile(fn).ToDictionary(r => r[0], r =>
                    new ModelMetaInfo
                    {
                        LongName = r["Name"],
                        ChargeNamingEquivalence = r["AtomEquivHybrid"],
                        ChargeNamingEquivalenceIgnoreBondTypes = r["AtomEquiv"],
                    }, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception e)
            {
                ModelLongNames = new Dictionary<string, ModelMetaInfo>();
                Log("Error reading LigExp info: {0}", e.Message);
            }
        }

        static Dictionary<string, string> ParseAtomEquiv(string str)
        {
            return str.ToUpperInvariant()
                .Split('-')
                .Select(g => g.Split(':').OrderBy(n => n, StringComparer.Ordinal).ToArray())
                .SelectMany(g => g.Select(n => new { Name = n, Key = g[0] }))
                .ToDictionary(p => p.Name, p => p.Key, StringComparer.OrdinalIgnoreCase);
        }

        static void FilterAtomEquiv(Dictionary<string, string> cls, MotiveModel model)
        {
            foreach (var a in model.Structure.Atoms)
            {
                if (a.ElementSymbol == ElementSymbols.H && cls.ContainsKey(a.PdbName()))
                {
                    cls.Remove(a.PdbName());
                }
            }
        }

        public void SetLongName(MotiveModel model)
        {            
            if (ModelLongNames.ContainsKey(model.Name))
            {
                var info = ModelLongNames[model.Name];
                model.LongName = info.LongName;
                model.AtomNamingEquivalence[AtomNamingEquivalenceType.Charge] = ParseAtomEquiv(info.ChargeNamingEquivalence);
                model.AtomNamingEquivalence[AtomNamingEquivalenceType.ChargeIgnoreBondTypes] = ParseAtomEquiv(info.ChargeNamingEquivalenceIgnoreBondTypes);

                FilterAtomEquiv(model.AtomNamingEquivalence[AtomNamingEquivalenceType.Charge], model);
                FilterAtomEquiv(model.AtomNamingEquivalence[AtomNamingEquivalenceType.ChargeIgnoreBondTypes], model);
            }
            else
            {
                model.LongName = "";                
            }
        }

        public void LogError(string file, string format, params object[] args)
        {
            var m = string.Format(format, args);
            Errors[file] = m;
            Log("{0}: {1}", file, m);
        }

        public void ShowError(string format, params object[] args)
        {
            Log(string.Format(format, args));
        }           

        void HandleWarnings(StructureReaderResult result, MotiveModel model)
        {
            if (result.Warnings.Count > 0)
            {
                model.AddWarnings(result.Structure.Id, result.Warnings);
            }
        }

        void HandleParentWarnings(StructureReaderResult result, MotiveModel model)
        {
            var warnings = result.Warnings.Where(w => !(w is ConectBondLengthReaderWarning)).ToArray();
            if (warnings.Length > 0)
            {
                model.AddWarnings(result.Structure.Id, warnings);
            }
        }

        void HandleChildWarnings(StructureReaderResult result, MotiveModel model, IStructure motif, out int alternateLocationCount)
        {
            List<StructureReaderWarning> warnings = new List<StructureReaderWarning>();
            alternateLocationCount = 0;

            foreach (var w in result.Warnings)
            {
                if (w is AtomStructureReaderWarning)
                {
                    if (motif.Atoms.Contains((w as AtomStructureReaderWarning).Atom)) warnings.Add(w);
                }
                else if (w is ResidueStructureReaderWarning)
                {
                    var rw = w as ResidueStructureReaderWarning;
                    if (motif.PdbResidues().FromIdentifier((rw).Id) != null)
                    {
                        if (rw.WarningType == ResidueStructureReaderWarningType.IgnoredAlternateLocation
                            || rw.WarningType == ResidueStructureReaderWarningType.ResiduesTooClose)
                        {
                            alternateLocationCount++;
                        }
                        warnings.Add(w);
                    }
                }
                else if (w is ConectBondLengthReaderWarning)
                {
                    var ww = w as ConectBondLengthReaderWarning;
                    if (motif.Atoms.Contains(ww.Atom1) && motif.Atoms.Contains(ww.Atom2)) warnings.Add(w);
                }
            }

            if (warnings.Count > 0)
            {
                model.AddWarnings(motif.Id, warnings);
            }
        }

        List<MotiveEntry> FindMotives(string folder)
        {
            List<MotiveEntry> ret = new List<MotiveEntry>();
            var query = QueryBuilder.Residues(ModelNames).Named().ConnectedAtoms(2, YieldNamedDuplicates: true).ToMetaQuery().Compile() as QueryMotive;

            foreach (var file in Directory.EnumerateFiles(folder).Where(f => StructureReader.IsStructureFilename(f)))
            {
                FileInfo fi = new FileInfo(file);
                try
                {
                    var sr = StructureReader.Read(file);

                    var s = FilterStructureAtoms(sr.Structure);
                    var residues = s.PdbResidues();
                    var matches = query.Matches(ExecutionContext.Create(s));

                    HashSet<MotiveModel> includedModels = new HashSet<MotiveModel>();

                    int index = 0;
                    foreach (var match in matches)
                    {
                        var residue = residues.FromAtom(s.Atoms.GetById(match.Name.Value));
                        var model = Models[residue.Name];
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
                    AnalyzedProteinIds.Add(s.Id);
                }
                catch (Exception e)
                {
                    LogError(fi.Name, e.Message);
                }
            }

            return ret;
        }

        List<MotiveEntry> FindMotives(List<StructureReaderResult> xs)
        {
            List<MotiveEntry> ret = new List<MotiveEntry>();
            var query = QueryBuilder.Residues(ModelNames).Named().ConnectedAtoms(2, YieldNamedDuplicates: true).ToMetaQuery().Compile() as QueryMotive;

            foreach (var sr in xs)
            {
                FileInfo fi = new FileInfo(sr.Filename);
                try
                {
                    var s = FilterStructureAtoms(sr.Structure);
                    var residues = s.PdbResidues();
                    var matches = query.Matches(ExecutionContext.Create(s));

                    HashSet<MotiveModel> includedModels = new HashSet<MotiveModel>();
                    int index = 0;
                    foreach (var match in matches)
                    {
                        var residue = residues.FromAtom(s.Atoms.GetById(match.Name.Value));
                        var model = Models[residue.Name];
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
                    AnalyzedProteinIds.Add(s.Id);
                }
                catch (Exception e)
                {
                    LogError(fi.Name, e.Message);
                }
            }

            return ret;
        }
        
        List<MotiveEntry> ReadMotives(string folder)
        {
            List<MotiveEntry> ret = new List<MotiveEntry>();
            var model = Models.Values.First();
            foreach (var file in Directory.EnumerateFiles(folder).Where(f => StructureReader.IsStructureFilename(f)))
            {
                try
                {
                    var sr = StructureReader.Read(file);
                    HandleWarnings(sr, model);

                    var s = FilterStructureAtoms(sr.Structure);
                    //var nameIndex = s.Id.IndexOf("_");
                    var entry = new MotiveEntry
                    {
                        Model = model,
                        //ParentId = s.Id.Substring(0, nameIndex > 0 ? nameIndex : s.Id.Length),
                        Motive = s,
                        //Id = s.Id.Substring(nameIndex > 0 ? nameIndex + 1 : 0)
                    };

                    ret.Add(entry);
                }
                catch (Exception e)
                {
                    LogError(new FileInfo(file).Name, e.Message);
                }
            }

            return ret;
        }

        List<MotiveEntry> WrapMotives(List<StructureReaderResult> xs)
        {
            List<MotiveEntry> ret = new List<MotiveEntry>();
            var model = Models.Values.First();
            foreach (var sr in xs)
            {
                HandleWarnings(sr, model);
                var s = FilterStructureAtoms(sr.Structure);

                //var nameIndex = s.Id.IndexOf("_");
                var entry = new MotiveEntry
                {
                    Model = model,
                    //ParentId = s.Id.Substring(0, nameIndex > 0 ? nameIndex : s.Id.Length),
                    Motive = s,
                    //Id = s.Id.Substring(nameIndex > 0 ? nameIndex + 1 : 0)
                };

                ret.Add(entry);
            }

            return ret;
        }

        List<StructureReaderResult> GetStructuresFromZip(string filename)
        {
            List<StructureReaderResult> ret = new List<StructureReaderResult>();
            try
            {
                using (var zip = new ZipFile(filename))
                {
                    foreach (ZipEntry entry in zip)
                    {
                        try
                        {
                            if (entry.IsDirectory || !StructureReader.IsStructureFilename(entry.Name)) continue;
                            var s = StructureReader.Read(entry.Name, () => zip.GetInputStream(entry));
                            
                            ret.Add(s);
                        }
                        catch (Exception e)
                        {
                            LogError(entry.Name, e.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(new FileInfo(filename).Name, "Reading motifs: {0}", ex.Message);
            }
            return ret;
        }

        List<MotiveEntry> GetMotives()
        {
            if (IsStandalone)
            {
                if (StandaloneSettings.ValidationType == MotiveValidationType.Sugars
                    || StandaloneSettings.ValidationType == MotiveValidationType.CustomModels)
                {
                    return FindMotives(StandaloneSettings.InputFolder);
                }
                return ReadMotives(StandaloneSettings.InputFolder);                
            }

            //var filename = Path.Combine(Settings.InputRepository.GetEntityPath(), "motives.zip");
            var filename = Path.Combine(Computation.GetInputDirectory(), "motives.zip");
            if (Settings.ValidationType == MotiveValidationType.Sugars
                || Settings.ValidationType == MotiveValidationType.CustomModels)
            {
                return FindMotives(GetStructuresFromZip(filename));
            }
            return WrapMotives(GetStructuresFromZip(filename));
        }

        Dictionary<string, MotiveEntry[]> ReadMotives()
        {
            var motives = GetMotives();

            return motives
                .GroupBy(m => m.Model.Name)
                .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);
        }
        
        public static IStructure FilterStructureAtoms(IStructure s)
        {
            if (s.Atoms.Count(a => a.ElementSymbol == ElementSymbols.H) == 0) return s;
            //return s.InducedSubstructure(s.Id, s.Atoms.Where(a => a.ElementSymbol != ElementSymbols.H && !a.IsWater()));
            return s.WithoutHydrogens();
        }
        
        ModelLoader CreateModelLoader()
        {
            if (IsStandalone)
            {
                if (StandaloneSettings.ModelsSource.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return ModelLoader.FromZip(this, StandaloneSettings.ModelsSource);
                if (StandaloneSettings.IsModelsSourceComponentDictionary) return ModelLoader.FromComponentDictionaryFile(StandaloneSettings.ModelsSource, StandaloneSettings.IgnoreObsoleteComponentDictionaryEntries, this);
                return ModelLoader.FromFolder(this, StandaloneSettings.ModelsSource);
            }
            else
            {
                if (ValidationType == MotiveValidationType.Sugars)
                {
                    return ModelLoader.FromHostedSugars(this, Settings);
                }
                else
                {
                    return ModelLoader.FromZip(this, Directory.EnumerateFiles(Computation.GetInputDirectory(), "models.zip").First());
                }
            }
        }


        void ReadModels()
        {
            var loader = CreateModelLoader();
            var models = loader.ReadModels();

            if (ValidationType == MotiveValidationType.Model)
            {
                if (models.Count != 1)
                {
                    LogError("models", "Wrong model count was provided for Model analysis. Got {0}, expected 1.", models.Count);
                    return;
                }
            }

            this.Models = models;
            this.ModelNames = this.Models.Keys.OrderBy(k => k).ToArray();            
        }

        static string ErrorsToCsv(Dictionary<string, string> errors)
        {
            return errors
                .OrderBy(e => e.Key)
                .Select(e => new { e.Key, e.Value })
                .GetExporter()
                .AddExportableColumn(e => e.Key, Framework.Core.ColumnType.String, "Key")
                .AddExportableColumn(e => e.Value, Framework.Core.ColumnType.String, "Message")
                .ToCsvString();
        }

        static string WarningsToCsv(Dictionary<string, string[]> warnings)
        {
            return warnings
                .OrderBy(e => e.Key)
                .SelectMany(e => e.Value.Select(w => new { e.Key, Value = w }))
                .GetExporter()
                .AddExportableColumn(e => e.Key, Framework.Core.ColumnType.String, "Key")
                .AddExportableColumn(e => e.Value, Framework.Core.ColumnType.String, "Message")
                .ToCsvString();
        }
        
        ValidationResult MakeResult(GroupAnalysis[] analyzedGroups, MotiveEntry[] allEntries, bool ignoreErrors = false)
        {
            return new ValidationResult
            {
                Version = GetVersion(),
                Errors = ignoreErrors ? new Dictionary<string, string>() : Errors,
                Models = analyzedGroups
                    .Select(g => new ModelValidationEntry
                    {
                        ModelName = g.Model.Name,
                        LongName = g.Model.LongName,
                        Formula = g.Model.Formula,
                        CoordinateSource = g.Model.SourceType.ToString(),
                        StructureNames = g.StructureNames,
                        NotAnalyzedNames = g.Entries.Where(e => !e.IsAnalyzed).Select(e => e.Motive.Id).OrderBy(n => n).ToArray(),
                        Entries = g.AnalyzedEntries.Select(e => e.Result).ToArray(),
                        Summary = g.Summary,
                        ChiralAtoms = g.Model.ChiralAtoms.Select(a => a.Id).ToArray(),
                        ChiralAtomsInfo = g.Model.ChiralAtomsInfo,
                        AtomNamingEquivalence = g.Model.AtomNamingEquivalence,
                        PlanarAtoms = g.Model.NearPlanarAtoms.Select(a => a.Id).OrderBy(a => a).ToArray(),
                        ModelNames = g.ModelNames,
                        ModelAtomTypes = g.ModelAtomTypes,
                        ModelBonds = g.ModelBonds,
                        Errors = g.Model.MotiveErrors,
                        Warnings = g.Model.MotiveWarnings
                    })
                    .ToArray(),
                ValidationType = IsStandalone ? StandaloneSettings.ValidationType : Settings.ValidationType,
                MotiveCount = allEntries.Length
            };
        }

        static ModelValidationEntry CloneModelEntry(ModelValidationEntry entry)
        {
            return new ModelValidationEntry
            {
                ModelName = entry.ModelName,
                LongName = entry.LongName,
                Formula = entry.Formula,
                CoordinateSource = entry.CoordinateSource,
                StructureNames = entry.StructureNames.ToArray(),
                NotAnalyzedNames = entry.NotAnalyzedNames.ToArray(),
                Entries = entry.Entries.ToArray(),
                Summary = entry.Summary.ToDictionary(s => s.Key, s => s.Value, entry.Summary.Comparer),
                ChiralAtoms = entry.ChiralAtoms,
                ChiralAtomsInfo = entry.ChiralAtomsInfo,
                AtomNamingEquivalence = entry.AtomNamingEquivalence,
                PlanarAtoms = entry.PlanarAtoms,
                ModelNames = entry.ModelNames,
                ModelAtomTypes = entry.ModelAtomTypes,
                ModelBonds = entry.ModelBonds,
                Errors = entry.Errors.ToDictionary(s => s.Key, s => s.Value, entry.Errors.Comparer),
                Warnings = entry.Warnings.ToDictionary(s => s.Key, s => s.Value, entry.Warnings.Comparer),
            };
        }

        void InitAndRun()
        {
            //Environment.SetEnvironmentVariable("BABEL_DATADIR", @"C:\ProgramData\OpenBabel-2.3.1\data__", EnvironmentVariableTarget.User);
            //Converter = new OBConversion();
            //Converter.SetInFormat("pdb");
            //Converter.SetOutFormat("mol");
            //Converter = new StructureConverter(MaxDegreeOfParallelism);
            //Log("Degree of parallelism: {0}", MaxDegreeOfParallelism);

            try
            {
                var timer = Stopwatch.StartNew();

                if (!IsStandalone && ValidationType == MotiveValidationType.Database)
                {
                    RunCustomAggregate();
                }
                else
                {
                    ReadLongNames();
                    if (ValidationType != MotiveValidationType.Database) RunSingle();
                    else RunDatabase();
                }

                timer.Stop();
                Log("Done in {0}.", timer.Elapsed);
            }
            finally
            {
                //Converter.Dispose();
            }
        }

        protected override void RunHostedInternal()
        {
            ValidationType = Settings.ValidationType;
            MaxDegreeOfParallelism = Settings.MaxDegreeOfParallelism;
            InitAndRun();
        }

        protected override void RunStandaloneInternal()
        {
            ValidationType = StandaloneSettings.ValidationType;
            SummaryOnly = StandaloneSettings.SummaryOnly;
            MaxDegreeOfParallelism = StandaloneSettings.MaxDegreeOfParallelism;
            InitAndRun();
        }

        public override string GetName()
        {
            return "MotiveValidator";
        }

        public override void UpdateHelpConfig(ServiceHelpConfig config)
        {
            config.RunningRemarks = new string [0];

            config.OutputStructure
                .AddSpecificFileStructure(
                    HelpOutputSpecificFileDescription.Csv("index.csv",
                    "Contains complete information about the result of validation of each motif in a compressed manner.",
                    MotiveEntry.GetResultExporter(new ValidationResultEntry[0], new Dictionary<string,MotiveModel>())))
                .AddSpecificFileStructure(
                    HelpOutputSpecificFileDescription.Generic("index.json", 
                    "File that contains complete information about the result of validation of each motif, as well as list of warnings and errors, list of general errors, " +
                    "number of validated motifs, validation type and MotiveValidator version. This file is used to display the data in the web UI." + Environment.NewLine + Environment.NewLine +
                    "In .NET languages, the file can be parsed into the type WebChemistry.MotiveValidator.DataModel.ValidationResult from the library 'WebChemistry.MotiveValidator.DataModel' using the " +
                    "Newtonsoft.Json or similar library. Other straightforward analysis tool for this file is JavaScript (for example in browser or using Node.js)."));
        }

        protected override HelpOutputStructureDescription MakeOutputStructure()
        {
            return  HelpFolder("result", "Folder with computation result.",
                      HelpFile("result.zip", "A zip archive with compressed result.",
                        HelpFile("result.csv", "File that contains complete information about the result of validation of each motif in a compressed manner."),
                        HelpFile("result.json", "File that contains complete information about the result of validation of each motif, as well as list of warnings and errors, list of general errors, number of validated motifs, validation type and MotiveValidator version."),
                        HelpFile("general_errors.csv", "File that contains list of general errors."),
                        HelpFolder("[3-letter residue code]", "A folder for each input model.",
                          HelpFolder("matched", "Folder that contains pairs of validated motif and aligned model, both in PDB format."),
                          //HelpFolder("mols", "Folder that contains validated motifs in mol format."),
                          HelpFolder("json", "Folder that contains validated motifs in JSON format."),
                          HelpFolder("motifs", "Folder that contains validated motifs in PDB format."),
                          HelpFolder("notanalyzed", "Folder that contains motifs that were not validated."),
                          HelpFile("errors.csv", "File that contains list of errors."),
                          HelpFile("pairing.csv", "File that contains pairing matrix of motifs' atoms versus model's atoms."),
                          HelpFile("summary.json", "File that contains rich summary of each single validation run."),
                          HelpFile("[3-letter residue code]_model.mol", "Mol file that contains the model."),
                          HelpFile("[3-letter residue code]_model.pdb", "PDB file that contains the model."),
                          HelpFile("warnings.csv", "File that contains list of warnings."))));
        }

        protected override MotiveValidatorStandaloneConfig SampleStandaloneSettings()
        {
            return new MotiveValidatorStandaloneConfig 
            {
                InputFolder = @"./MAN",
                ModelsSource = @"./MAN_Models",
                ValidationType = MotiveValidationType.Model,
                SummaryOnly = false
            };
        }
    }
}
