namespace WebChemistry.Platform.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using WebChemistry.Framework.Core;
    using WebChemistry.Platform.Computation;
    using WebChemistry.Platform.Server;
    
    /// <summary>
    /// A base class for implementing services.
    /// 
    /// TODO: Add app mode.
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <typeparam name="TSettings"></typeparam>
    /// <typeparam name="TStandaloneSettings"></typeparam>
    /// <typeparam name="TCustomState"></typeparam>
    public abstract class ServiceBase<TService, TSettings, TStandaloneSettings, TCustomState>
        where TService : ServiceBase<TService, TSettings, TStandaloneSettings, TCustomState>, new()
        where TSettings : class
        where TStandaloneSettings : class, new()
        where TCustomState : class, new()
    {
        System.Globalization.CultureInfo invariantCulture = System.Globalization.CultureInfo.InvariantCulture;

        Stopwatch Timer;
        TimeSpan LastUpdatedElapsed;
        //DateTime statusLastSaved;

        bool isStandalone;
        StreamWriter logWriter;
        string standaloneWorkingFolder;

        /// <summary>
        /// Check if the app is in the standalone mode.
        /// </summary>
        public bool IsStandalone { get { return isStandalone; } }

        /// <summary>
        /// The computation.
        /// </summary>
        protected ComputationInfo Computation { get; set; }

        /// <summary>
        /// Computation status.
        /// </summary>
        public JsonComputationStatus Status { get; private set; }

        /// <summary>
        /// Custom state. Such as number of found motives in MotiveQUery.
        /// </summary>
        public TCustomState CustomState { get; private set; }

        /// <summary>
        /// Computation settings.
        /// </summary>
        public TSettings Settings { get; private set; }

        /// <summary>
        /// Computation settings.
        /// </summary>
        public TStandaloneSettings StandaloneSettings { get; private set; }

        /// <summary>
        /// Path where to save the result.
        /// </summary>
        protected string ResultFolder { get; private set; }

        /// <summary>
        /// The actual computation.
        /// </summary>
        protected abstract void RunHostedInternal();

        /// <summary>
        /// Standalone version of the service.
        /// </summary>
        protected abstract void RunStandaloneInternal();

        /// <summary>
        /// Version of the service.
        /// </summary>
        /// <returns></returns>
        public abstract AppVersion GetVersion();

        /// <summary>
        /// Name of the service.
        /// </summary>
        /// <returns></returns>
        public abstract string GetName();

        /// <summary>
        /// Update the configuration with app specific remarks.
        /// </summary>
        /// <param name="config"></param>
        public virtual void UpdateHelpConfig(ServiceHelpConfig config)
        {

        }

        private HelpOutputStructureDescription MakeStandardOutputStructure()
        {
            return HelpFolder("[WorkingDirectory]", "Working directory of the application.", 
                HelpFolder("result", "Contains the result of the computation."),
                HelpFile("log.txt", "Contains the console output of the application."),
                HelpFile("status.json", "Contains general information of the computation (running time, version, etc.)."));
        }

        /// <summary>
        /// Creates the output structure of the computation.
        /// </summary>
        /// <returns></returns>
        protected virtual HelpOutputStructureDescription MakeOutputStructure()
        {
            return HelpFolder("result", "Contains the result of the computation.");
        }

        /// <summary>
        /// Helper function to make a file.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="desctiption"></param>
        /// <param name="children"></param>
        /// <returns></returns>
        protected static HelpOutputStructureDescription HelpFile(string name, string desctiption, params HelpOutputStructureDescription[] children)
        {
            return HelpOutputStructureDescription.File(name, desctiption, children);
        }

        /// <summary>
        /// Helper to make output folder.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="desctiption"></param>
        /// <param name="children"></param>
        /// <returns></returns>
        protected static HelpOutputStructureDescription HelpFolder(string name, string desctiption, params HelpOutputStructureDescription[] children)
        {
            return HelpOutputStructureDescription.Folder(name, desctiption, children);
        }

        /// <summary>
        /// Sample settings.
        /// </summary>
        /// <returns></returns>
        protected abstract TStandaloneSettings SampleStandaloneSettings();

        /// <summary>
        /// Determines whether the UpdateProgress function does anything or not.
        /// </summary>
        protected bool SuppressProgressUpdate { get; set; }

        /// <summary>
        /// Update the current progress.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="current"></param>
        /// <param name="max"></param>
        /// <param name="forceSave"></param>
        public void UpdateProgress(string message, int current = -1, int max = -1, bool forceSave = false)
        {
            if (SuppressProgressUpdate) return;

            Status.Message = message;
            if (current < 0 || max < 0)
            {
                Status.IsIndeterminate = true;
                Status.MaxProgress = Status.CurrentProgress = 0;
            }
            else
            {
                Status.IsIndeterminate = false;
                Status.CurrentProgress = current;
                Status.MaxProgress = max;
            }

            if (forceSave) SaveStatus();
            else UpdateStatus();
        }
        
        /// <summary>
        /// The app is responsible for calling this often enough.
        /// </summary>
        protected void UpdateStatus()
        {
            var now = Timer.Elapsed;
            var delta = now - LastUpdatedElapsed;
            if (delta.TotalSeconds > 5)
            {
                Status.SetJsonCustomState(CustomState);
                if (Status.TrySave()) LastUpdatedElapsed = now;
                else Log("Error saving status.");
            }
        }

        /// <summary>
        /// Check if the update is required.
        /// </summary>
        /// <returns></returns>
        protected bool RequiresUpdate()
        {
            return (Timer.Elapsed - LastUpdatedElapsed).TotalSeconds > 5;
        }

        /// <summary>
        /// Saves the status.
        /// </summary>
        protected void SaveStatus()
        {
            Status.SetJsonCustomState(CustomState);
            if (Status.TrySave()) LastUpdatedElapsed = Timer.Elapsed;
            else Log("Error saving status.");
        }

        object logSync = new object();

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public string Log(string format, params object[] args)
        {
            string text = string.Format(invariantCulture, "[{0} UTC] {1}", DateTimeService.GetCurrentTime(), string.Format(invariantCulture, format, args));
            lock (logSync)
            {
                if (isStandalone) Console.WriteLine(text);
                logWriter.WriteLine(text);
            }
            return text;
        }

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void LogFile(string format, params object[] args)
        {
            lock (logSync)
            {
                string text = string.Format(invariantCulture, "[{0} UTC] {1}", DateTimeService.GetCurrentTime(), string.Format(invariantCulture, format, args));
                logWriter.WriteLine(text);
            }
        }

        ServiceHelpConfig MakeHelpConfig()
        {
            return new ServiceHelpConfig
            {
                AppName = GetName(),
                ExecutableName = System.AppDomain.CurrentDomain.FriendlyName,
                Example = SampleStandaloneSettings(),
                Version = GetVersion(),
                OutputStructure = new HelpOutputStructure
                {
                    AppSpecificStructure = MakeOutputStructure(),
                    StandardStructure = MakeStandardOutputStructure()
                }
            };
        }

        void ShowHelp()
        {
            var cfg = MakeHelpConfig();
            UpdateHelpConfig(cfg);
            Console.WriteLine(ServiceHelpGenerator.GenerateHelp<TStandaloneSettings>(cfg));
        }

        string MakeWikiHelp()
        {
            var cfg = MakeHelpConfig();
            UpdateHelpConfig(cfg);
            return ServiceHelpGenerator.GenerateWikiHelp<TStandaloneSettings>(cfg);
        }

        /// <summary>
        /// Run the service.
        /// </summary>
        void RunHosted()
        {
            using (logWriter = new StreamWriter(File.OpenWrite(Computation.Id.GetChildId(ComputationInfo.LogFilename).GetEntityPath())))
            {
                try
                {
                    RunHostedInternal();
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Console.WriteLine("Exception: {0}", e);
                        Log("Error: {0}", e.Message);                        
                    }
                    Status.State = ComputationState.Failed;
                    Status.Message = "Multiple errors. See the log.";
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: {0}", e);
                    Log("Error: {0}", e.Message);
                    Status.State = ComputationState.Failed;
                    Status.Message = e.Message;
                }
                finally
                {
                    Status.ResultSizeInBytes = Directory
                        .EnumerateFiles(ResultFolder, "*.*", SearchOption.AllDirectories)
                        .Sum(f => new FileInfo(f).Length);

                    if (Status.State == ComputationState.Running) Status.State = ComputationState.Success;
                    Status.FinishedTime = DateTimeService.GetCurrentTime();
                    Status.EnsureSave();

                    ServerManager.MasterServer.ComputationScheduler.ComputationFinished(Computation);
                }
            }
        }

        /// <summary>
        /// Run the service.
        /// </summary>
        void RunStandalone()
        {
            var logFileName = Path.Combine(standaloneWorkingFolder, "log.txt");
            if (File.Exists(logFileName)) File.Delete(logFileName);

            using (logWriter = new StreamWriter(File.OpenWrite(logFileName)))
            {
                try
                {
                    RunStandaloneInternal();
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Log("Error: {0}", e.Message);
                    }
                    Status.State = ComputationState.Failed;
                    Status.Message = "Multiple errors. See the log.";
                }
                catch (Exception e)
                {
                    Log("Error: {0}", e.Message);
                    Status.State = ComputationState.Failed;
                    Status.Message = e.Message;
                }
                finally
                {
                    Status.ResultSizeInBytes = Directory
                            .EnumerateFiles(ResultFolder, "*.*", SearchOption.AllDirectories)
                            .Sum(f => new FileInfo(f).Length);

                    if (Status.State == ComputationState.Running) Status.State = ComputationState.Success;
                    Status.FinishedTime = DateTimeService.GetCurrentTime();
                    Status.Message = "Done.";
                    Status.EnsureSave();
                }
            }
        }
        
        /// <summary>
        /// Init the service.
        /// </summary>
        /// <param name="commandLineArgs"></param>
        /// <returns></returns>
        public static void Run(string[] commandLineArgs)
        {
            var svc = new TService();
            Console.WriteLine("{0} {1}, (c) David Sehnal", svc.GetName(), svc.GetVersion());
            Console.WriteLine();

            if (commandLineArgs.Length == 0 || commandLineArgs.Length > 3)
            {
                var appName = System.AppDomain.CurrentDomain.FriendlyName;
                Console.WriteLine("== Usage ==");
                Console.WriteLine();
                Console.WriteLine("  --hosted serverConfig computationId", appName);
                Console.WriteLine("    Run the application in environment \"hosted\" by the WebChem Platform.");
                Console.WriteLine();
                Console.WriteLine("  workingFolder standaloneConfig.json", appName);
                Console.WriteLine("    Run the application.");
                Console.WriteLine();
                Console.WriteLine("  --version", appName);
                Console.WriteLine("    Print the application version.");
                Console.WriteLine();
                Console.WriteLine("  --help", appName);
                Console.WriteLine("    Display the help.");
                Console.WriteLine();
                Console.WriteLine("  --wiki-help output.txt", appName);
                Console.WriteLine("    Generate the help with MediaWiki markup and write it to a file.");
                Console.WriteLine();
                return;
            }
                        
            Thread.CurrentThread.CurrentCulture = svc.invariantCulture;
            Thread.CurrentThread.CurrentUICulture = svc.invariantCulture;

            if (commandLineArgs[0].ToLowerInvariant() == "--version")
            {
                Console.WriteLine(svc.GetVersion());
                return;
            }

            if (commandLineArgs[0].ToLowerInvariant() == "--help")
            {
                svc.ShowHelp();
                return;
            }

            if (commandLineArgs[0].ToLowerInvariant() == "--wiki-help")
            {
                File.WriteAllText(commandLineArgs[1], svc.MakeWikiHelp());
                Console.WriteLine("Help generated.");
                return;
            }

            svc.Timer = Stopwatch.StartNew();
            svc.LastUpdatedElapsed = TimeSpan.FromSeconds(0);

            if (commandLineArgs[0].ToLowerInvariant() == "--hosted")
            {
                ServerManager.Init(commandLineArgs[1]);
                svc.Computation = ComputationInfo.Load(EntityId.Parse(commandLineArgs[2].Trim()));

                var status = svc.Computation.GetJsonStatus();
                status.ServiceVersion = svc.GetVersion();
                status.State = ComputationState.Running;
                status.Message = "Running...";
                status.StartedTime = DateTimeService.GetCurrentTime();
                svc.Status = status;
                svc.CustomState = new TCustomState();
                svc.Settings = svc.Computation.GetSettings<TSettings>();
                svc.SaveStatus();

                svc.ResultFolder = svc.Computation.GetResultFolderId().GetEntityPath();

                svc.RunHosted();
            }
            else
            {
                svc.isStandalone = true;
                string workingFolder = commandLineArgs[0];
                svc.standaloneWorkingFolder = workingFolder;
                if (!Directory.Exists(workingFolder)) Directory.CreateDirectory(workingFolder);

                var status = JsonComputationStatus.Open(Path.Combine(workingFolder, "status.json"));
                status.ServiceVersion = svc.GetVersion();
                status.State = ComputationState.Running;
                status.Message = "Running...";
                status.StartedTime = DateTimeService.GetCurrentTime();
                svc.Status = status;
                svc.CustomState = new TCustomState();
                svc.StandaloneSettings = JsonHelper.ReadJsonFile<TStandaloneSettings>(commandLineArgs[1]);
                svc.SaveStatus();

                svc.ResultFolder = Path.Combine(workingFolder, "result");

                if (!Directory.Exists(svc.ResultFolder)) Directory.CreateDirectory(svc.ResultFolder);

                svc.RunStandalone();
            }
        }
    }
}
