namespace WebChemistry.Charges.Service.DataModel
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ChargeAnalyzerStructureCategory
    {
        SmallMolecule,
        Protein
    }
    
    public class ChargesAnalyzerResultEntry
    {
        public string StructureId { get; set; }

        public bool IsValid { get; set; }
        public ChargeAnalyzerStructureCategory[] StructureCategories { get; set; }

        public bool HasBuiltInReferenceCharges { get; set; }
        public string[] ReferenceChargeFilenames { get; set; }
                
        public Dictionary<string, int> AtomCounts { get; set; }
        public int BondCount { get; set; }

        public int SuggestedCharge { get; set; }

        public string[] Warnings { get; set; }
        public string[] ReferenceChargeWarnings { get; set; }

        public string ErrorText { get; set; }

        public ChargesAnalyzerResultEntry()
        {
            ReferenceChargeFilenames = new string[0];
            AtomCounts = new Dictionary<string, int>();
            Warnings = new string[0];
            ReferenceChargeWarnings = new string[0];
            ErrorText = "";
        }
    }

    public class ChargeAnalyzerParameterSetEntry
    {
        public string Name { get; set; }
        public string[] AvailableAtoms { get; set; }
        public string[][] Properties { get; set; }
        public string Xml { get; set; }
    }

    public class ChargesAnalyzerResult
    {
        public ChargesAnalyzerResultEntry[] Entries { get; set; }

        public string[] Errors { get; set; }
        public string[] Warnings { get; set; }

        public ChargeAnalyzerParameterSetEntry[] ParameterSets { get; set; }
        public ChargeAnalyzerParameterSetEntry EmptySetTemplate { get; set; }
    }
}
