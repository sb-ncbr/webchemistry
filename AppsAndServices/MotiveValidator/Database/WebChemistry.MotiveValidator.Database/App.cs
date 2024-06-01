namespace WebChemistry.MotiveValidator.Database
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.MotiveValidator.DataModel;
    using WebChemistry.Platform;
    using WebChemistry.Platform.Computation;
    using WebChemistry.Platform.Server;
    using WebChemistry.Platform.Users;
    
    public class MotiveValidatorDatabaseApp : PersistentObjectBase<MotiveValidatorDatabaseApp>
    {
        public static readonly char SpecificResidueSeparator = ':';
        public static readonly char SpecificResidueDelimiter = ';';

        /// <summary>
        /// Name of the app.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// DB user id.
        /// </summary>
        public EntityId UserId { get; set; }

        /// <summary>
        /// Id of the latest database.
        /// </summary>
        public string DatabaseVersionId { get; set; }

        /// <summary>
        /// When was the DB last updated.
        /// </summary>
        public DateTime LastUpdated { get; set; }
        
        /// <summary>
        /// Return the root folder of the current database.
        /// </summary>
        /// <returns></returns>
        public string GetCurrentDatabasePath()
        {
            return Path.Combine(CurrentDirectory, DatabaseVersionId, "computation", "result");
        }
        
        /// <summary>
        /// Db base path.
        /// </summary>
        /// <returns></returns>
        public string GetBasePath()
        {
            return CurrentDirectory;
        }

        /// <summary>
        /// Return the path to the audit directory.
        /// </summary>
        /// <returns></returns>
        public string GetCurrentAuditPath()
        {
            return Path.Combine(CurrentDirectory, DatabaseVersionId, "audit");
        }


        static void MergeSummary<T>(Dictionary<T, int> into, Dictionary<T, int> from)
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

        public static void MergeModels(ModelValidationEntry into, ModelValidationEntry from)
        {
            into.Entries = into.Entries.Concat(from.Entries).OrderBy(e => e.Id, StringComparer.Ordinal).ToArray();
            MergeSummary(into.Summary, from.Summary);            
            into.StructureNames = into.StructureNames.Concat(from.StructureNames).OrderBy(n => n, StringComparer.Ordinal).ToArray();
            into.NotAnalyzedNames = into.NotAnalyzedNames.Concat(from.NotAnalyzedNames).OrderBy(n => n, StringComparer.Ordinal).ToArray();
            MergeMessages(into.Warnings, from.Warnings);
            MergeMessages(into.Errors, from.Errors);
        }

        /// <summary>
        /// Merges two validation results.
        /// </summary>
        /// <param name="into"></param>
        /// <param name="from"></param>
        public static void MergeValidations(ValidationResult into, ValidationResult from)
        {
            var models = into.Models.ToDictionary(m => m.ModelName, StringComparer.Ordinal);
            foreach (var m in from.Models)
            {
                ModelValidationEntry intoEntry;
                if (models.TryGetValue(m.ModelName, out intoEntry))
                {
                    MergeModels(intoEntry, m);
                }
                else
                {
                    models[m.ModelName] = m;
                }
            }
            MergeMessages(into.Errors, from.Errors);
            into.MotiveCount += from.MotiveCount;
            into.Models = models.Values.OrderBy(m => m.ModelName, StringComparer.Ordinal).ToArray();
        }

        /// <summary>
        /// Merges results that are no intersected motif-wise. The user is responsible for this.
        /// 
        /// Useful for merging data from multiple residues or structures.
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        static ValidationResult MergeDisjunctResults(List<ValidationResult> results)
        {
            if (results.Count == 0)
            {
                return new ValidationResult
                {
                    Version = "n/a",
                    ValidationType = MotiveValidationType.Database
                };
            }

            var ret = results[0];
            for (int i = 1; i < results.Count; i++)
            {
                MergeValidations(ret, results[i]);
            }
            ret.Models = ret.Models.OrderBy(m => m.ModelName, StringComparer.Ordinal).ToArray();
            return ret;
        }

        ValidationResult GetResults(string prefix, IEnumerable<string> names, MotiveValidatorDatabaseInterfaceProvider dbProvider)
        {
            names = names.Select(n => n.ToUpperInvariant()).Distinct(StringComparer.Ordinal).ToArray();
            var count = names.Count();

            if (count == 0)
            {
                return new ValidationResult
                {
                    Version = "n/a",
                    ValidationType = MotiveValidationType.Database
                };
            }
            if (count > 10)
            {
                throw new InvalidOperationException(string.Format("Too many queries ({0}, max. allowed is 10)", names.Count()));
            }

            var db = GetInterface(dbProvider);

            names = names.Where(n => db.HasEntry(Path.Combine(prefix, n, "result.json"))).ToArray();
            //if (missing.Length > 0)
            //{
            //    throw new InvalidOperationException("No data found for '" + missing.JoinBy() + "'.");
            //}

            if (names.Count() == 1)
            {
                return JsonHelper.FromJsonString<ValidationResult>(db.GetEntry(Path.Combine(prefix, names.First(), "result.json")));
            }

            var results = names
                .Select(n => db.GetEntry(Path.Combine(prefix, n, "result.json")))
                .Where(d => d != null)
                .Select(d => JsonHelper.FromJsonString<ValidationResult>(d))
                .ToList();

            return MergeDisjunctResults(results);
        }
        
        static PdbResidueIdentifier GetRid(string id)
        {
            var t = PdbResidueIdentifier.Parse(id.Substring(id.IndexOf(' ') + 1));
            return new PdbResidueIdentifier(t.Number, t.ChainIdentifier, ' ');
        }

        static PdbResidueIdentifier GetExactRid(string id)
        {
            return PdbResidueIdentifier.Parse(id.Substring(id.IndexOf(' ') + 1));
        }

        static PdbResidueIdentifier? IsResiduePresentExact(ValidationResultEntry e, HashSet<PdbResidueIdentifier> rids)
        {
            if (e.MainResidue != null)
            {
                var rid = GetExactRid(e.MainResidue);
                if (rids.Contains(rid)) return rid;
            }
            return e.Residues.Select(r => (PdbResidueIdentifier?)GetExactRid(r)).FirstOrDefault(r => rids.Contains(r.Value));
        }

        ValidationResult GetSearchResultsInternal(QuickSearchCriteria criteria, MotiveValidatorDatabaseInterfaceProvider dbProvider)
        {
            if (criteria.StructuresAndResidues.Count > 10)
            {
                throw new InvalidOperationException(string.Format("Too many queries ({0}, max. allowed is 10)", criteria.StructuresAndResidues.Count));
            }


            var modelSet = criteria.Models.ToHashSet(StringComparer.OrdinalIgnoreCase);
            Func<string, bool> includeModel = modelSet.Count > 0 ? new Func<string, bool>(m => modelSet.Contains(m)) : new Func<string, bool>(_ => true);

            var includedStructures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var db = GetInterface(dbProvider);
            var results = criteria.StructuresAndResidues
                .Where(n => db.HasEntry(Path.Combine("by_structure", n.Key, "result.json")))
                .Select(n =>
                {
                    var hasSpecificResidues = n.Value.Length > 0;
                    var sId = n.Key;

                    if (!includedStructures.Add(sId)) return null;

                    var ret = JsonHelper.FromJsonString<ValidationResult>(db.GetEntry(Path.Combine("by_structure", sId, "result.json")));
                    if (!hasSpecificResidues)
                    {
                        ret.Models = ret.Models.Where(m => includeModel(m.ModelName)).ToArray();
                        return ret;
                    }
                    else
                    {   
                        var rids = new HashSet<PdbResidueIdentifier>();
                        var incorrectRids = new List<string>();

                        foreach (var i in n.Value)
                        {
                            try
                            {
                                var t = PdbResidueIdentifier.Parse(i);
                                t = new PdbResidueIdentifier(t.Number, t.ChainIdentifier, ' ');
                                rids.Add(t);
                            }
                            catch
                            {
                                incorrectRids.Add(i);
                            }
                        }

                        var usedRids = new HashSet<PdbResidueIdentifier>();

                        foreach (var m in ret.Models)
                        {                            
                            foreach (var p in m.Summary.ToArray())
                            {
                                m.Summary[p.Key] = 0;
                            }
                            var newEntries = new List<ValidationResultEntry>();

                            foreach (var e in m.Entries)
                            {
                                PdbResidueIdentifier? rid = null;
                                if (e.MainResidue != null)
                                {
                                    var t = GetRid(e.MainResidue);
                                    if (rids.Contains(t)) rid = t;
                                }
                                else
                                {
                                    rid = e.Residues.Select(r => (PdbResidueIdentifier?)GetRid(r)).FirstOrDefault(r => rids.Contains(r.Value));
                                }

                                if (rid.HasValue)
                                {
                                    newEntries.Add(e);
                                    usedRids.Add(rid.Value);

                                    foreach (var v in e.Flags)
                                    {
                                        m.Summary[v]++;
                                    }
                                } 
                                else
                                {
                                    m.Errors.Remove(e.Id);
                                    m.Warnings.Remove(e.Id);
                                }
                            }

                            m.Entries = newEntries.ToArray();  
                        }

                        ret.Models = ret.Models.Where(m => includeModel(m.ModelName) && m.Entries.Length > 0).ToArray();

                        if (usedRids.Count != rids.Count)
                        {
                            ret.Errors.Add("Missing Search Molecule(s) in " + sId, string.Format("The molecule(s) with id(s) '{0}' are not present or were not validated.", 
                                rids.Where(r => !usedRids.Contains(r)).Select(r => r.ToString()).JoinBy()));
                        }

                        if (incorrectRids.Count > 0)
                        {
                            ret.Errors.Add("Incorrect Search Identifier(s) in " + sId, string.Format("The id(s) '{0}' are not in correct format (number chain, e.g. 123 A).",
                                incorrectRids.JoinBy()));
                        }

                        return ret;
                    }
                })
                .Where(r => r != null)
                .ToList();

            return MergeDisjunctResults(results);
        }
        
        public Dictionary<PdbResidueIdentifier, string[]> GetValidationFlags(
            string structureId, Dictionary<string, PdbResidueIdentifier[]> idsByResidueName, MotiveValidatorDatabaseDataInterface db)
        {

            var ret = idsByResidueName.Values.SelectMany(xs => xs).Distinct().ToDictionary(r => r, _ => new string[0]);

            if (ret.Count == 0 || !db.HasEntry(Path.Combine("by_structure", structureId, "result.json")))
            {
                return ret;
            }
            if (idsByResidueName.Comparer != StringComparer.OrdinalIgnoreCase)
            {
                idsByResidueName = idsByResidueName.ToDictionary(m => m.Key, m => m.Value, StringComparer.OrdinalIgnoreCase);
            }
            var validation = JsonHelper.FromJsonString<ValidationResult>(db.GetEntry(Path.Combine("by_structure", structureId, "result.json")));

            var comp = StringComparer.Ordinal;
            foreach (var model in validation.Models)
            {
                if (!idsByResidueName.ContainsKey(model.ModelName) || idsByResidueName[model.ModelName].Length == 0) continue;

                var rids = idsByResidueName[model.ModelName]
                    .ToHashSet();
                foreach (var e in model.Entries)
                {
                    var rid = IsResiduePresentExact(e, rids);
                    if (!rid.HasValue) continue;
                    ret[rid.Value] = e.Flags;
                }
            }
            
            return ret;
        }

        /// <summary>
        /// Merges the validations of multiple residues.
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public string GetValidationForModelsJson(IEnumerable<string> names, MotiveValidatorDatabaseInterfaceProvider dbProvider)
        {
            return GetResults("by_model", names, dbProvider).ToJsonString();
        }

        /// <summary>
        /// Merges the validation results of multiple structures.
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public string GetValidationForStructuresJson(IEnumerable<string> names, MotiveValidatorDatabaseInterfaceProvider dbProvider)
        {
            return GetResults("by_structure", names, dbProvider).ToJsonString();
        }

        /// <summary>
        /// Get search validation for given structures and/or models.
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="dbProvider"></param>
        /// <returns></returns>
        public string GetSearchValidationJson(QuickSearchCriteria criteria, MotiveValidatorDatabaseInterfaceProvider dbProvider)
        {
            return GetSearchValidation(criteria, dbProvider).ToJsonString();
        }

        public ValidationResult GetSearchValidation(QuickSearchCriteria criteria, MotiveValidatorDatabaseInterfaceProvider dbProvider)
        {
            if (criteria.StructuresAndResidues.Count == 0) return GetResults("by_model", criteria.Models, dbProvider);
            if (criteria.Models.Length == 0 && criteria.StructuresAndResidues.All(s => s.Value.Length == 0)) return GetResults("by_structure", criteria.StructuresAndResidues.Keys, dbProvider);
            return GetSearchResultsInternal(criteria, dbProvider);
        }

        /// <summary>
        /// Criteria for searching of structures.
        /// </summary>
        public class QuickSearchCriteria
        {
            /// <summary>
            /// If the residue array is empty, include all residues.
            /// </summary>
            public Dictionary<string, string[]> StructuresAndResidues { get; set; }

            /// <summary>
            /// A list of models.
            /// </summary>
            public string[] Models { get; set; }
        }

        /// <summary>
        /// Create the search criteria from input strings.
        /// </summary>
        /// <param name="structures"></param>
        /// <param name="models"></param>
        /// <returns></returns>
        public static QuickSearchCriteria GetSearchCriteria(string structures, string models)
        {
            structures = structures ?? "";
            models = models ?? "";

            var xs = structures
                .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(e =>
                {
                    var sep = e.IndexOf(SpecificResidueSeparator);
                    return new
                    {
                        Id = sep > 0 ? e.Substring(0, sep).Trim().ToUpperInvariant() : e.Trim().ToUpperInvariant(),
                        Res = sep > 0 && e.Length > sep + 1
                            ? e.Substring(sep + 1)
                                .Split(new[] { SpecificResidueDelimiter }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(i => i.Trim())
                                .ToArray()
                            : new string[0]
                    };
                })
                .GroupBy(e => e.Id, StringComparer.Ordinal)
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(e => e.Res).Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToArray(),
                    StringComparer.Ordinal);

            var ys = models
                .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();

            return new QuickSearchCriteria
            {
                StructuresAndResidues = xs,
                Models = ys
            };
        }

        /// <summary>
        /// The the inteface to the database.
        /// </summary>
        /// <param name="inMemory"></param>
        /// <param name="provider"
        /// <returns></returns>
        public MotiveValidatorDatabaseDataInterface GetInterface(MotiveValidatorDatabaseInterfaceProvider provider)
        {
            return provider.GetInterface(this);
        }

        /// <summary>
        /// Stores the current version of the object.
        /// </summary>
        public void Store()
        {
            this.Save();
        }

        /// <summary>
        /// Get the user.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public UserInfo GetAppUser(AppServer server, string name)
        {
            var user = server.Users.TryGetByShortId(name);
            if (user != null) return user;
            user = server.Users.CreateUser(name);
            var profile = user.GetProfile();
            profile.ConcurrentComputationLimit = 16;
            user.SaveProfile(profile);
            return user;
        }

        /// <summary>
        /// Creates the aggregator computation.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="structureIds"></param>
        /// <param name="modelIds"></param>
        /// <param name="customId"></param>
        /// <returns></returns>
        public ComputationInfo CreateAggregatorComputation(string source, string[] structureIds, string[] modelIds, string customId = null)
        {
            var user = UserInfo.Load(UserId);

            var config = new MotiveValidatorConfig
            {
                ValidationType = MotiveValidationType.Database,                
                DatabaseModeServerName = Id.ServerName,
                DatabaseModeAppName = Name, 
                DatabaseModeCustomModelIds = modelIds,
                DatabaseModeCustomStructureIds = structureIds
            };

            var svc = ServerManager.MasterServer.Services.GetService("MotiveValidator");
            var comp = user.Computations.CreateComputation(user, svc, "ValidatorDBComputation", config, source, customId: customId);
            return comp;
        }

        /// <summary>
        /// Creates the app.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="server"></param>
        /// <returns></returns>
        public static MotiveValidatorDatabaseApp Create(string name, AppServer server)
        {            
            var user = server.Users.GetOrCreateUserByName(name);
            var profile = user.GetProfile();
            profile.ConcurrentComputationLimit = 16;
            user.SaveProfile(profile);

            var id = server.GetAppId(name);
            var ret = CreateAndSave(id, x =>
            {
                x.Name = name;
                x.DatabaseVersionId = "1";
                x.UserId = user.Id;
            });
            
            return ret;
        }
    }
}
