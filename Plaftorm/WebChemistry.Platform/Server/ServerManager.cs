namespace WebChemistry.Platform.Server
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    
    /// <summary>
    /// Server manager.
    /// </summary>
    public static class ServerManager
    {
        static string configurationPath;
        /// <summary>
        /// Configuration file path.
        /// </summary>
        public static string ConfigurationPath
        {
            get
            {
                if (!initialized) throw new InvalidOperationException("The server manager is not initialized.");
                return configurationPath;
            }
        }

        static bool initialized = false;

        static Dictionary<string, ServerInfo> servers;
        static Dictionary<string, ServerBase> serverCache = new Dictionary<string, ServerBase>(StringComparer.Ordinal);

        /// <summary>
        /// Get entity path.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string GetEntityPath(EntityId id)
        {
            if (!initialized) throw new InvalidOperationException("The server manager is not initialized.");

            if (id.ServerName.Equals(MasterServer.Name, StringComparison.Ordinal))
            {
                return masterServer.Root + id.Value;
            }

            ServerInfo info;
            if (servers.TryGetValue(id.ServerName, out info))
            {
                return info.Root + id.Value;
            }

            throw new ArgumentException(string.Format("Server '{0}' was not found.", id.ServerName));
        }

        static MasterServer masterServer = null;
        /// <summary>
        /// There can be only one.
        /// </summary>
        public static MasterServer MasterServer
        {
            get
            {
                if (!initialized) throw new InvalidOperationException("The server manager is not initialized.");
                return masterServer;
            }
        }

        /// <summary>
        /// Get a platform server by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static PlatformServer GetPlatformServer(string name)
        {
            if (!initialized) throw new InvalidOperationException("The server manager is not initialized.");

            ServerBase server;
            if (serverCache.TryGetValue(name, out server)) return (PlatformServer)server;

            ServerInfo info;
            if (servers.TryGetValue(name, out info))
            {
                server = PlatformServer.Load(info);
                serverCache[name] = server;
                return (PlatformServer)server;
            }

            throw new ArgumentException(string.Format("Server called '{0}' was not found.", name));
        }

        /// <summary>
        /// Get an app server by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static AppServer GetAppServer(string name)
        {
            if (!initialized) throw new InvalidOperationException("The server manager is not initialized.");

            ServerBase server;
            if (serverCache.TryGetValue(name, out server)) return (AppServer)server;

            ServerInfo info;
            if (servers.TryGetValue(name, out info))
            {
                server = AppServer.Load(info);
                serverCache[name] = server;
                return (AppServer)server;
            }

            throw new ArgumentException(string.Format("Server called '{0}' was not found.", name));
        }
        
        /// <summary>
        /// Load the server manager from a json file.
        /// </summary>
        /// <param name="filename"></param>
        public static void Init(string filename)
        {            
            var info = JsonHelper.ReadJsonFile<ServerManagerInfo>(filename, false);

            var dirs = new[] { info.Master.Root }.Concat(info.Servers.Select(s => s.Root)).ToArray();

            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            }

            ServerManager.serverCache.Clear();
            ServerManager.configurationPath = filename;
            ServerManager.servers = info.Servers.ToDictionary(s => s.Name, StringComparer.Ordinal);
            ServerManager.masterServer = MasterServer.Load(info.Master);

            //if (useObjectCache) PersistentObjectStringCache.Init(dirs);

            initialized = true;
        }
    }
}
