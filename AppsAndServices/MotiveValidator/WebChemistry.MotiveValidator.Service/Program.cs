namespace WebChemistry.MotiveValidator.Service
{
    using System.Linq;
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.MotiveValidator.Database;
    using WebChemistry.MotiveValidator.DataModel;
    using WebChemistry.Platform;
    using WebChemistry.Platform.Server;

    class Program
    {
        #region Tests
        static void TestDb()
        {
            var root = @"I:\test\MotiveValidatorDb\";

            var config = new MotiveValidatorStandaloneConfig
            {
                ValidationType = MotiveValidationType.Database,
                InputFolder = root + "Test_2gud",
                ModelsSource = root + "Test_Models_2gud",
                DatabaseModeMinModelAtomCount = 1,
                MaxDegreeOfParallelism = 1
                //DatabaseModeIgnoreNames = PdbResidue.AminoNamesList.Concat(new[] { "A", "C", "G", "T", "U", "DA", "DC", "DG", "DT", "DU", "HOH", "MSE" }).ToArray()
            };
            
            JsonHelper.WriteJsonFile(root + "config.json", config);
            
            var args = new string[]
            {
                root + "test_result_2gud",
                root + "config.json"
            };
            
            ValidatorService.Run(args);
        }

        static void TestCcd()
        {
            var root = @"I:\test\MotiveValidatorDb\";

            var config = new MotiveValidatorStandaloneConfig
            {
                ValidationType = MotiveValidationType.Database,
                InputFolder = root + "testgz",
                ModelsSource = "i:/test/ligexp/Components-pub.cif.gz",
                DatabaseModeMinModelAtomCount = 7,
                MaxDegreeOfParallelism = 1,
                IsModelsSourceComponentDictionary = true,
                IgnoreObsoleteComponentDictionaryEntries = true,
                DatabaseModeIgnoreNames = PdbResidue.AminoNamesList.Concat(new[] { "A", "C", "G", "T", "U", "DA", "DC", "DG", "DT", "DU", "HOH", "MSE" }).ToArray()
            };

            JsonHelper.WriteJsonFile(root + "config.json", config);

            var args = new string[]
            {
                root + "test_result_gz",
                root + "config.json"
            };

            ValidatorService.Run(args);
        }

        static void TestMan()
        {
            var root = @"E:\test\MotiveValidator\";

            var config = new MotiveValidatorStandaloneConfig
            {
                ValidationType = MotiveValidationType.Model,
                InputFolder = @"E:\test\MotiveValidator\sampledata\man_motives\MAN",
                ModelsSource = @"E:\test\MotiveValidator\sampledata\inputs\MotifsMAN\input\models.zip",
                MaxDegreeOfParallelism = 1
                //DatabaseModeIgnoreNames = PdbResidue.AminoNamesList.Concat(new[] { "A", "C", "G", "T", "U", "DA", "DC", "DG", "DT", "DU", "HOH", "MSE" }).ToArray()
            };

            JsonHelper.WriteJsonFile(root + "config_man.json", config);

            var args = new string[]
            {
                root + "test_result_MAN",
                root + "config_man.json"
            };

            ValidatorService.Run(args);
        }

        static void TestEmpty()
        {
            var root = @"E:\test\MotiveValidator\";

            var config = new MotiveValidatorStandaloneConfig
            {
                ValidationType = MotiveValidationType.Model,
                InputFolder = @"E:\test\MotiveValidator\sampledata\empty\inputs\empty",
                ModelsSource = @"E:\test\MotiveValidator\sampledata\empty\input\models.zip",
                MaxDegreeOfParallelism = 1
                //DatabaseModeIgnoreNames = PdbResidue.AminoNamesList.Concat(new[] { "A", "C", "G", "T", "U", "DA", "DC", "DG", "DT", "DU", "HOH", "MSE" }).ToArray()
            };

            JsonHelper.WriteJsonFile(root + "config_man.json", config);

            var args = new string[]
            {
                root + "test_result_MAN",
                root + "config_man.json"
            };

            ValidatorService.Run(args);
        }

        static void Test188()
        {
            var root = @"I:\test\MotiveValidator\1xkw\";

            var config = new MotiveValidatorStandaloneConfig
            {
                ValidationType = MotiveValidationType.CustomModels,
                InputFolder = @"I:\test\MotiveValidator\1xkw\st",
                ModelsSource = @"I:\test\MotiveValidator\1xkw\mod",
                MaxDegreeOfParallelism = 1
                //DatabaseModeIgnoreNames = PdbResidue.AminoNamesList.Concat(new[] { "A", "C", "G", "T", "U", "DA", "DC", "DG", "DT", "DU", "HOH", "MSE" }).ToArray()
            };

            JsonHelper.WriteJsonFile(root + "config.json", config);

            var args = new string[]
            {
                root + "result",
                root + "config.json"
            };

            ValidatorService.Run(args);
        }

        static void TestAggregatorCreate()
        {
            ServerManager.Init("c:/webchemservers.json");
            var server = ServerManager.GetAppServer("Atlas");
            var app = server.GetOrCreateApp<MotiveValidatorDatabaseApp>("ValidatorDb", svr => MotiveValidatorDatabaseApp.Create("ValidatorDb", svr));

            var comp = app.CreateAggregatorComputation("::1", null, null, "test1");
            //comp.Schedule();
            //ServerManager.MasterServer.ComputationScheduler.Update();
        }

        class TstId
        {
            public EntityId? Id { get; set; }
        }

        static void TestAggregatorRun()
        {
            var args = new[] { "--hosted", "c:/webchemservers.json", "Atlas:users/ValidatorDb/computations/test1" };
            ValidatorService.Run(args);
        }

        #endregion

        static void Main(string[] args)
        {
            //TestCcd();
            //Test188();
            //TestAggregatorCreate();
            //TestAggregatorRun();
            //return;
            //TestDb();
            //TestMan();
            //TestEmpty();
            //return;
            ValidatorService.Run(args);

            ////if (args.Length == 1)
            ////{
            ////    Validator.Run(args);
            ////    return;
            ////}

            ////var config = new MotiveValidatorStandaloneConfig
            ////{
            ////    AutoFindMotives = true,
            ////    InputFolder = @"I:\test\MotiveValidator\input",
            ////    LigandExpoFolder = @"I:\test\LigandExpo_models",
            ////    ModelFilename = ""
            ////};

            ////config = new MotiveValidatorStandaloneConfig
            ////{
            ////    AutoFindMotives = false,
            ////    InputFolder = @"I:\test\SiteBinder\MAN_conn_atoms_2",
            ////    LigandExpoFolder = "",
            ////    ModelFilename = @"I:\test\LigandExpo_models\MAN_model.pdb",
            ////    SummaryOnly = true
            ////};


            //var root = @"I:\test\MotiveValidator\";

            //////////////var config = new MotiveValidatorStandaloneConfig
            //////////////{
            //////////////    AutoFindMotives = false,
            //////////////    InputFolder = root + "FUC",
            //////////////    LigandExpoFolder = "",
            //////////////    ModelFilename = @"C:\TestData\LigExp\FUC_model.pdb",
            //////////////    SummaryOnly = true
            //////////////};

            //var config = new MotiveValidatorStandaloneConfig
            //{
            //    ValidationType = MotiveValidationType.Model,
            //    InputFolder = root + "MAN",
            //    ModelsFolder = root + "MAN_models",
            //    //SummaryOnly = true
            //};

            ////////config = new MotiveValidatorStandaloneConfig
            ////////{
            ////////    ValidationType = MotiveValidationType.Sugars,
            ////////    InputFolder = root + "all_PDBs_wimmerova",
            ////////    LigandExpoFolder = @"I:\test\LigandExpo_models\",
            ////////    ModelFilename = "",
            ////////    //SummaryOnly = true
            ////////};

            //JsonHelper.WriteJsonFile(root + "config.json", config);

            //////////if (Directory.Exists(root + "result")) Directory.Delete(root + "result", true);

            //////args = new string[]
            //////{
            //////    root + "result",
            //////    root + "config.json"
            //////};

            //args = new string[]
            //{
            //    @"I:\test\MotiveValidator\result_man",
            //    @"I:\test\MotiveValidator\config.json"
            //};
            //////args = new [] { "--wiki-help", "i:/test/MotiveValidator/wikihelp.txt" };
            //args = new[] { "--help" };
            
            
            //ValidatorService.Run(args);
        }
    }
}
