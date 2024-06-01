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
    public class PlatformServer : ServerBase
    {   
        UserManager users;
        /// <summary>
        /// Users.
        /// </summary>
        public UserManager Users
        {
            get
            {
                return users = users ?? UserManager.Load(GetServerEntityId("users"));
            }
        }

        /// <summary>
        /// Loads a server.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static PlatformServer Load(ServerInfo info)
        {           
            return new PlatformServer(info.Name, info.Root);
        }

        private PlatformServer(string name, string root)
            : base(name, root)
        {

        }
    }
}
