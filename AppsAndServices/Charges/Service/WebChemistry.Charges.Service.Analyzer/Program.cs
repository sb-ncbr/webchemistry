namespace WebChemistry.Charges.Service.Analyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            //var set = WebChemistry.Charges.Core.EemParameterSet.FromXml(System.Xml.Linq.XElement.Parse(System.IO.File.ReadAllText("e:/test/sets_pg1.xml")).Elements().First());
            //System.IO.File.WriteAllText("e:\\Test\\sets_pg1.json", AnalyzerUtils.ConvertSetToEntry(set).ToJsonString());
            //return;
            //var configFilename = "i:/test/EEMOnline/analyzer/config.json";
            //args = new[]
            //{
            //    "i:/test/EEMOnline/analyzer/out",
            //    configFilename
            //};

            //var config = new ChargesAnalyzerStandaloneConfig
            //{
            //    InputFolder = "i:/test/EEMOnline/analyzer/in"
            //};

            //JsonHelper.WriteJsonFile(configFilename, config);

            AnalyzerService.Run(args);
        }
    }
}
