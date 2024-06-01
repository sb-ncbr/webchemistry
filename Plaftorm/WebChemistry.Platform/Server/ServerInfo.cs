namespace WebChemistry.Platform.Server
{
    using System;
    using System.IO;

    /// <summary>
    /// Server info.
    /// </summary>
    public class ServerInfo
    {
        /// <summary>
        /// Server name.
        /// </summary>
        public string Name { get; set; }

        string serverRoot;
        /// <summary>
        /// Base path prefix for all platform services.
        /// Should be an absolute path.
        /// Always ends with \ or /.
        /// </summary>
        public string Root
        {
            get { return serverRoot; }
            set
            {
                serverRoot = Path.GetFullPath(value);
                var dir = Path.GetDirectoryName(serverRoot);
                if (serverRoot != dir
                    && !serverRoot.EndsWith("/", StringComparison.InvariantCulture)
                    && !serverRoot.EndsWith("\\", StringComparison.InvariantCulture))
                {
                    serverRoot += Path.DirectorySeparatorChar;
                }
            }
        }
    }

    /// <summary>
    /// Server manager info.
    /// </summary>
    public class ServerManagerInfo
    {
        /// <summary>
        /// Master info.
        /// </summary>
        public ServerInfo Master { get; set; }

        /// <summary>
        /// Servers.
        /// </summary>
        public ServerInfo[] Servers { get; set; }
    }
}
