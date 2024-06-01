namespace WebChemistry.MotiveValidator.DataModel
{
    using Newtonsoft.Json;
    using System.ComponentModel;
    using WebChemistry.Platform;
    using WebChemistry.Platform.Services;

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [HelpDescribe, HelpTypeName("ValidationTypes")]
    public enum MotiveValidationType
    {
        [Description("Analyzes the input biomolecule fragments (motifs) against the provided model using the ModelFilename attribute.")]
        Model = 0,

        [Description("When running from the command line, identical to the CustomModels mode.")] //"Automatically identifies and analyzes sugars inside the input biomolecules (with models from the ModelsFolder attribute).")]
        Sugars,

        [Description("Automatically identifies and validates input models within the PDB structures.")]
        CustomModels,

        [Description("Special mode for creating ValidatorDB.")]
        Database,

        [Description("Special mode for creating custom ValidatorDB analysis.")]
        DatabaseCustom
    }

    public class MotiveValidatorConfig
    {
        public MotiveValidationType ValidationType { get; set; }
        public EntityId? SugarModelsView { get; set; }

        public string DatabaseModeServerName { get; set; }
        public string DatabaseModeAppName { get; set; }
        public string[] DatabaseModeCustomModelIds { get; set; }
        public string[] DatabaseModeCustomStructureIds { get; set; }

        public int MaxDegreeOfParallelism { get; set; }

        public MotiveValidatorConfig()
        {
            MaxDegreeOfParallelism = 8;
        }
    }
    
    public class MotiveValidatorStandaloneConfig
    {
        [Description("Type of the validation.")]
        [DefaultValue(MotiveValidationType.Model)]
        [HelpDescribe]
        public MotiveValidationType ValidationType { get; set; }

        [Description("Folder with the input data. Each molecule to be validated has to be in a separate file. Supported file formats: *.pdb; *.pdbX, *.cif, *.pqr, *.mol. Gzip compression (*.gz) is also supported.")]
        public string InputFolder { get; set; }

        [Description("Folder or zip file with models in PDB (and SD/SDF/MOL for bonds) format. Alternatively, use the new PDB standard mmCIF (*.cif).")]
        public string ModelsSource { get; set; }

        [Description("Determines if the ModelsSource is a CIF file containing models to be validated.")]
        public bool IsModelsSourceComponentDictionary { get; set; }

        [Description("Determines if to ignore Component Dictionary entries with _chem_comp.pdbx_release_status = OBS flag.")]
        public bool IgnoreObsoleteComponentDictionaryEntries { get; set; }

        [Description("Determines whether to compute only the summary file and not files with motifs.")]
        [DefaultValue(false)]
        public bool SummaryOnly { get; set; }
        
        [Description("Determines the minimum number of atoms a model needs to have.")]
        public int DatabaseModeMinModelAtomCount { get; set; }

        [Description("Residues to ignore in the database mode.")]
        public string[] DatabaseModeIgnoreNames { get; set; }

        [Description("Max degree of parallelism.")]
        [DefaultValue(8)]
        public int MaxDegreeOfParallelism { get; set; }

        public MotiveValidatorStandaloneConfig()
        {
            DatabaseModeIgnoreNames = new string[0];
            MaxDegreeOfParallelism = 8;
        }
    }
}
