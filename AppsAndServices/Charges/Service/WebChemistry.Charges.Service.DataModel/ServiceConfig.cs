namespace WebChemistry.Charges.Service.DataModel
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using WebChemistry.Charges.Core;
    using WebChemistry.Platform.Services;

    [HelpDescribe, HelpTypeName("SetConfig")]
    public class ChargeSetConfig
    {
        [Description("The name of the set.")]
        public string Name { get; set; }

        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        
        //[HelpDescribe]
        [Description("Computation method.")]
        public ChargeComputationMethod Method { get; set; }
        
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        //[HelpDescribe]
        [Description("Precision (Double is 64bit, Single is 32bit).")]
        public ChargeComputationPrecision Precision { get; set; }

        [Description("Cutoff radius parameter for methods that require it.")]
        public double CutoffRadius { get; set; }

        [Description("Determines whether to adjust the total charge during the cutoff computations.")]
        [DefaultValue(true)]
        public bool CorrectCutoffTotalCharge { get; set; }

        [Description("Determines whether to ignore water atoms or not.")]
        public bool IgnoreWaters { get; set; }

        public ChargeSetConfig()
        {
            CorrectCutoffTotalCharge = true;
        }
    }

    [HelpDescribe, HelpTypeName("Job")]
    public class ChargesServiceJob
    {
        [Description("Id of the job is the filename of the molecule without extension.")]
        public string Id { get; set; }

        [Description("A list of total charges that need to be computed.")]
        public double[] TotalCharges { get; set; }
    }
    
    public class ChargesServiceConfigBase
    {
        [Description("A list of sets that will be computed."), HelpDescribe(typeof(ChargeSetConfig)), HelpTypeName("SetConfig")]
        public ChargeSetConfig[] Sets { get; set; }

        [Description("A list of job that need to be computed."), HelpDescribe(typeof(ChargesServiceJob)), HelpTypeName("Job")]
        public ChargesServiceJob[] Jobs { get; set; }

        [Description("A maximum number of atoms a molecule can have to be eligible for the 'Eem' method. 30000 atoms molecule consumes about 8GB of memory using the Double precision.")]
        [DefaultValue(30000)]
        public int MaxFullEemAtomCount { get; set; }

        [Description("Maximum number of parallel tasks that can be executed simultaneously. Recommended to be set to the number of cores your system has.")]
        [DefaultValue(8)]
        public int MaxDegreeOfParallelism { get; set; }

        public ChargesServiceConfigBase()
        {
            Sets = new ChargeSetConfig[0];
            Jobs = new ChargesServiceJob[0];
            MaxDegreeOfParallelism = 8;
            MaxFullEemAtomCount = 30000;
        }
    }

    public class ChargesServiceConfig : ChargesServiceConfigBase
    {
        public string[] SetsXml { get; set; }        
    }

    public class ChargesServiceStandaloneConfig : ChargesServiceConfigBase
    {
        [Description("Path to a folder containing the input data.")]
        public string InputFolder { get; set; }

        [Description("Path to a file containing the parameter sets.")]
        public string SetsXmlFilename { get; set; }

        [Description("If true, the output will not be compressed in ZIP.")]
        public bool ExportUncompressed { get; set; }
    }
}
