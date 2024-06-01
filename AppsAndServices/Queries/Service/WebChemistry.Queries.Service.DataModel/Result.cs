namespace WebChemistry.Queries.Service.DataModel
{
    using System.Collections.Generic;
    using WebChemistry.Framework.Core;

    public class ComputationQuerySummary
    {
        public string Id { get; set; }
        public string QueryString { get; set; }

        public int PatternCount { get; set; }

        public int StructureCount { get; set; }
        public int StructureWithPatternsCount { get; set; }
        public int StructureWithErrorCount { get; set; }
        public int StructureWithWarningCount { get; set; }
        public int StructureWithComputationWarningCount { get; set; }
        public int StructureWithReaderWarningCount { get; set; }
    }

    public class ComputationSummary
    {
        public string ServiceVersion { get; set; }
        public int TotalStructureCount { get; set; }
        public ComputationQuerySummary[] Queries { get; set; }
        public int TotalTimeMs { get; set; }

        public bool PatternLimitReached { get; set; }
        public int PatternLimit { get; set; }

        public bool AtomLimitReached { get; set; }
        public long AtomLimit { get; set; }
        
        public string[] Errors { get; set; }
        public string[] Warnings { get; set; }

        public ComputationSummary()
        {
            Warnings = new string[0];
            Errors = new string[0];
        }
    }

    public class ComputationPatternEntry
    {
        public string Id { get; set; }
        public string ParentId { get; set; }
        public int Serial { get; set; }

        public int AtomCount { get; set; }
        public string Atoms { get; set; }

        public int ResidueCount { get; set; }
        public string Residues { get; set; }
        public string Signature { get; set; }

        public int ValidationFlagsId { get; set; }

        public static ListExporter GetExporter(IEnumerable<ComputationPatternEntry> xs)
        {
            return xs.GetExporter(xmlRootName: "Patterns", xmlElementName: "Pattern")
                .AddStringColumn(x => x.Id, "Id")
                .AddStringColumn(x => x.ParentId, "ParentId")
                .AddNumericColumn(x => x.AtomCount, "AtomCount")
                .AddStringColumn(x => x.Atoms, "Atoms")
                .AddNumericColumn(x => x.ResidueCount, "ResidueCount")
                .AddStringColumn(x => x.Residues, "Residues")
                .AddStringColumn(x => x.Signature, "Signature");
        }

        public ComputationPatternEntry()
        {
            ValidationFlagsId = -1;
        }
    }

    public class ComputationStructureEntryMetadata
    {
        public int? YearOfPublication { get; set; }
        public double? Resolution { get; set; }
        
        public string ExperimentMethod { get; set; }
        public string PolymerType { get; set; }
        public string ProteinStoichiometry { get; set; }

        public string[] EcNumbers { get; set; }
        public string[] OriginOrganisms { get; set; }
        public string[] OriginOrganismsGenus { get; set; }
        public string[] HostOrganisms { get; set; }
        public string[] HostOrganismsGenus { get; set; }

    }

    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ComputationStructureErrorType
    {
        None = 0,
        Reader,
        Generic,
        Computation
    }

    public class ComputationStructureEntry
    {
        public string Id { get; set; }
                
        public int PatternCount { get; set; }
        public int LoadTimingMs { get; set; }
        public int QueryTimingMs { get; set; }

        public ComputationStructureErrorType ErrorType { get; set; }
        public string ErrorMessage { get; set; }
        public string[] ReaderWarnings { get; set; }

        public static ListExporter GetExporter(IEnumerable<ComputationStructureEntry> xs)
        {
            return xs.GetExporter(xmlRootName: "Structures", xmlElementName: "Structure")
                .AddExportableColumn(x => x.Id, ColumnType.String, "Id")
                .AddExportableColumn(x => x.PatternCount, ColumnType.Number, "PatternCount")
                .AddExportableColumn(x => EnumHelper.ToString(x.ErrorType), ColumnType.String, "ErrorType")
                .AddExportableColumn(x => x.ErrorMessage ?? "-", ColumnType.String, "ErrorMessage")
                .AddExportableColumn(x => x.LoadTimingMs, ColumnType.Number, "LoadTimingMs")
                .AddExportableColumn(x => x.QueryTimingMs, ColumnType.Number, "QueryTimingMs")
                .AddExportableColumn(x => x.ReaderWarnings.Length, ColumnType.Number, "ReaderWarningCount")
                .AddExportableColumn(x => x.ReaderWarnings.JoinBy(" ; "), ColumnType.String, "ReaderWarnings");
        }

        public override string ToString()
        {
            if (ErrorType != ComputationStructureErrorType.None) return string.Format("{0}: Error ({1}) '{2}'", Id, ErrorType, ErrorMessage);
            return string.Format("{0}: Patterns {1} Load {2}ms Query {3}ms.", Id, PatternCount, LoadTimingMs, QueryTimingMs);
        }

        public ComputationStructureEntry()
        {
            ReaderWarnings = new string[0];
            ErrorMessage = "";
        }
    }

    public class ComputationStructureWithMetadataEntry : ComputationStructureEntry
    {
        public int AtomCount { get; set; }
        public int ResidueCount { get; set; }
        public ComputationStructureEntryMetadata Metadata { get; set; }
        public List<string> ComputationWarnings { get; set; }

        public Dictionary<string, int> ResidueValidationIds { get; set; }

        public static ListExporter GetExporter(IEnumerable<ComputationStructureWithMetadataEntry> xs)
        {
            return xs.GetExporter(xmlRootName: "Structures", xmlElementName: "Structure")
                .AddExportableColumn(x => x.Id, ColumnType.String, "Id")
                .AddExportableColumn(x => x.PatternCount, ColumnType.Number, "PatternCount")
                .AddExportableColumn(x => EnumHelper.ToString(x.ErrorType), ColumnType.String, "ErrorType")
                .AddExportableColumn(x => x.ErrorMessage ?? "-", ColumnType.String, "ErrorMessage")
                .AddExportableColumn(x => x.LoadTimingMs, ColumnType.Number, "LoadTimingMs")
                .AddExportableColumn(x => x.QueryTimingMs, ColumnType.Number, "QueryTimingMs")
                .AddNumericColumn(x => x.Metadata.YearOfPublication.HasValue ? x.Metadata.YearOfPublication.ToString() : "-", "YearOfPublication")
                .AddNumericColumn(x => x.Metadata.Resolution.HasValue ? x.Metadata.Resolution.Value.ToStringInvariant("0.00") : "-", "Resolution")
                .AddStringColumn(x => x.Metadata.PolymerType, "PolymerType")
                .AddStringColumn(x => x.Metadata.ExperimentMethod, "ExperimentMethod")
                .AddStringColumn(x => x.Metadata.ProteinStoichiometry, "ProteinStoichiometry")
                .AddStringColumn(x => x.Metadata.EcNumbers.JoinBy(" ; "), "EcNumbers")
                .AddStringColumn(x => x.Metadata.OriginOrganisms.JoinBy(" ; "), "OriginOrganisms")
                .AddStringColumn(x => x.Metadata.OriginOrganismsGenus.JoinBy(" ; "), "OriginOrganismsGenus")
                .AddStringColumn(x => x.Metadata.HostOrganisms.JoinBy(" ; "), "HostOrganisms")
                .AddStringColumn(x => x.Metadata.HostOrganismsGenus.JoinBy(" ; "), "HostOrganismsGenus")
                .AddExportableColumn(x => x.ComputationWarnings.Count, ColumnType.Number, "ComputationWarningCount")
                .AddExportableColumn(x => x.ComputationWarnings.JoinBy(" ; "), ColumnType.String, "ComputationWarnings")
                .AddExportableColumn(x => x.ReaderWarnings.Length, ColumnType.Number, "ReaderWarningCount")
                .AddExportableColumn(x => x.ReaderWarnings.JoinBy(" ; "), ColumnType.String, "ReaderWarnings");
        }

        public static ComputationStructureWithMetadataEntry FromEntry(ComputationStructureEntry entry, int patternCount, int queryTiming, int atomCount, int residueCount, ComputationStructureEntryMetadata metadata)
        {
            return new ComputationStructureWithMetadataEntry 
            {
                Id = entry.Id,
                ErrorType = entry.ErrorType,
                ErrorMessage = entry.ErrorMessage,
                ReaderWarnings = entry.ReaderWarnings,
                LoadTimingMs = entry.LoadTimingMs,
                QueryTimingMs = queryTiming,
                
                PatternCount = patternCount,
                AtomCount = atomCount,
                ResidueCount = residueCount,
                Metadata = metadata
            };
        }
  
        public ComputationStructureWithMetadataEntry()
        {
            ComputationWarnings = new List<string>();
            ResidueValidationIds = new Dictionary<string, int>(System.StringComparer.Ordinal);
        }
    }

    public class QueriesMetadataSummaryEntry
    {
        public object Value { get; set; }
        public int PatternCount { get; set; }
        public int StructureCount { get; set; }
    }

    public class QueriesMetadataPropertySummary
    {
        public string Name { get; set; }
        public QueriesMetadataSummaryEntry[] Entries { get; set; }

        public QueriesMetadataPropertySummary()
        {
            Entries = new QueriesMetadataSummaryEntry[0];
        }
    }

    public class QueriesComputationQueryResult
    {
        public ComputationQuerySummary Query { get; set; }
        public ComputationPatternEntry[] Patterns { get; set; }
        public ComputationStructureWithMetadataEntry[] Structures { get; set; }
        public QueriesMetadataPropertySummary[] MetadataSummary { get; set; }
        public Dictionary<int, string[]> ValidationFlags { get; set; }
    }
}
