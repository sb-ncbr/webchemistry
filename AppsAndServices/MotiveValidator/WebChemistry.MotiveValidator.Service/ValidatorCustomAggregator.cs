namespace WebChemistry.MotiveValidator.Service
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Csv;
    using WebChemistry.MotiveValidator.Database;
    using WebChemistry.MotiveValidator.DataModel;
    using WebChemistry.Platform;
    using WebChemistry.Platform.Server;
    using WebChemistry.Platform.Services;

    partial class ValidatorService : ServiceBase<ValidatorService, MotiveValidatorConfig, MotiveValidatorStandaloneConfig, object>
    {
        static Func<string, bool> CreateMemberPredicate(HashSet<string> set)
        {
            if (set.Count == 0) return _ => true;
            return s => set.Contains(s);
        }

        static string CreateAggregateStructureSummaryCsv(Dictionary<string, ValidationResult> entries, Dictionary<string, Dictionary<string, int>> summary, Dictionary<string, string> atomCounts)
        {
            var exp = entries
                .OrderBy(m => m.Key, StringComparer.Ordinal)
                .Select(m => new { Name = m.Key, Validation = m.Value, Summary = summary[m.Key] })
                .ToArray().GetExporter()
                .AddExportableColumn(m => m.Name, ColumnType.String, "Id")
                .AddExportableColumn(m => m.Validation.Models.JoinBy(x => x.ModelName, " "), ColumnType.String, "Models")
                .AddExportableColumn(m => atomCounts.ContainsKey(m.Name) ? atomCounts[m.Name] : "0", ColumnType.Number, "AtomCount");

            var template = summary.Values.FirstOrDefault(s => s.Count != 0);
            if (template != null)
            {
                foreach (var c in template.Keys.OrderBy(k => k))
                {
                    var key = c;
                    exp.AddExportableColumn(m => m.Summary.ContainsKey(key) ? m.Summary[key] : 0, ColumnType.Number, c);
                }
            }

            return exp.ToCsvString();
        }

        struct ModelEntryInfo
        {
            public string AtomCount, ChiralAtomCount;
        }

        static string CreateAggregateModelSummaryCsv(Dictionary<string, ValidationResult> entries, Dictionary<string, ModelEntryInfo> infos)
        {
            var exp = entries
                .OrderBy(m => m.Key, StringComparer.Ordinal)
                .Select(m => new { Name = m.Key, Validation = m.Value, Summary = m.Value.Models[0].Summary })
                .ToArray().GetExporter()
                .AddExportableColumn(m => m.Name, ColumnType.String, "Name")
                .AddExportableColumn(m => infos.ContainsKey(m.Name) ? infos[m.Name].AtomCount : "0", ColumnType.Number, "AtomCount")
                .AddExportableColumn(m => infos.ContainsKey(m.Name) ? infos[m.Name].ChiralAtomCount : "0", ColumnType.Number, "ChiralAtomCount")
                .AddExportableColumn(m => m.Validation.Models[0].StructureNames.Length, ColumnType.Number, "StructureCount");

            var template = entries.Values.FirstOrDefault(m => m.Models[0].Summary.Count != 0);
            if (template != null)
            {
                foreach (var c in template.Models[0].Summary.Keys.OrderBy(k => k))
                {
                    var key = c;
                    exp.AddExportableColumn(m => m.Summary.ContainsKey(key) ? m.Summary[key] : 0, ColumnType.Number, c);
                }
            }

            return exp.ToCsvString();
        }

        void RunCustomAggregate()
        {
            UpdateProgress("Initializing...", forceSave: true);
            Log("Initializing...");
            var app = ServerManager.GetAppServer(Settings.DatabaseModeServerName).GetApp<MotiveValidatorDatabaseApp>(Settings.DatabaseModeAppName);

            var structureSet = Settings.DatabaseModeCustomStructureIds == null ? new HashSet<string>() : Settings.DatabaseModeCustomStructureIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var modelSet = Settings.DatabaseModeCustomModelIds == null ? new HashSet<string>() : Settings.DatabaseModeCustomModelIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var structurePredicate = CreateMemberPredicate(structureSet);
            var modelPredicate = CreateMemberPredicate(modelSet);

            var visitedStructures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visitedModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Dictionary<string, Dictionary<string, int>> byStructureSummary = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
            Dictionary<string, ValidationResult> byStructure = new Dictionary<string, ValidationResult>(StringComparer.Ordinal);
            Dictionary<string, int> summary = new Dictionary<string, int>(StringComparer.Ordinal);
            Dictionary<string, ValidationResult> byModel = new Dictionary<string,ValidationResult>(StringComparer.Ordinal);
            using (var db = new MotiveValidatorDatabaseDataInterface(app, false))
            {
                var names = db.GetEntryNames().Where(e => e.StartsWith("by_structure", StringComparison.OrdinalIgnoreCase) && e.EndsWith("result.json", StringComparison.OrdinalIgnoreCase)).ToList();

                int visited = 0;

                UpdateProgress("Merging results...", current: 0, max: names.Count, forceSave: true);
                foreach (var e in names)
                {
                    visited++;

                    if (visited % 500 == 0)
                    {
                        Log("Visited {0} / {1} entries...", visited, names.Count);
                    }

                    if (visited % 250 == 0)
                    {
                        UpdateProgress("Merging results...", current: visited, max: names.Count);
                    }
                    
                    var id = new DirectoryInfo(e).Parent.Name;
                    visitedStructures.Add(id);
                    if (!structurePredicate(id)) continue;

                    var result = JsonHelper.FromJsonString<ValidationResult>(db.GetEntry(e));                    
                    result.Models = result.Models.Where(m => modelPredicate(m.ModelName)).ToArray();

                    // If no model is present, do not include the structure.
                    if (result.Models.Length == 0) continue;

                    result.MotiveCount = result.Models.Sum(m => m.Entries.Length);
                    byStructure.Add(id, result);

                    Dictionary<string, int> structureSummary = new Dictionary<string, int>(StringComparer.Ordinal);

                    foreach (var m in result.Models)
                    {
                        visitedModels.Add(m.ModelName);
                        DatabaseAggregator.MergeSummary(structureSummary, m.Summary);

                        // Create the result for a single model.
                        var modelResult = new ValidationResult
                        {
                            Version = result.Version,
                            ValidationType = result.ValidationType,
                            MotiveCount = m.Entries.Length,
                            Errors = result.Errors,
                            Models = new[] { m }
                        };

                        // Merge the models.
                        ValidationResult mr;
                        if (byModel.TryGetValue(m.ModelName, out mr))
                        {
                            MotiveValidatorDatabaseApp.MergeValidations(mr, modelResult);
                        }
                        else
                        {
                            modelResult.Models = new[] { CloneModelEntry(modelResult.Models[0]) };
                            byModel.Add(m.ModelName, modelResult);
                        }
                    }

                    byStructureSummary.Add(id, structureSummary);
                    DatabaseAggregator.MergeSummary(summary, structureSummary);
                }
            }

            UpdateProgress("Exporting...", forceSave: true);
            Log("Exporting results...");
            using (var zip = ZipUtils.CreateZip(Path.Combine(ResultFolder, "data.zip")))
            {
                foreach (var v in byStructure) zip.AddEntry(Path.Combine("by_structure", v.Key, "result.json"), v.Value.ToJsonString());
                foreach (var v in byModel) zip.AddEntry(Path.Combine("by_model", v.Key, "result.json"), v.Value.ToJsonString());
            }

            var unknownStructures = new string[0];
            if (structureSet.Count > 0)
            {
                unknownStructures = structureSet.Where(s => !visitedStructures.Contains(s)).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
            }

            var unknownModels = new string[0];
            if (modelSet.Count > 0)
            {
                unknownModels = modelSet.Where(s => !visitedModels.Contains(s)).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
            }

            File.WriteAllText(Path.Combine(ResultFolder, "missing_data.json"), new { Structures = unknownStructures, Models = unknownModels }.ToJsonString());

            var atomsCounts = CsvTable.ReadFile(Path.Combine(app.GetCurrentDatabasePath(), "structure_summary.csv")).ToDictionary(e => e["Id"], e => e["AtomCount"], StringComparer.OrdinalIgnoreCase);
            var csvByStructure = CreateAggregateStructureSummaryCsv(byStructure, byStructureSummary, atomsCounts);
            File.WriteAllText(Path.Combine(ResultFolder, "structure_summary.csv"), csvByStructure);

            var modelInfos = CsvTable.ReadFile(Path.Combine(app.GetCurrentDatabasePath(), "model_summary.csv")).ToDictionary(
                e => e["Name"],
                e => new ModelEntryInfo { AtomCount = e["AtomCount"], ChiralAtomCount = e["ChiralAtomCount"] }, 
                StringComparer.OrdinalIgnoreCase);
            var csvByModel = CreateAggregateModelSummaryCsv(byModel, modelInfos);
            File.WriteAllText(Path.Combine(ResultFolder, "model_summary.csv"), csvByModel);

            File.WriteAllText(Path.Combine(ResultFolder, "summary.json"), summary.ToJsonString());
            
            File.WriteAllText(Path.Combine(ResultFolder, "stats.json"), new
            {
                MotiveCount = summary.DefaultIfNotPresent("Analyzed") + summary.DefaultIfNotPresent("NotAnalyzed"),
                StructureCount = byStructure.Count,
                ModelCount = byModel.Count,
                MissingStructureCount = unknownStructures.Length,
                MissingModelCount = unknownModels.Length
            }.ToJsonString());

            UpdateProgress("Done.", forceSave: true);
            Log("Export complete.");
        }
    }
}
