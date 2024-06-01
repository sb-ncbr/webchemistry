using System.Collections.Generic;
using WebChemistry.Charges.Service.DataModel;
using WebChemistry.Platform;

namespace WebChemistry.Charges.Service
{
    class Program
    {
        static void Main(string[] args)
        {
            ////var config = new ChargesServiceStandaloneConfig
            ////{
            ////    InputFolder = @"I:\test\EEMOnline\Service\in_1tqn",
            ////    SetsXmlFilename = @"I:\test\EEMOnline\Service\DefaultSets.xml",
            ////    Sets = new[] { 
            ////        //new ChargeSetConfig
            ////        //{
            ////        //    Name = "RS2-E",
            ////        //    Method = Core.ChargeComputationMethod.Eem
            ////        //},
            ////        new ChargeSetConfig
            ////        {
            ////            Name = "RS2-EX",
            ////            Method = Core.ChargeComputationMethod.EemCutoffCover,
            ////            CutoffRadius = 13.0,
            ////            Precision = Core.ChargeComputationPrecision.Double
            ////        },
            ////        new ChargeSetConfig
            ////        {
            ////            Name = "RS2-E",
            ////            Method = Core.ChargeComputationMethod.Eem,
            ////            Precision = Core.ChargeComputationPrecision.Double
            ////        }
            ////    },
            ////    Jobs = new[]
            ////    {
            ////        new ChargesServiceJob { Id = "1tqn", TotalCharges = new [] { -6.0 } },
            ////        //new ChargesServiceJob { Id = "2x43", TotalCharges = new [] { -60.0 } }
            ////    },
            ////    //TotalCharges = new Dictionary<string, double[]> { { "1IRU-wo-water-ion", new[] { -52.0 } } },
            ////    ExportUncompressed = false,
            ////    MaxDegreeOfParallelism = 4,
            ////    //MaxFullEemAtomCount = 3
            ////};

            ////var configFilename = "i:/test/EEMOnline/Service/config_1tqn.json";
            ////JsonHelper.WriteJsonFile(configFilename, config);

            ////args = new[] { @"I:\test\EEMOnline\Service\out_1tqn", configFilename };

            ChargesService.Run(args);
        }
    }
}
