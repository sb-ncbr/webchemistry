// -----------------------------------------------------------------------
// <copyright file="ComputationInfo.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace WebChemistry.Platform.Computation
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.IO;
    using Newtonsoft.Json;
    using WebChemistry.Platform.Services;
    using WebChemistry.Platform.Users;
    using WebChemistry.Platform.Server;
    using System.Collections.Generic;
    using WebChemistry.Framework.Core;


    /// <summary>
    /// Computation process info;
    /// </summary>
    public class ComputationProcessInfo
    {
        /// <summary>
        /// Computation process id.
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// Computation start time.
        /// </summary>
        public DateTime StartTimeUtc { get; set; } 
    }

    /// <summary>
    /// Computation info.
    /// </summary>
    public class ComputationInfo : ManagedPersistentObjectBase<ComputationInfo, ComputationInfo.Index, object>
    {
        /// <summary>
        /// Index entry.
        /// </summary>
        public class Index
        {
            public EntityId ServiceId { get; set; }
            public ComputationPriority Priority { get; set; }
        }

        /// <summary>
        /// Filename of the status object.
        /// </summary>
        public static readonly string StatusFilename = "status.json";

        /// <summary>
        /// Filename of the input parameters object.
        /// </summary>
        public static readonly string SettingsFilename = "settings.json";

        /// <summary>
        /// Filename of the process info object.
        /// </summary>
        public static readonly string ProcessInfoFilename = "processinfo.json";

        /// <summary>
        /// Filename of the apps standard output dump.
        /// </summary>
        public static readonly string LogFilename = "log.txt";
        
        /// <summary>
        /// Result folder name.
        /// </summary>
        public static readonly string ResultFolderName = "result";
        
        string GetStatusPath() { return Path.Combine(CurrentDirectory, StatusFilename); }
        public string GetSettingsPath() { return Path.Combine(CurrentDirectory, SettingsFilename); }
        internal string GetProcessInfoPath() { return Path.Combine(CurrentDirectory, ProcessInfoFilename); }
        string GetLogPath() { return Path.Combine(CurrentDirectory, LogFilename); }
                
        /// <summary>
        /// Computation name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Index entry.
        /// </summary>
        internal override ComputationInfo.Index IndexEntry
        {
            get { return new Index { ServiceId = ServiceId, Priority = Priority }; }
        }

        /// <summary>
        /// Service path.
        /// </summary>
        public EntityId ServiceId { get; set; }
        
        /// <summary>
        /// Computation type.
        /// </summary>
        public ComputationPriority Priority { get; set; }

        /// <summary>
        /// User platform path.
        /// </summary>
        public EntityId UserId { get; set; }

        /// <summary>
        /// Where was the computation created from.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Objects the computation depends on. Such as database snapshots.
        /// These objects are deleted when the computation is deleted.
        /// </summary>
        public EntityId[] DependentObjectIds { get; set; }

        /// <summary>
        /// Computation's custom state (useful for multiple state computations).
        /// </summary>
        public string CustomStateJson { get; set; }

        /// <summary>
        /// Reads the custom state.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <returns></returns>
        public TState GetCustomState<TState>()
            where TState : class, new()
        {
            if (string.IsNullOrEmpty(CustomStateJson)) return null;
            return JsonHelper.FromJsonString<TState>(CustomStateJson);
        }

        /// <summary>
        /// Sets the custom state.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="state"></param>
        public void SetCustomStateAndSave<TState>(TState state)
            where TState : class, new()
        {
            this.CustomStateJson = state.ToJsonString(prettyPrint: false);
            this.Save();
        }

        /// <summary>
        /// Get the underlaying service information.
        /// </summary>
        /// <returns></returns>
        public ServiceInfo GetService()
        {
            return ServiceInfo.Load(ServiceId);
        }

        internal override void UpdateAndSaveInternal(object model)
        {
            throw new NotSupportedException();
        }

        protected override void OnObjectLoaded()
        {
            if (!IsRunning()) CheckStatus(false);
        }

        void CheckStatus(bool running)
        {
            var status = GetJsonStatus();
            if (running && status.State == ComputationState.Running)
            {
                status.State = ComputationState.Terminated;
                status.Message = "The computation has terminated for unknown reason (for example a server crash).";
                status.Save();
            }
        }

        /// <summary>
        /// Get the computation's process.
        /// </summary>
        /// <returns></returns>
        public Maybe<Process> GetProcess()
        {
            var pinfo = GetProcessInfo();
            try
            {
                var p = Process.GetProcessById(pinfo.ProcessId);
                if (p.StartTime.ToUniversalTime().Equals(pinfo.StartTimeUtc)) return Maybe.Just(p);
            }
            catch
            {
            }
            return Maybe.Nothing<Process>();
        }

        /// <summary>
        /// Determine if the computation is running by checking the processes.
        /// </summary>
        /// <returns></returns>
        public bool IsRunning()
        {
            if (!File.Exists(GetProcessInfoPath()))
            {
                CheckStatus(false);
                return false; 
            }

            var pinfo = GetProcessInfo();
            bool running = false;
            try
            {
                var p = Process.GetProcessById(pinfo.ProcessId);
                if (p.StartTime.ToUniversalTime().Equals(pinfo.StartTimeUtc)) running = true;
            }
            catch 
            {
            }

            // if the computation is not running, we can delete the file.
            if (!running)
            {
                File.Delete(GetProcessInfoPath());
                CheckStatus(false);
            }

            return running;
        }

        internal ComputationProcessInfo GetProcessInfo()
        {
            return JsonHelper.ReadJsonFile<ComputationProcessInfo>(GetProcessInfoPath());
        }
        
        /// <summary>
        /// Read the computaion input parameters from file.
        /// </summary>
        /// <typeparam name="TSettings"></typeparam>
        /// <returns></returns>
        public TSettings GetSettings<TSettings>()
            where TSettings : class
        {
            return JsonConvert.DeserializeObject<TSettings>(File.ReadAllText(GetSettingsPath()));
        }

        /// <summary>
        /// Updates the settings of the computation.
        /// </summary>
        /// <typeparam name="TSettings"></typeparam>
        /// <param name="settings"></param>
        public void UpdateSettings<TSettings>(TSettings settings)
        {
            JsonHelper.WriteJsonFile(this.GetSettingsPath(), settings);
        }

        /// <summary>
        /// Get the apps stdout.
        /// </summary>
        /// <returns></returns>
        public string GetLogFilename()
        {
            return GetLogPath();
        }
        
        /// <summary>
        /// Creates the input directory and return the path to it.
        /// </summary>
        /// <returns></returns>
        public string MakeInputDirectory()
        {
            var path = Path.Combine(CurrentDirectory, "input");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Creates the input directory and return the path to it.
        /// </summary>
        /// <returns></returns>
        public string GetInputDirectory()
        {
            return GetInputDirectoryId().GetEntityPath();
        }

        /// <summary>
        /// Creates the input directory and returns it's entity id.
        /// </summary>
        /// <returns></returns>
        public EntityId GetInputDirectoryId()
        {
            var id = Id.GetChildId("input");
            var path = Path.Combine(CurrentDirectory, "input");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return id;
        }

        /// <summary>
        /// Get the result folder entity id.
        /// </summary>
        /// <returns></returns>
        public EntityId GetResultFolderId()
        {
            return Id.GetChildId("result");
        }

        /// <summary>
        /// Read the computation status from file.
        /// </summary>
        /// <returns></returns>
        public ComputationStatus GetStatus()
        {
            return JsonHelper.ReadJsonFile<ComputationStatus>(GetStatusPath()); 
        }

        internal JsonComputationStatus GetJsonStatus()
        {
            return JsonComputationStatus.Open(GetStatusPath());
        }

        /// <summary>
        /// Schedule the computation.
        /// </summary>
        public void Schedule()
        {
            ServerManager.MasterServer.ComputationScheduler.Schedule(this);
        }

        /// <summary>
        /// Fails the computation with a given message.
        /// </summary>
        /// <param name="message"></param>
        public void Fail(string message)
        {
            KillIfRunning(true);
            var status = JsonComputationStatus.Open(GetStatusPath());
            status.State = ComputationState.Failed;
            status.Message = message;
            status.Save();
        }

        /// <summary>
        /// Kill the computation.
        /// </summary>
        public void KillIfRunning(bool waitForExit = false)
        {
            if (IsRunning()) ServerManager.MasterServer.ComputationScheduler.Kill(this, waitForExit);            
        }
        
        /// <summary>
        /// Creates a computation in the given folder and returns its id.
        /// </summary>
        /// <typeparam name="TSettings"></typeparam>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <param name="service"></param>
        /// <param name="name"></param>
        /// <param name="settings"></param>
        /// <param name="customPriority"></param>
        /// <param name="dependentObjects"></param>
        /// <param name="customId"></param>
        /// <param name="customState"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        internal static ComputationInfo Create<TSettings>(ComputationManager manager, UserInfo user, ServiceInfo service,
            string name, TSettings settings, string source,  IEnumerable<EntityId> dependentObjects = null,
            ComputationPriority? customPriority = null, string customId = null, object customState = null)
            where TSettings : class
        {
            var id = string.IsNullOrEmpty(customId) ? Guid.NewGuid().ToString() : customId;
            var computation = CreateAndSave(manager.Id, id, newComp =>
            {
                newComp.Name = name;
                newComp.UserId = user.Id;
                newComp.ServiceId = service.Id;
                newComp.DependentObjectIds = dependentObjects != null ? dependentObjects.ToArray() : new EntityId[0];
                newComp.Priority = customPriority ?? service.DefaultComputationPriority;
                newComp.Source = source;
                if (customState != null)
                {
                    newComp.CustomStateJson = customState.ToJsonString(prettyPrint: false);
                }
            });

            var status = JsonComputationStatus.Open(computation.GetStatusPath());
            status.IsIndeterminate = true;
            status.Message = "The computation is ready to be executed.";
            status.Save();
            JsonHelper.WriteJsonFile(computation.GetSettingsPath(), settings);
            Directory.CreateDirectory(Path.Combine(computation.CurrentDirectory, ResultFolderName));
            computation.Save();

            return computation;
        }
    }
}
