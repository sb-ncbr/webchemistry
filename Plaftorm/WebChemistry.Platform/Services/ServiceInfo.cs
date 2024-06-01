namespace WebChemistry.Platform.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using WebChemistry.Platform.Computation;
    using System.Diagnostics;
    using WebChemistry.Framework.Core;

    /// <summary>
    /// Service info.
    /// </summary>
    public class ServiceInfo : ManagedPersistentObjectBase<ServiceInfo, ServiceInfo.Index, object>
    {
        /// <summary>
        /// Index entry.
        /// </summary>
        public class Index { public static readonly Index Instance = new Index(); }

        /// <summary>
        /// Name of the service.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Index Entry.
        /// </summary>
        internal override ServiceInfo.Index IndexEntry
        {
            get { return Index.Instance; }
        }

        /// <summary>
        /// Name of the executable.
        /// </summary>
        public string ExecutableName { get; set; }

        /// <summary>
        /// The sub-directory where the service is stored.
        /// </summary>
        public string ExecutableDirectory { get; set; }

        /// <summary>
        /// Default computation priority.
        /// </summary>
        public ComputationPriority DefaultComputationPriority { get; set; }

        /// <summary>
        /// Version of the service.
        /// </summary>
        public AppVersion CurrentVersion { get; set; }

        /// <summary>
        /// Versions of the app. (version, guid)
        /// </summary>
        public Dictionary<AppVersion, string> Versions { get; set; }

        /// <summary>
        /// Get the executable path.
        /// </summary>
        /// <returns></returns>
        public string GetExecutablePath()
        {
            return Path.Combine(CurrentDirectory, ExecutableDirectory, ExecutableName);
        }

        /// <summary>
        /// Gets the path to the current version of the service.
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public string GetServicePath(AppVersion version)
        {
            return Path.Combine(CurrentDirectory, Versions[version]);
        }

        /// <summary>
        /// Gets the path to the current version of the service.
        /// </summary>
        /// <returns></returns>
        public string GetServicePath()
        {
            return Path.Combine(CurrentDirectory, ExecutableDirectory);
        }

        /// <summary>
        /// Gets the string version of the change log.
        /// </summary>
        /// <returns></returns>
        public string GetChangeLog()
        {
            var path = Path.Combine(CurrentDirectory, ExecutableDirectory, "changelog.txt");
            if (File.Exists(path)) return File.ReadAllText(path);
            return "Not available";
        }

        /// <summary>
        /// Get path of the current version.
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public string GetVersionPath(AppVersion version)
        {
            string path;
            if (Versions.TryGetValue(version, out path)) return Path.Combine(CurrentDirectory, path);
            throw new ArgumentException(string.Format("Version '{0}' not found.", version));
        }

        internal override void UpdateAndSaveInternal(object model)
        {
            throw new NotSupportedException();
        }

        static AppVersion GetVersion(string filename)
        {
            try
            {
                using (var process = Process.Start(new ProcessStartInfo(filename, "--version")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }))
                {
                    string ret = process.StandardOutput.ReadLine();
                    process.WaitForExit();
                    return AppVersion.Parse(ret);
                }
            }
            catch
            {
                return AppVersion.Unknown;
            }
        }

        /// <summary>
        /// Create a service.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="name"></param>
        /// <param name="executableName"></param>
        /// <param name="defaultCompPririty"></param>
        /// <param name="sourceFolder"></param>
        /// <returns></returns>
        internal static ServiceInfo Create(ServiceManager manager, string name, string executableName,
            ComputationPriority defaultCompPririty, string sourceFolder)
        {
            if (!File.Exists(Path.Combine(sourceFolder, executableName)))
            {
                throw new InvalidOperationException(string.Format("Executable '{0}' is missing from the folder '{1}'", executableName, sourceFolder));
            }

            var existingSvc = ServiceInfo.TryLoad(manager.Id.GetChildId(ServiceManager.GetIdFromName(name)));
            var history = existingSvc != null ? existingSvc.Versions : new Dictionary<AppVersion, string>();
            var version = GetVersion(Path.Combine(sourceFolder, executableName));
            
            if (existingSvc != null && existingSvc.CurrentVersion == version)
            {
                throw new InvalidOperationException(string.Format("Cannot update service '{0}' to the same version ({1}).", name, version));
            }

            var svcSubDir = Guid.NewGuid().ToString();
            var svc = CreateAndSave(manager.Id, ServiceManager.GetIdFromName(name), newSvc =>
            {
                newSvc.Name = name;
                newSvc.ExecutableName = executableName;
                newSvc.ExecutableDirectory = svcSubDir;
                newSvc.CurrentVersion = version;
                newSvc.Versions = history;
                history[version] = svcSubDir;
                newSvc.DefaultComputationPriority = defaultCompPririty;
            });

            // keep the old versions??
            ////foreach (var dir in Directory.EnumerateDirectories(svc.CurrentDirectory))
            ////{
            ////    try
            ////    {
            ////        Directory.Delete(dir, true);
            ////    }
            ////    catch
            ////    {

            ////    }
            ////}

            var scvDir = Path.Combine(svc.CurrentDirectory, svcSubDir);
            Directory.CreateDirectory(scvDir);
            Helpers.DirectoryCopy(sourceFolder, scvDir, true);

            return svc;
        }

        public ServiceInfo()
        {
            this.Versions = new Dictionary<AppVersion, string>();
        }
    }
}
