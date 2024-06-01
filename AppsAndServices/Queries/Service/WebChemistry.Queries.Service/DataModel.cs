namespace WebChemistry.Queries.Service
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using WebChemistry.Framework.Core;
    using WebChemistry.Queries.Core;
    using WebChemistry.Queries.Service.DataModel;
    using WebChemistry.MotiveValidator.Database;
    using WebChemistry.Platform;
    using WebChemistry.Framework.Core.Pdb;

    class QueryWrap
    {
        public QueryInfo Info { get; set; }
        public string Id { get; set; }
        public string QueryString { get; set; }
        public Query Query { get; set; }
        
        public List<ComputationPatternEntry> Patterns { get; private set; }
        public List<ComputationStructureWithMetadataEntry> Structures { get; private set; }
        public QueriesMetadataPropertySummary[] MetadataSummary { get; set; }

        public ValidationFlagsHelper ValidationFlags { get; set; }
        
        public QueryWrap()
        {
            Patterns = new List<ComputationPatternEntry>();
            Structures = new List<ComputationStructureWithMetadataEntry>();
            MetadataSummary = new QueriesMetadataPropertySummary[0];
            ValidationFlags = new ValidationFlagsHelper();
        }
    }

    class QueryResult
    {
        public int TimingMs { get; set; }
        public QueryWrap Query { get; set; }
        public string ParentId { get; set; }
        public string ErrorMessage { get; set; }

        public List<Tuple<IStructure, ComputationPatternEntry>> Motives { get; set; }
    }

    class StructureResultWrap
    {
        public IStructure Structure { get; set; }
        public ComputationStructureEntry Entry { get; set; }
        public List<QueryResult> Results { get; set; }
    }  

    class MetadataEntry
    {
        public static string GetKey(string prop, object value)
        {
            if (value == null) return prop + ":null";
            return prop + ":" + value.ToString().ToLowerInvariant();
        }

        public string Prop { get; private set; }
        public object Value { get; private set; }

        public int PatternCount { get; set; }
        public int StructureCount { get; set; }
        
        public MetadataEntry(string prop, object value)
        {
            this.Prop = prop;
            this.Value = value;
        }
    }

    class ValidationFlagsWrapper : IEquatable<ValidationFlagsWrapper>
    {
        int hash;

        public override int GetHashCode()
        {
            return hash;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ValidationFlagsWrapper;
            if (other == null) return false;
            return Equals(other);
        }
        
        public string[] Flags { get; private set; }

        public ValidationFlagsWrapper(string[] flags)
        {
            var hash = 31;
            for (int i = 0; i < flags.Length; i++)
            {
                hash = 23 * hash + flags[i].GetHashCode();
            }
            this.hash = hash;
            this.Flags = flags;
        }

        public bool Equals(ValidationFlagsWrapper other)
        {
            string[] xs = Flags, ys = other.Flags;
            if (this.hash != other.hash || xs.Length != ys.Length) return false;

            var comp = StringComparer.Ordinal;
            for (int i = 0; i < xs.Length; i++)
            {
                if (!comp.Equals(xs[i], ys[i])) return false;
            }
            return true;
        }
    }

    class ValidationFlagsHelper
    {
        int serial;
        public Dictionary<ValidationFlagsWrapper, int> Flags;

        public int IncludeFlags(string[] flags)
        {
            var wrap = new ValidationFlagsWrapper(flags);
            int id;
            lock (Flags)
            {
                if (Flags.TryGetValue(wrap, out id)) return id;
                id = serial++;
                Flags[wrap] = id;
            }
            return id;
        }

        public ValidationFlagsHelper()
        {
            Flags = new Dictionary<ValidationFlagsWrapper, int>();
        }
    }

    partial class ValidationHelper
    {
        public bool FailedToInit { get; private set; }

        bool DoValidation;
        MotiveValidatorDatabaseApp App;
        MotiveValidatorDatabaseDataInterface Db;
        
        readonly HashSet<string> IgnoredNames = new[] {
            "ALA", "ARG", "ASP", "CYS", "GLN", "GLU", "GLY", "HIS", "ILE", "LEU", "LYS", "MET", "PHE", "PRO", "SER", "THR", "TRP", "TYR", "VAL", "ASN",
            "A", "C", "G", "T", "U", "DA", "DC", "DG", "DT", "DU", "HOH", "MSE", "UNK"
        }.ToHashSet(StringComparer.OrdinalIgnoreCase);

        public void Validate(StructureResultWrap result, 
            Dictionary<string, ComputationStructureWithMetadataEntry> entries, 
            Dictionary<string, QueryWrap> queries,
            QueriesService svc)
        {
            //svc.Log()

            if (!DoValidation || result.Structure == null || entries.Count == 0) return;
            
            var rs = result.Structure.PdbResidues();
            var toValidate = result.Results
                .SelectMany(r => r.Motives.SelectMany(m => m.Item1.PdbResidues().Select(x => x)))
                .Where(r => !r.IsAmino && !r.IsWater && !IgnoredNames.Contains(r.Name))                
                .Distinct()
                .GroupBy(r => r.Name)
                .ToDictionary(g => g.Key, g => g.Select(r => r.Identifier).ToArray(), StringComparer.OrdinalIgnoreCase);


            if (toValidate.Count == 0) return;

            var flags = App.GetValidationFlags(result.Structure.Id, toValidate, Db);

            //svc.Log("Validing {0} residues in {1} with {2} results.",
            //    toValidate.Keys.JoinBy(), result.Structure.Id, flags.Count);

            var flagsInPattern = new HashSet<string>();
            foreach (var r in result.Results)
            {
                var q = queries[r.Query.Id];

                ComputationStructureWithMetadataEntry entry;
                if (!entries.TryGetValue(r.Query.Id, out entry)) continue;

                ValidationFlagsHelper helper = q.ValidationFlags;

                foreach (var t in r.Motives)
                {                    
                    flagsInPattern.Clear();
                    foreach (var residue in t.Item1.PdbResidues())
                    {
                        string[] fs;
                        if (!flags.TryGetValue(residue.Identifier, out fs) || fs.Length == 0) continue;
                        entry.ResidueValidationIds[residue.ToString()] = helper.IncludeFlags(fs);
                        foreach (var f in fs) flagsInPattern.Add(f);
                    }
                    if (flagsInPattern.Count > 0)
                    {
                        t.Item2.ValidationFlagsId = helper.IncludeFlags(
                            flagsInPattern.OrderBy(f => f, StringComparer.Ordinal).ToArray());
                    }
                }
            }
        }

        public ValidationHelper(bool doValidation, EntityId appId)
        {
            if (!doValidation)
            {
                DoValidation = false;
                return;
            }

            this.DoValidation = true;
            this.App = MotiveValidatorDatabaseApp.TryLoad(appId);

            if (App == null)
            {
                FailedToInit = true;
                DoValidation = false;
                return;
            }

            try
            {
                this.Db = new MotiveValidatorDatabaseDataInterface(App, false);
            }
            catch
            {
                FailedToInit = true;
                DoValidation = false;
                return;
            }
        }
    }
}

