namespace WebChemistry.Queries.Service
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Queries.Service.DataModel;
    using WebChemistry.Platform.Services;

    public partial class QueriesService : ServiceBase<QueriesService, QueriesServiceSettings, QueriesStandaloneServiceSettings, QueriesServiceState>
    {
        static string ReadMe = new string[] 
        {
            "query_id/         Folder for each query named by its unique id.",
            "  patterns/              Folder with patterns stored in PDB format.",
            "  data.json               Information about the result in JSON format.",
            "  patterns.csv           Information about the patterns in CSV format.",
            "  metadata_summary.csv    Information about the metadata (origin organism, EC number, etc.) in CSV format.",
            "  structures.csv          Information about entries that contain the given patterns in CSV format.",
            "structures.csv    Information about all entries that were queried in CSV format.",
            "structures.json   Information about all entries that were queried in JSON format.",
            "summary.json      Summary information (number of found patterns, etc.) about the query execution in JSON format."
        }.JoinBy(Environment.NewLine);

        void Export(Stopwatch totalTime)
        {
            var summaries = new List<ComputationQuerySummary>();
            foreach (var q in Queries)
            {
                var summary = new ComputationQuerySummary
                {
                    Id = q.Id,
                    QueryString = q.QueryString,

                    PatternCount = q.Patterns.Count,
                    StructureCount = q.Structures.Count,
                    StructureWithWarningCount = q.Structures.Count(s => s.ReaderWarnings.Length > 0 || s.ComputationWarnings.Count > 0),
                    StructureWithComputationWarningCount = q.Structures.Count(s => s.ComputationWarnings.Count > 0),
                    StructureWithReaderWarningCount = q.Structures.Count(s => s.ReaderWarnings.Length > 0),
                    StructureWithErrorCount = q.Structures.Count(s => s.ErrorType != ComputationStructureErrorType.None),
                    StructureWithPatternsCount = q.Structures.Count(s => s.PatternCount > 0)
                };
                
                //Zip.AddEntry(Path.Combine(q.Id, "summary.json"), summary.ToJsonString());
                summaries.Add(summary);

                var result = new QueriesComputationQueryResult
                {
                    Query = summary,
                    Patterns = q.Patterns.OrderBy(m => m.ParentId, StringComparer.Ordinal).ThenBy(m => m.Serial).ToArray(),
                    Structures = q.Structures.OrderBy(s => s.Id, StringComparer.Ordinal).ToArray(),
                    ValidationFlags  = q.ValidationFlags.Flags.ToDictionary(f => f.Value, f => f.Key.Flags),
                    MetadataSummary = q.MetadataSummary
                };

                Zip.AddEntry(Path.Combine(q.Id, "data.json"), result.ToJsonString());
                Zip.AddEntry(Path.Combine(q.Id, "patterns.csv"), w => ComputationPatternEntry.GetExporter(result.Patterns).WriteCsvString(w));
                Zip.AddEntry(Path.Combine(q.Id, "structures.csv"), w => ComputationStructureWithMetadataEntry.GetExporter(result.Structures).WriteCsvString(w));

                var metadataCsv = q.MetadataSummary
                    .SelectMany(p => p.Entries.Select(e => new { Property = p.Name, Value = e.Value ?? (object)"NotAssigned", PatternCount = e.PatternCount, StructureCount = e.StructureCount }))
                    .ToArray()
                    .GetExporter()
                    .AddPropertyColumns();

                Zip.AddEntry(Path.Combine(q.Id, "metadata_summary.csv"), w => metadataCsv.WriteCsvString(w));
            }

            var totalSummary = new ComputationSummary
            {
                ServiceVersion = GetVersion().ToString(),
                TotalStructureCount = TotalStructureCount,
                Queries = summaries.OrderBy(q => q.Id, StringComparer.Ordinal).ToArray(),
                TotalTimeMs = (int)totalTime.ElapsedMilliseconds,

                PatternLimitReached = PatternLimitReached,
                PatternLimit = this.PatternCountLimit,

                AtomLimitReached = this.AtomLimitReached,
                AtomLimit = this.AtomCountLimit,

                Warnings = Validator == null || Validator.FailedToInit
                    ? new string[] { "Failed to initialize ValidatorDB. Validations won't be available." }
                    : new string[0]
            }.ToJsonString();

            File.WriteAllText(Path.Combine(ResultFolder, "summary.json"), totalSummary);
            Zip.AddEntry("summary.json", totalSummary);
            Zip.AddEntry("structures.json", Structures.OrderBy(s => s.Id).ToList().ToJsonString());            
            Zip.AddEntry("structures.csv", w => ComputationStructureEntry.GetExporter(Structures).WriteCsvString(w));
            Zip.AddEntry("readme.txt", ReadMe);
        }
    }
}
