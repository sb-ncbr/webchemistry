using System.Linq;
using WebChemistry.Platform;
using WebChemistry.Platform.Computation;
using WebChemistry.Platform.MoleculeDatabase;
using WebChemistry.Platform.MoleculeDatabase.Filtering;
using WebChemistry.Platform.Server;
using WebChemistry.Platform.Users;

namespace WebChemistry.MotiveValidator.DataModel
{
    public class MotiveValidatorApp : PersistentObjectBase<MotiveValidatorApp>
    {
        public EntityId UserId { get; set; }
        public string[] RecentComputations { get; set; }
        public EntityId SugarModelsView { get; set; }
        public EntityId AllModelsView { get; set; }
        public EntityId ComputationsId { get; set; }

        public ComputationInfo CreateComputation(string source, MotiveValidationType validationType, string customId = null)
        {
            var user = UserInfo.Load(UserId);
            //var dataFolder = repository.GetEntityPath();
            //Directory.CreateDirectory(dataFolder);

            var config = new MotiveValidatorConfig
                {
                    ValidationType = validationType,
                    SugarModelsView = SugarModelsView
                };

            var svc = ServerManager.MasterServer.Services.GetService("MotiveValidator");
            var comp = user.Computations.CreateComputation(user, svc, "MotiveValidatorComputation", config, source /*, dependentObjects: new[] { repository } */, customId: customId);

            this.RecentComputations = new[] { comp.ShortId }.Concat(RecentComputations).Take(10).ToArray();
            this.Save();

            //return Tuple.Create(dataFolder, comp);
            return comp;
        }

        public static MotiveValidatorApp Create(string name, DatabaseInfo wwPdbCcd, AppServer server)
        {
            var user = server.Users.GetOrCreateUserByName(name);
            var profile = user.GetProfile();
            profile.ConcurrentComputationLimit = 16;
            user.SaveProfile(profile);
            var id = server.GetAppId(name);
            var ret = CreateAndSave(id, x => 
            {
                x.UserId = user.Id;
                x.ComputationsId = user.Computations.Id;
                x.RecentComputations = new string[0];
            });

            var viewSugars = DatabaseView.CreateCustom(wwPdbCcd, ret.Id.GetChildId("view_sugars"), "sugars",
                filters: new[] { EntryFilter.Create(DatabaseIndexEntry.RingFingerprintsPropertyName, FilterPropertyType.StringArray, FilterComparisonType.StringEqual, "@(C,C,C,C,O)|@(C,C,C,C,C,O)") });
            var viewAll = DatabaseView.CreateCustom(wwPdbCcd, ret.Id.GetChildId("view_all"), "all");

            ret.SugarModelsView = viewSugars.Id;
            ret.AllModelsView = viewAll.Id;
            ret.Save();
            
            return ret;
        }
    }
}
