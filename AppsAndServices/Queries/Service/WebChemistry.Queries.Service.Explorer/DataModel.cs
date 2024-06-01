namespace WebChemistry.Queries.Service.Explorer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    
    public class AddStructureEntry
    {
        public string Filename { get; set; }
        public Func<Stream> Provider { get; set; }
    }

    public class StructureEntry
    {
        public string Id { get; set; }
        public string[] Warnings { get; set; }
        public int AtomCount { get; set; }
        public int ResidueCount { get; set; }
    }

    class AddStructuresResultInternal
    {
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public List<StructureEntry> Structures { get; set; }

        public AddStructuresResultInternal()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
            Structures = new List<StructureEntry>();
        }
    }

    public class AddStructuresResult
    {
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public string[] NewIdentifiers { get; set; }
        public StructureEntry[] AllStructures { get; set; }

        public AddStructuresResult()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
            NewIdentifiers = new string[0];
            AllStructures = new StructureEntry[0];
        }
    }

    public class PatternEntry
    {
        public string Id { get; set; }
        public int Serial { get; set; }
        public string ParentId { get; set; }

        public int AtomCount { get; set; }
        public string Atoms { get; set; }

        public int ResidueCount { get; set; }
        public string Residues { get; set; }
        public string Signature { get; set; }

        public string SourceJson { get; set; }
    }

    public class QueryResult
    {
        public string Query { get; set; }

        public int QueryTimeMs { get; set; }

        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }

        public StructureEntry[] Structures { get; set; }

        public bool PatternLimitReached { get; set; }
        public int PatternLimit { get; set; }
        public bool PatternAtomLimitReached { get; set; }
        public int PatternAtomLimit { get; set; }

        public bool IsZipAvailable { get; set; }
        public int ZipSizeInBytes { get; set; }

        public int StructureCount { get; set; }

        public List<PatternEntry> Patterns { get; set; }

        public QueryResult()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
            Patterns = new List<PatternEntry>();
        }
    }

    public class AppState
    {
        public string[] QueryHistory { get; set; }
        public StructureEntry[] Structures { get; set; }
        public QueryResult LatestResult { get; set; }
    }
}
