namespace WebChemistry.Platform.Server
{
    using System;
    using System.IO;
    using WebChemistry.Platform.Computation;
    using WebChemistry.Platform.MoleculeDatabase;
    using WebChemistry.Platform.Services;
    using WebChemistry.Platform.Users;

    /// <summary>
    /// Server.
    /// </summary>
    public class MasterServer : ServerBase
    {        
        DatabaseManager databases;
        /// <summary>
        /// Public database manager.
        /// </summary>
        public DatabaseManager PublicDatabases
        {
            get
            {
                return databases = databases ?? DatabaseManager.Load(GetServerEntityId("databases"));
            }
        }

        /// <summary>
        /// Public database views.
        /// </summary>
        public DatabaseViewManager PublicDatabaseViews
        {
            get
            {
                return DatabaseViewManager.Load(GetServerEntityId("dbviews"));
            }
        }

        ComputationScheduler compScheduler;
        /// <summary>
        /// Computation scheduler.
        /// </summary>
        public ComputationScheduler ComputationScheduler
        {
            get
            {
                return compScheduler = compScheduler ?? ComputationScheduler.Open(GetServerEntityId("scheduler"));  //ComputationScheduler.Load(GetServerEntityId("scheduler"));
            }
        }

        ServiceManager services;
        /// <summary>
        /// Platform services.
        /// </summary>
        public ServiceManager Services
        {
            get
            {
                return services = services ?? ServiceManager.Load(GetServerEntityId("services"));
            }
        }

        /// <summary>
        /// Loads a server.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static MasterServer Load(ServerInfo info)
        {
            return new MasterServer(info.Name, info.Root);
        }

        private MasterServer(string name, string root)
            : base(name, root)
        {

        }
    }
}
