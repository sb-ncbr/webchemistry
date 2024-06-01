namespace WebChemistry.Platform.Server
{
    using System;
    using System.IO;
    using WebChemistry.Platform.Computation;
    using WebChemistry.Platform.MoleculeDatabase;
    using WebChemistry.Platform.Services;
    using WebChemistry.Platform.Users;

    /// <summary>
    /// Application Server.
    /// </summary>
    public class AppServer : ServerBase
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
        /// Get or create an app.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="creator"></param>
        /// <returns></returns>
        public T GetOrCreateApp<T>(string name, Func<AppServer, T> creator)
            where T : PersistentObjectBase<T>, new()
        {
            var ret = PersistentObjectBase<T>.TryLoad(GetAppId(name));
            if (ret == null) ret = creator(this);
            return ret;
        }

        /// <summary>
        /// Get an app.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public T GetApp<T>(string name)
            where T : PersistentObjectBase<T>, new()
        {
            return PersistentObjectBase<T>.Load(GetAppId(name));
        }

        /// <summary>
        /// Try Get an app.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public T TryGetApp<T>(string name)
            where T : PersistentObjectBase<T>, new()
        {
            return PersistentObjectBase<T>.TryLoad(GetAppId(name));
        }

        /// <summary>
        /// Get entity ID of the app.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public EntityId GetAppId(string name)
        {
            return GetServerChildId("apps", name);
        }

        /// <summary>
        /// Loads a server.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static AppServer Load(ServerInfo info)
        {
            return new AppServer(info.Name, info.Root);
        }

        private AppServer(string name, string root)
            : base(name, root)
        {

        }
    }
}
