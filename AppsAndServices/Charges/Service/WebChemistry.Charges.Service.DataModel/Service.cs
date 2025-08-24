namespace WebChemistry.Charges.Service.DataModel
{
    using System.Collections.Generic;

    public class ChargesServiceComputationEntrySummary
    {
        public string Id { get; set; }
        public bool IsValid { get; set; }
        public Dictionary<string, int> AtomCounts { get; set; }

        public long TimingMs { get; set; }

        public string[] ReaderWarnings { get; set; }
        public string[] ComputationWarnings { get; set; }
        public string[] Errors { get; set; }

        public ChargesServiceComputationEntrySummary()
        {
            AtomCounts = new Dictionary<string, int>();
        }
    }

    public class ChargesServiceStructureData
    {
        public ChargesServiceComputationEntrySummary Summary { get; set; }

        public Dictionary<int, int> BondTypes { get; set; }
        public Dictionary<string, StructureAtomCharges> Charges { get; set; }
        public Dictionary<string, StructureAtomPartition> Partitions { get; set; }

        public string StructureJson { get; set; }
    }
    
    public class ChargesServiceComputationSummary
    {
        public string Version { get; set; }
        public long TimingMs { get; set; }
        public string[] Errors { get; set; }
        public string[] Warnings { get; set; }

        public ChargesServiceComputationEntrySummary[] Entries { get; set; }

        public ChargesServiceComputationSummary()
        {
            Entries = new ChargesServiceComputationEntrySummary[0];
        }
    }
}
