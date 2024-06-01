namespace WebChemistry.Charges.Service.Analyzer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using WebChemistry.Platform.Services;
    using WebChemistry.Charges.Service.DataModel;
    using WebChemistry.Framework.Core;
    using Newtonsoft.Json;
    using System.IO;
    using WebChemistry.Charges.Core;
    using System.Xml.Linq;
    using WebChemistry.Platform;
    using WebChemistry.Framework.Core.Collections;
    using ICSharpCode.SharpZipLib.Zip;

    class AnalyzerService : ServiceBase<AnalyzerService, ChargesAnalyzerConfig, ChargesAnalyzerStandaloneConfig, object>
    {
        public override AppVersion GetVersion()
        {
            return new AppVersion(1, 0, 23, 12, 27, 'b');
        }

        public override string GetName()
        {
            return "Charges.Analyzer";
        }
        
        string InputFolder;
        
        ZipUtils.ZipWrapper InputData, StructureJsonData;

        int CurrentProgress, TotalProgress;

        List<ChargesAnalyzerResultEntry> Entries;
        ChargeAnalyzerParameterSetEntry[] ParameterSets;
        List<string> GeneralErrors;
        List<string> GeneralWarnings;
        HashSet<string> UsedStructureIds, MultipleStructureIdWarning;
        
        void ProcessStructure(ServiceInputEntryProvider structureInput, ServiceInputEntryProvider[] refChargesInput)
        {
            var filenameId = StructureReader.GetStructureIdFromFilename(structureInput.Filename);
            Log("Processing {0}...", filenameId);

            if (UsedStructureIds.Contains(filenameId))
            {
                MultipleStructureIdWarning.Add(filenameId);
                return;
            }
            
            UpdateProgress(string.Format("Analyzing {0}...", filenameId), ++CurrentProgress, TotalProgress);
            
            ChargesAnalyzerResultEntry entry;
            try
            {
                //var src = structureInput.GetContent();
                var sr = StructureReader.Read(structureInput.Filename, structureInput.StreamProvider);
                UpdateStatus();
                var structure = sr.Structure;

                var usedReferenceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var referenceFiles = new List<ServiceInputEntryProvider>();
                var referenceWarnings = new List<string>();

                bool missingHydrogens = structure.Atoms.Count(a => a.ElementSymbol == ElementSymbols.H) == 0;
                var unknownAtoms = structure.Atoms.Where(a => !ElementSymbol.IsKnownSymbol(a.ElementSymbol.ToString())).ToArray();
                //var strangeResidues = structure.PdbResidues()
                //    .Where(r => r.Atoms.Select(a => a.PdbResidueName()).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                //    .Select(r => new { Id = r.Identifier, Names = r.Atoms.Select(a => a.PdbResidueName()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() })
                //    .ToArray();

                foreach (var rc in refChargesInput)
                {
                    var name = AnalyzerUtils.GetRefChargesName(rc.Filename);
                    if (!usedReferenceIds.Add(name))
                    {
                        referenceWarnings.Add(string.Format("{0}: Duplicate filename.", rc.Filename));
                        continue;
                    }

                    try
                    {
                        ChargeComputationResult.FromReference(rc.Filename, structure, rc.StreamProvider);
                        referenceFiles.Add(rc);
                    }
                    catch (Exception e)
                    {
                        referenceWarnings.Add(string.Format("{0}: {1}", rc.Filename, e.Message));
                    }
                }

                var warnings = new List<string>();
                if (unknownAtoms.Length > 0) 
                {
                    var theAtoms = unknownAtoms.Select(a => a.ElementSymbol.ToString()).Distinct(StringComparer.Ordinal).OrderBy(a => a, StringComparer.Ordinal);
                    warnings.Add(string.Format("The file contains {1} atoms with unknown chemical element names ({0}). These atoms will be skipped unless you add EEM parameters for them.",
                        theAtoms.JoinBy(), unknownAtoms.Length));
                }
                //if (strangeResidues.Length > 0)
                //{
                //    warnings.Add(string.Format("The molecule contains residue(s) with the same identifier but multiple names. Affected residue(s): {0}",
                //        strangeResidues.JoinBy(r => string.Format("{0} ({1})", r.Id, r.Names.JoinBy()), "; ")));
                //}
                if (missingHydrogens) warnings.Add("The molecule is missing hydrogen atoms. Is this intended?");
                warnings.AddRange(sr.Warnings.Select(w => w.ToString()));

                entry = new ChargesAnalyzerResultEntry
                {
                    StructureId = structure.Id,

                    AtomCounts = structure.Atoms.GroupBy(a => a.ElementSymbol).ToDictionary(g => g.Key.ToString(), g => g.Count(), StringComparer.Ordinal),
                    BondCount = structure.Bonds.Count,
                    StructureCategories = AnalyzerUtils.GetStructureCategories(structure),
                    SuggestedCharge = AnalyzerUtils.EstimateTotalCharge(structure),

                    HasBuiltInReferenceCharges = structure.Mol2ContainsCharges() || structure.PqrContainsCharges(),
                    ReferenceChargeFilenames = referenceFiles.Select(e => e.Filename).ToArray(),
                    ReferenceChargeWarnings = referenceWarnings.ToArray(),

                    IsValid = true,
                    Warnings = warnings.ToArray()
                };
                UsedStructureIds.Add(filenameId);
                InputData.AddEntry(Path.Combine(StructureReader.GetStructureIdFromFilename(structureInput.Filename), structureInput.Filename), structureInput.StreamProvider);

                UpdateStatus();
                foreach (var rc in referenceFiles)
                {
                    InputData.AddEntry(Path.Combine(structure.Id, rc.Filename), rc.StreamProvider);
                }
                UpdateStatus();

                var json = structure.ToJsonString();
                StructureJsonData.AddEntry(structure.Id + ".json", json);
            }
            catch (Exception e)
            {
                entry = new ChargesAnalyzerResultEntry
                {
                    StructureId = filenameId,
                    IsValid = false,
                    ErrorText = e.Message
                };
            }
            Entries.Add(entry);
            UpdateStatus();
        }
        
        void ProcessStructures()
        {
            UpdateProgress("Processing input data...");
            using (var input = ServiceInputProvider.FromFolder(InputFolder))
            {
                CurrentProgress = 0;
                var structures = input.GetStructureEntries();
                TotalProgress = structures.Count;

                foreach (var s in structures)
                {
                    var refs = input.GetPrefixEntries(s.StructureFilenameId + "_ref_").Where(e => AnalyzerUtils.IsReferenceChargesFilename(e.Filename)).ToArray();
                    ProcessStructure(s, refs);
                }
            }
        }

        void ExportResultJson()
        {
            var result = new ChargesAnalyzerResult
            {
                Entries = Entries.ToArray(),
                Errors = GeneralErrors.ToArray(),
                Warnings = GeneralWarnings.ToArray(),
                ParameterSets = ParameterSets,
                EmptySetTemplate = AnalyzerUtils.ConvertSetToEntry(EemParameterSet.NewSet())
            };
            JsonHelper.WriteJsonFile(Path.Combine(ResultFolder, "result.json"), result);
        }

        void ExportGeneralFail(string errorMessage)
        {
            var result = new ChargesAnalyzerResult
            {
                Entries = new ChargesAnalyzerResultEntry[0],
                Errors = new [] { errorMessage },
                Warnings = new string[0],
                ParameterSets = new ChargeAnalyzerParameterSetEntry[0],
                EmptySetTemplate = null
            };
            JsonHelper.WriteJsonFile(Path.Combine(ResultFolder, "result.json"), result);
        }

        void DoParameterSets()
        {
            var filename = Path.Combine((new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location)).Directory.FullName, "DefaultSets.wxml");
            var manager = new ParameterSetManager();
            manager.Update(XElement.Load(filename));
            ParameterSets = manager.Sets.Select(s => AnalyzerUtils.ConvertSetToEntry(s)).ToArray();
        }

        void RunAnalysis()
        {            
            Entries = new List<ChargesAnalyzerResultEntry>();
            ParameterSets = new ChargeAnalyzerParameterSetEntry[0];
            GeneralErrors = new List<string>();
            GeneralWarnings = new List<string>();
            UsedStructureIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            MultipleStructureIdWarning = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        
            try
            {
                ProcessStructures();
            }
            catch (Exception e)
            {
                var msg = string.Format("Error processing input: {0}", e.Message);
                Log(msg);
                GeneralErrors.Add(msg);
            }

            Log("Loading parameter sets...");
            UpdateProgress("Loading parameter sets...", forceSave: true);
            
            try
            {
                DoParameterSets();
            }
            catch (Exception e)
            {
                var msg = string.Format("Error loading parameter sets: {0}", e.Message);
                Log(msg);
                GeneralErrors.Add(msg);
            }

            Log("Exporting results...");
            UpdateProgress("Exporting...", forceSave: true);
            ExportResultJson();
            Log("Done.");
        }
                
        string GetInputZipFilename()
        {
            if (IsStandalone) return Path.Combine(ResultFolder, "input.zip");
            return Path.Combine(Platform.Computation.ComputationInfo.Load(Computation.GetCustomState<ChargeAnalysisState>().ChargeComputationId).GetInputDirectory(), "input.zip");
        }

        void RunGuard()
        {
            try
            {
                using (StructureJsonData = ZipUtils.CreateZip(Path.Combine(ResultFolder, "structures-json.zip")))
                using (InputData = ZipUtils.CreateZip(GetInputZipFilename()))
                {
                    RunAnalysis();
                }
            }
            catch (Exception e)
            {
                Log("Fatal error: {0}", e.Message);
                ExportGeneralFail(e.Message);
            }
        }

        protected override void RunHostedInternal()
        {
            InputFolder = this.Computation.GetInputDirectory();
            RunGuard();
        }

        protected override void RunStandaloneInternal()
        {
            InputFolder = StandaloneSettings.InputFolder;
            RunGuard();
        }

        protected override ChargesAnalyzerStandaloneConfig SampleStandaloneSettings()
        {
            return new ChargesAnalyzerStandaloneConfig
            {
                InputFolder = "C:\\Data\\Charges\\InputStructures"
            };
        }
    }
}
