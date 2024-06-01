namespace WebChemistry.MotiveValidator.Service
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.MdlMol;
    using WebChemistry.MotiveValidator.DataModel;

    class DatabaseAggregator
    {
        public class ModelWrapper
        {
            public MotiveModel Model;

            public Dictionary<string, int> Summary;
            public Dictionary<int, int> ChiralityAnalysis, MissingAtomAnalysis;
            //public int ChiralityTotalCount, MissingAtomTotalCount;

            public List<string> NotAnalyzedNames, StructureNames;

            public List<ValidationResultEntry> Entries;

            public Dictionary<int, string> ModelNames;
            public Dictionary<int, string> ModelAtomTypes;
            public int[] ModelChiralAtoms;
            public Dictionary<string, int[]> ModelChiralAtomsInfo;
            public Dictionary<string, Dictionary<string, string>> ModelAtomNamingEquivalence;
            public Dictionary<string, int> ModelBonds;

            public string SourceType;
            
            public string GetChiralityAnalysisString()
            {
                var atoms = Model.Structure.Atoms;
                return ChiralityAnalysis.JoinBy(a => atoms.GetById(a.Key).PdbName() + ":" + a.Value, by: " ");
            }

            public string GetMissingAtomsAnalysisString()
            {
                var atoms = Model.Structure.Atoms;
                return MissingAtomAnalysis.JoinBy(a => atoms.GetById(a.Key).PdbName() + ":" + a.Value, by: " ");
            }

            public ModelWrapper()
            {
                Summary = new Dictionary<string, int>(StringComparer.Ordinal);
                ChiralityAnalysis = new Dictionary<int, int>();
                MissingAtomAnalysis = new Dictionary<int, int>();
                Entries = new List<ValidationResultEntry>();
                NotAnalyzedNames = new List<string>();
                StructureNames = new List<string>();
            }

            public ValidationResult MakeResults(AppVersion version)
            {
                return new ValidationResult
                {
                    Version = version,
                    Errors = new Dictionary<string, string>(),
                    Models = new ModelValidationEntry[]
                    { 
                        new ModelValidationEntry
                        {
                            ModelName = Model.Name,
                            LongName = Model.LongName,
                            Formula = Model.Formula,
                            CoordinateSource = Model.SourceType.ToString(),
                            StructureNames = StructureNames.OrderBy(n => n, StringComparer.Ordinal).ToArray(),
                            NotAnalyzedNames = NotAnalyzedNames.OrderBy(n => n, StringComparer.Ordinal).ToArray(),
                            Entries = Entries.OrderBy(n => n.Id, StringComparer.Ordinal).ToArray(),
                            Summary = Summary,
                            PlanarAtoms = Model.NearPlanarAtoms.Select(a => a.Id).OrderBy(a => a).ToArray(),
                            ChiralAtoms = ModelChiralAtoms ?? new int[0],
                            ChiralAtomsInfo = ModelChiralAtomsInfo ?? new Dictionary<string, int[]>(StringComparer.Ordinal),
                            AtomNamingEquivalence = ModelAtomNamingEquivalence ?? new Dictionary<string,  Dictionary<string, string>>(StringComparer.Ordinal),
                            ModelNames = ModelNames ?? new Dictionary<int, string>(),
                            ModelAtomTypes = ModelAtomTypes ?? new Dictionary<int, string>(),
                            ModelBonds = ModelBonds ?? new Dictionary<string, int>(),
                            Errors = Model.MotiveErrors,
                            Warnings = Model.MotiveWarnings
                        }
                    },
                    ValidationType = MotiveValidationType.Database,
                    MotiveCount = Entries.Count
                };
            }
        }

        public class StructureWrapper
        {
            public string Id { get; set; }
            public int AtomCount { get; set; }
            public string[] ModelNames { get; set; }
            public Dictionary<string, int> Summary { get; set; }
        }

        public Dictionary<string, ModelWrapper> Models;
        public List<StructureWrapper> Structures;

        public static void MergeSummary<T>(Dictionary<T, int> into, Dictionary<T, int> from) 
        {
            foreach (var e in from)
            {
                int val;
                into.TryGetValue(e.Key, out val);
                into[e.Key] = val + e.Value;
            }
        }

        static void MergeMessages<T>(Dictionary<string, T> into, Dictionary<string, T> from)
        {
            foreach (var e in from)
            {
                into[e.Key] = e.Value;
            }
        }

        /// <summary>
        /// Not thread safe -- sync it.
        /// </summary>
        /// <param name="result"></param>
        public void ProcessResult(string id, int atomCount, ValidationResult result)
        {
            Dictionary<string, int> structureSummary = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var r in result.Models)
            {
                MergeSummary(structureSummary, r.Summary);

                var model = Models[r.ModelName];
                lock (model)
                {
                    model.Entries.AddRange(r.Entries);
                    MergeSummary(model.Summary, r.Summary);

                    model.NotAnalyzedNames.AddRange(r.NotAnalyzedNames);
                    model.StructureNames.AddRange(r.StructureNames);

                    MergeMessages(model.Model.MotiveErrors, r.Errors);
                    MergeMessages(model.Model.MotiveWarnings, r.Warnings);

                    if (model.ModelChiralAtoms == null) model.ModelChiralAtoms = r.ChiralAtoms;
                    if (model.ModelChiralAtomsInfo == null) model.ModelChiralAtomsInfo = r.ChiralAtomsInfo;
                    if (model.ModelAtomNamingEquivalence == null) model.ModelAtomNamingEquivalence = r.AtomNamingEquivalence;
                    if (model.ModelNames == null) model.ModelNames = r.ModelNames;
                    if (model.ModelAtomTypes == null) model.ModelAtomTypes = r.ModelAtomTypes;
                    if (model.ModelBonds == null) model.ModelBonds = r.ModelBonds;
                    if (model.SourceType  == null) model.SourceType = r.CoordinateSource;
                }
            }

            var entry = new StructureWrapper
            {
                Id = id,
                AtomCount = atomCount,
                ModelNames = result.Models.Select(m => m.ModelName).OrderBy(n => n, StringComparer.Ordinal).ToArray(),
                Summary = structureSummary
            };

            lock (Structures)
            {
                Structures.Add(entry);
            }
        }

        public void ExportModelSummary(string folder)
        {
            var exp = Models.Select(p => p.Value).OrderBy(m => m.Model.Name, StringComparer.Ordinal).ToArray().GetExporter()
                .AddExportableColumn(m => m.Model.Name, ColumnType.String, "Name")
                .AddExportableColumn(m => m.Model.Structure.Atoms.Count, ColumnType.Number, "AtomCount")
                .AddExportableColumn(m => m.Model.ChiralAtoms.Length, ColumnType.Number, "ChiralAtomCount")
                .AddExportableColumn(m => m.StructureNames.Count, ColumnType.Number, "StructureCount");

            var template = Models.Select(p => p.Value).FirstOrDefault(m => m.Summary.Count != 0);
            if (template != null)
            {
                foreach (var c in template.Summary.Keys.OrderBy(k => k))
                {
                    var key = c;
                    exp.AddExportableColumn(m => m.Summary.ContainsKey(key) ? m.Summary[key] : 0, ColumnType.Number, c);
                }
            }

            //File.WriteAllText(Path.Combine(folder, "model_summary.json"), exp.ToDictionaryList().ToJsonString());
            File.WriteAllText(Path.Combine(folder, "model_summary.csv"), exp.ToCsvString());

            var totalSummary = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var m in Models.Select(p => p.Value)) MergeSummary(totalSummary, m.Summary);
            File.WriteAllText(Path.Combine(folder, "summary.json"), totalSummary.ToJsonString());
        }

        public void ExportStructureSummary(string folder)
        {
            var exp = Structures.OrderBy(m => m.Id, StringComparer.Ordinal).ToArray().GetExporter()
                .AddExportableColumn(m => m.Id, ColumnType.String, "Id")
                .AddExportableColumn(m => m.ModelNames.JoinBy(" "), ColumnType.String, "Models")
                .AddExportableColumn(m => m.AtomCount, ColumnType.Number, "AtomCount");

            var template = Structures.FirstOrDefault(m => m.Summary.Count != 0);
            if (template != null)
            {
                foreach (var c in template.Summary.Keys.OrderBy(k => k))
                {
                    var key = c;
                    exp.AddExportableColumn(m => m.Summary.ContainsKey(key) ? m.Summary[key] : 0, ColumnType.Number, c);
                }
            }

            //File.WriteAllText(Path.Combine(folder, "model_summary.json"), exp.ToDictionaryList().ToJsonString());
            File.WriteAllText(Path.Combine(folder, "structure_summary.csv"), exp.ToCsvString());
        }

        public static DatabaseAggregator Create(Dictionary<string, MotiveModel> models, ValidatorService svc)
        {
            object sync = new object();
            Dictionary<string, ModelWrapper> modelWraps = new Dictionary<string, ModelWrapper>(StringComparer.OrdinalIgnoreCase);

            //int progress = 0;
            Parallel.ForEach(models, new ParallelOptions { MaxDegreeOfParallelism = svc.MaxDegreeOfParallelism }, m =>
            {
                var w = new ModelWrapper { Model = m.Value.Clone() };
                lock (sync)
                {
                    modelWraps[m.Key] = w;
                    //progress++;
                    //if (progress % 1000 == 0)
                    //{
                    //    svc.Log("{0} / {1} models initialized.", progress, models.Count);
                    //}
                }
            });

            return new DatabaseAggregator
            {
                Models = modelWraps,
                Structures = new List<StructureWrapper>()
            };
        }
    }
}
