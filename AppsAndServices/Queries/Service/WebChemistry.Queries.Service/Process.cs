namespace WebChemistry.Queries.Service
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.Queries.Core;
    using WebChemistry.Queries.Service.DataModel;
    using WebChemistry.Platform;
    using WebChemistry.Platform.MoleculeDatabase;
    using WebChemistry.Platform.Services;

    public partial class QueriesService : ServiceBase<QueriesService, QueriesServiceSettings, QueriesStandaloneServiceSettings, QueriesServiceState>
    {
        const string MissingMetadataPlaceholder = "NotAssinged";
        object ProcessStructureLock = new object();

        void ProcessStructure(StructureResultWrap structure)
        {
            ComputationStructureEntryMetadata metadata = null;
            var entries = new Dictionary<string,ComputationStructureWithMetadataEntry>(StringComparer.Ordinal);
            
            var entry = structure.Entry;

            foreach (var r in structure.Results)
            {
                var q = QueryIndex[r.Query.Id];
                
                if (r.Motives.Count > 0 || !string.IsNullOrEmpty(r.ErrorMessage))
                {
                    if (metadata == null && structure.Structure != null)
                    {
                        var data = structure.Structure.PdbMetadata();
                        metadata = new ComputationStructureEntryMetadata
                        {
                            EcNumbers = data.EcNumbers,
                            ExperimentMethod = string.IsNullOrWhiteSpace(data.ExperimentMethod) ? MissingMetadataPlaceholder : data.ExperimentMethod,
                            HostOrganisms = data.HostOrganisms,
                            HostOrganismsGenus = data.HostOrganismsGenus,
                            OriginOrganisms = data.OriginOrganisms,
                            OriginOrganismsGenus = data.OriginOrganismsGenus,
                            PolymerType = EnumHelper.ToString(data.PolymerType),
                            Resolution = data.Resolution.HasValue ? (double?)Math.Round(data.Resolution.Value, 2) : null,
                            ProteinStoichiometry = EnumHelper.ToString(data.ProteinStoichiometry),
                            YearOfPublication = data.Released.HasValue ? (int?)data.Released.Value.Year : null
                        };
                    }

                    var me = ComputationStructureWithMetadataEntry.FromEntry(entry, 
                        r.Motives.Count,
                        entry.QueryTimingMs, 
                        structure.Structure == null ? 0 : structure.Structure.Atoms.Count, 
                        structure.Structure == null ? 0 : structure.Structure.PdbResidues().Count, 
                        metadata);

                    entries[r.Query.Id] = me;

                    if (!string.IsNullOrEmpty(r.ErrorMessage))
                    {
                        me.ErrorType = ComputationStructureErrorType.Computation;
                        me.ErrorMessage = r.ErrorMessage;
                    }
                    
                    lock (ProcessStructureLock)
                    {
                        q.Structures.Add(me);
                    }
                }

                if (r.Motives.Count > 0) ResultsQueue.Add(r);
            }

            lock (Structures)
            {
                Structures.Add(entry);
            }

            try
            {
                if (entries.Count > 0) 
                {
                    Validator.Validate(structure, entries, QueryIndex, this);
                }
            }
            catch (Exception ex)
            {
                foreach (var e in entries)
                {
                    e.Value.ComputationWarnings.Add("Error validating patterns. The validation for this structure won't be available.");

                }

                Log("[{0}] Validation error: {1}", structure.Entry.Id, ex.Message);
            }
        }
        
        void Process()
        {
            bool continueProcess = true;
            while (continueProcess)
            {
                QueryResult r = null;
                try
                {
                    r = ResultsQueue.Take();
                    
                    foreach (var pair in r.Motives)
                    {
                        var motive = pair.Item2;
                        var structure = pair.Item1;

                        if (!StatisticsOnly)
                        {
                            Zip.AddEntry(Path.Combine(r.Query.Id, "patterns", structure.Id + ".pdb"), w => structure.WritePdb(w));
                        }
                                                
                        r.Query.Patterns.Add(motive);
                    }                    
                }
                catch (InvalidOperationException)
                {
                    continueProcess = false;
                }
                catch (Exception e)
                {
                    if (r == null || r.ParentId == null) Log("Error: {0}", e.Message);
                    else Log("Error - {0}: {1}", r.ParentId, e.Message);
                }
            }
        }

        static void UpdateMetadataEntry(ComputationStructureWithMetadataEntry entry, string prop, object value, Dictionary<string, MetadataEntry> entries)
        {
            if (entry.PatternCount == 0) return;
            var key = MetadataEntry.GetKey(prop, value);
            MetadataEntry e;
            if (!entries.TryGetValue(key, out e))
            {
                e = new MetadataEntry(prop, value);
                entries[key] = e;
            }
            e.StructureCount++;
            e.PatternCount += entry.PatternCount;
        }

        static void VisitMetadataEntry(ComputationStructureWithMetadataEntry entry, Dictionary<string, MetadataEntry> entries)
        {
            var data = entry.Metadata;
            if (data == null) return;

            if (data.EcNumbers != null && data.EcNumbers.Length > 0) foreach (var e in data.EcNumbers) UpdateMetadataEntry(entry, "EcNumbers", e, entries);
            else UpdateMetadataEntry(entry, "EcNumbers", "None", entries);

            if (data.HostOrganisms != null && data.HostOrganisms.Length > 0) foreach (var e in data.HostOrganisms) UpdateMetadataEntry(entry, "HostOrganisms", e, entries);
            else UpdateMetadataEntry(entry, "HostOrganisms", "None", entries);

            if (data.HostOrganismsGenus != null && data.HostOrganismsGenus.Length > 0) foreach (var e in data.HostOrganismsGenus) UpdateMetadataEntry(entry, "HostOrganismsGenus", e, entries);
            else UpdateMetadataEntry(entry, "HostOrganismsGenus", "None", entries);

            if (data.OriginOrganisms != null && data.OriginOrganisms.Length > 0) foreach (var e in data.OriginOrganisms) UpdateMetadataEntry(entry, "OriginOrganisms", e, entries);
            else UpdateMetadataEntry(entry, "OriginOrganisms", "None", entries);

            if (data.OriginOrganismsGenus != null && data.OriginOrganismsGenus.Length > 0) foreach (var e in data.OriginOrganismsGenus) UpdateMetadataEntry(entry, "OriginOrganismsGenus", e, entries);
            else UpdateMetadataEntry(entry, "OriginOrganismsGenus", "None", entries);

            UpdateMetadataEntry(entry, "ExperimentMethod", data.ExperimentMethod, entries);
            UpdateMetadataEntry(entry, "ProteinStoichiometry", data.ProteinStoichiometry, entries);
            UpdateMetadataEntry(entry, "PolymerType", data.PolymerType, entries);
            UpdateMetadataEntry(entry, "YearOfPublication", data.YearOfPublication, entries);
            UpdateMetadataEntry(entry, "Resolution", data.Resolution, entries);            
        }

        void ProcessMetadata()
        {
            foreach (var q in this.Queries)
            {
                Dictionary<string, MetadataEntry> summary = new Dictionary<string, MetadataEntry>(StringComparer.Ordinal);
                foreach (var s in q.Structures) VisitMetadataEntry(s, summary);
                q.MetadataSummary = summary.Values
                    .GroupBy(v => v.Prop, StringComparer.Ordinal)
                    .Select(g => new QueriesMetadataPropertySummary
                    {
                        Name = g.Key,
                        Entries = g
                            .Select(e => new QueriesMetadataSummaryEntry { Value = e.Value, PatternCount = e.PatternCount, StructureCount = e.StructureCount })
                            .OrderBy(e => e.Value)
                            .ToArray()
                    })
                    .OrderBy(g => g.Name, StringComparer.Ordinal)
                    .ToArray();
            }
        }
    }
}
