namespace WebChemistry.Queries.Service
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using ICSharpCode.SharpZipLib.Zip;
    using Newtonsoft.Json;
    using WebChemistry.Framework.Core;

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ComputationState
    {
        New = 0,
        Scheduled = 1,
        Pending = 2,
        Running = 3,
        Success = 4,
        Failed = 5,
        Terminated = 6
    }

    public class ComputationStatus
    {
        public string ServiceVersion { get; set; }
        public ComputationState State { get; set; }
        public string Message { get; set; }
        public string CustomState { get; set; }
        public bool IsIndeterminate { get; set; }
        public int CurrentProgress { get; set; }
        public int MaxProgress { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public DateTime? StartedTime { get; set; }
        public DateTime? FinishedTime { get; set; }
        public long ResultSizeInBytes { get; set; }
    }

    public class JsonComputationStatus : ComputationStatus
    {
        string filename;

        public bool TrySave(bool prettyPrint = true)
        {
            try
            {
                Save(prettyPrint);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void EnsureSave(bool prettyPrint = true)
        {
            while (true)
            {
                try
                {
                    Save(prettyPrint);
                    break;
                }
                catch
                {
                    Thread.Sleep(50);
                }
            }
        }

        void Save(bool prettyPrint)
        {
            var json = JsonConvert.SerializeObject(this, prettyPrint ? Formatting.Indented : Formatting.None);
            using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            using (var sw = new StreamWriter(fs))
            {
                sw.Write(json);
                sw.Flush();
            }
        }

        public static JsonComputationStatus Open(string filename)
        {
            if (File.Exists(filename))
            {
                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    var ret = JsonConvert.DeserializeObject<JsonComputationStatus>(sr.ReadToEnd()) ?? new JsonComputationStatus();
                    ret.filename = filename;
                    return ret;
                }
            }

            var created = new JsonComputationStatus { filename = filename };
            created.EnsureSave();
            return created;
        }
    }

    public static class ComputationStatusHelper
    {
        public static void SetJsonCustomState<TState>(this ComputationStatus status, TState value)
            where TState : new()
        {
            status.CustomState = JsonConvert.SerializeObject(value);
        }
    }

    public static class JsonHelper
    {
        public static TResult ReadJsonFile<TResult>(string filename, bool createEmptyObjectIfFileDoesNotExist = true)
            where TResult : class
        {
            if (File.Exists(filename))
            {
                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    return JsonConvert.DeserializeObject<TResult>(sr.ReadToEnd());
                }
            }

            if (createEmptyObjectIfFileDoesNotExist) return JsonConvert.DeserializeObject<TResult>("{}");
            throw new FileLoadException(string.Format("File '{0}' does not exist.", filename));
        }
    }

    public static class DateTimeService
    {
        public static DateTime GetCurrentTime()
        {
            return DateTime.UtcNow;
        }
    }

    public static class ZipUtils
    {
        public sealed class ZipWrapper : IDisposable
        {
            readonly FileStream stream;
            readonly ZipOutputStream zip;
            readonly StreamWriter writer;
            bool disposed;

            static string NormalizePath(string path)
            {
                return path.Replace('\\', '/');
            }

            public void AddEntry(string name, string content)
            {
                AddEntry(name, w => w.Write(content));
            }

            public void AddEntry(string name, Action<TextWriter> content)
            {
                zip.PutNextEntry(new ZipEntry(NormalizePath(name)));
                content(writer);
                writer.Flush();
                zip.Flush();
                zip.CloseEntry();
            }

            public ZipWrapper(string filename)
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                stream = File.Create(filename);
                zip = new ZipOutputStream(stream);
                writer = new StreamWriter(zip);
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                writer.Dispose();
                zip.Dispose();
                stream.Dispose();
            }
        }

        public static ZipWrapper CreateZip(string filename)
        {
            return new ZipWrapper(filename);
        }
    }

    public class ServiceHelpConfig
    {
        public string AppName { get; set; }
        public string ExecutableName { get; set; }
        public AppVersion Version { get; set; }
        public object Example { get; set; }
        public HelpOutputStructure OutputStructure { get; set; }
    }

    public class HelpOutputStructure
    {
        public string Remark { get; set; }
        public HelpOutputStructureDescription StandardStructure { get; set; }
        public HelpOutputStructureDescription AppSpecificStructure { get; set; }

        public HelpOutputStructure()
        {
            Remark = "";
        }
    }

    public abstract class HelpOutputStructureDescription
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public HelpOutputStructureDescription[] Children { get; private set; }

        class FolderDescription : HelpOutputStructureDescription { }
        class FileDescription : HelpOutputStructureDescription { }

        protected HelpOutputStructureDescription()
        {
            Children = new HelpOutputStructureDescription[0];
        }

        void Format(StringBuilder text, int level)
        {
            text.AppendLine(string.Format("{0}* {1}", new string(' ', 2 * level), Name));
            text.AppendLine(ServiceHelpGenerator.MakeLines(Description, 2 * level + 2));
            foreach (var child in Children) child.Format(text, level + 1);
        }

        public string FormatToConsole()
        {
            var text = new StringBuilder();
            Format(text, 0);
            return text.ToString();
        }

        public static HelpOutputStructureDescription Folder(string name, string description, params HelpOutputStructureDescription[] children)
        {
            return new FolderDescription { Name = name, Description = description, Children = children };
        }

        public static HelpOutputStructureDescription File(string name, string description, params HelpOutputStructureDescription[] children)
        {
            return new FileDescription { Name = name, Description = description, Children = children };
        }
    }

    public static class ServiceHelpGenerator
    {
        public static string MakeLines(string str, int offset)
        {
            if (string.IsNullOrEmpty(str)) return new string(' ', offset);
            var maxLen = 79;
            var text = new StringBuilder();
            Action<string> append = null;
            append = s =>
            {
                if (s.Length <= maxLen - offset)
                {
                    text.Append(new string(' ', offset) + s);
                    return;
                }
                var index = s.LastIndexOf(' ', maxLen - offset);
                if (index < 0)
                {
                    text.Append(new string(' ', offset) + s);
                    return;
                }
                text.AppendLine(new string(' ', offset) + s.Substring(0, index));
                append(s.Substring(index + 1));
            };
            append(str);
            return text.ToString();
        }

        static string GetTypeName(PropertyInfo p)
        {
            var nameAttribute = p.GetCustomAttributes(false)
                .FirstOrDefault(a => a.GetType().Name == "HelpTypeNameAttribute");
            if (nameAttribute != null)
            {
                var prop = nameAttribute.GetType().GetProperty("Name");
                var value = prop != null ? prop.GetValue(nameAttribute) as string : null;
                if (!string.IsNullOrEmpty(value)) return value;
            }
            return p.PropertyType.Name;
        }

        static IEnumerable<string> MakeAttributes(Type type)
        {
            foreach (var p in type.GetProperties())
            {
                var description = p.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
                var defaultValue = p.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() as DefaultValueAttribute;
                var header = string.Format("* {0} [ {1} ]", p.Name, GetTypeName(p));
                if (defaultValue != null) header += string.Format(", Default value = {0}", defaultValue.Value.ToJsonString());
                yield return header;
                if (description != null) yield return MakeLines(description.Description, 4);
                yield return "";
            }
        }

        static string GenerateConfigHelp<TConfig>(ServiceHelpConfig config)
        {
            var lines = new List<string>
            {
                string.Format("This is help for version {0} or newer.", config.Version),
                "",
                "= Running the Service =",
                string.Format("{0} workingFolder configuration.json", config.ExecutableName),
                "",
                "= Configuration Example =",
                config.Example.ToJsonString(true),
                "",
                "= Attributes ="
            };

            lines.AddRange(MakeAttributes(typeof(TConfig)));
            lines.Add("= Output Description =");
            lines.Add("== General Structure ==");
            lines.Add(config.OutputStructure.StandardStructure.FormatToConsole());
            lines.Add(string.Format("== {0} Specific Structure ==", config.AppName));
            lines.Add(config.OutputStructure.AppSpecificStructure.FormatToConsole());
            return string.Join(Environment.NewLine, lines);
        }

        public static string GenerateHelp<TConfig>(ServiceHelpConfig config)
        {
            return GenerateConfigHelp<TConfig>(config);
        }

        public static string GenerateWikiHelp<TConfig>(ServiceHelpConfig config)
        {
            return GenerateConfigHelp<TConfig>(config);
        }
    }

    public abstract class ServiceBase<TService, TSettings, TStandaloneSettings, TCustomState>
        where TService : ServiceBase<TService, TSettings, TStandaloneSettings, TCustomState>, new()
        where TSettings : class
        where TStandaloneSettings : class, new()
        where TCustomState : class, new()
    {
        readonly System.Globalization.CultureInfo invariantCulture = System.Globalization.CultureInfo.InvariantCulture;

        Stopwatch Timer;
        TimeSpan LastUpdatedElapsed;
        StreamWriter logWriter;
        string standaloneWorkingFolder;

        public bool IsStandalone { get { return true; } }
        public JsonComputationStatus Status { get; private set; }
        public TCustomState CustomState { get; private set; }
        public TSettings Settings { get; private set; }
        public TStandaloneSettings StandaloneSettings { get; private set; }
        protected string ResultFolder { get; private set; }
        protected bool SuppressProgressUpdate { get; set; }

        protected virtual void RunHostedInternal()
        {
            throw new NotSupportedException("Hosted execution is not available in the standalone build.");
        }

        protected abstract void RunStandaloneInternal();
        public abstract AppVersion GetVersion();
        public abstract string GetName();

        public virtual void UpdateHelpConfig(ServiceHelpConfig config)
        {
        }

        protected virtual HelpOutputStructureDescription MakeOutputStructure()
        {
            return HelpFolder("result", "Contains the result of the computation.");
        }

        protected static HelpOutputStructureDescription HelpFile(string name, string description, params HelpOutputStructureDescription[] children)
        {
            return HelpOutputStructureDescription.File(name, description, children);
        }

        protected static HelpOutputStructureDescription HelpFolder(string name, string description, params HelpOutputStructureDescription[] children)
        {
            return HelpOutputStructureDescription.Folder(name, description, children);
        }

        protected abstract TStandaloneSettings SampleStandaloneSettings();

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

        protected void UpdateStatus()
        {
            var now = Timer.Elapsed;
            if ((now - LastUpdatedElapsed).TotalSeconds > 5)
            {
                Status.SetJsonCustomState(CustomState);
                if (Status.TrySave()) LastUpdatedElapsed = now;
                else Log("Error saving status.");
            }
        }

        protected bool RequiresUpdate()
        {
            return (Timer.Elapsed - LastUpdatedElapsed).TotalSeconds > 5;
        }

        protected void SaveStatus()
        {
            Status.SetJsonCustomState(CustomState);
            if (Status.TrySave()) LastUpdatedElapsed = Timer.Elapsed;
            else if (logWriter != null) Log("Error saving status.");
        }

        readonly object logSync = new object();

        public string Log(string format, params object[] args)
        {
            var text = string.Format(invariantCulture, "[{0} UTC] {1}", DateTimeService.GetCurrentTime(), string.Format(invariantCulture, format, args));
            lock (logSync)
            {
                Console.WriteLine(text);
                if (logWriter != null) logWriter.WriteLine(text);
            }
            return text;
        }

        ServiceHelpConfig MakeHelpConfig()
        {
            return new ServiceHelpConfig
            {
                AppName = GetName(),
                ExecutableName = AppDomain.CurrentDomain.FriendlyName,
                Example = SampleStandaloneSettings(),
                Version = GetVersion(),
                OutputStructure = new HelpOutputStructure
                {
                    StandardStructure = HelpFolder("[WorkingDirectory]", "Working directory of the application.",
                        HelpFolder("result", "Contains the result of the computation."),
                        HelpFile("log.txt", "Contains the console output of the application."),
                        HelpFile("status.json", "Contains general information of the computation.")),
                    AppSpecificStructure = MakeOutputStructure()
                }
            };
        }

        void ShowHelp()
        {
            var config = MakeHelpConfig();
            UpdateHelpConfig(config);
            Console.WriteLine(ServiceHelpGenerator.GenerateHelp<TStandaloneSettings>(config));
        }

        string MakeWikiHelp()
        {
            var config = MakeHelpConfig();
            UpdateHelpConfig(config);
            return ServiceHelpGenerator.GenerateWikiHelp<TStandaloneSettings>(config);
        }

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
                    foreach (var e in ex.InnerExceptions) Log("Error: {0}", e.Message);
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
                    Status.ResultSizeInBytes = Directory.Exists(ResultFolder)
                        ? Directory.EnumerateFiles(ResultFolder, "*.*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length)
                        : 0;
                    if (Status.State == ComputationState.Running) Status.State = ComputationState.Success;
                    Status.FinishedTime = DateTimeService.GetCurrentTime();
                    Status.Message = Status.State == ComputationState.Success ? "Done." : Status.Message;
                    Status.EnsureSave();
                }
            }
        }

        public static void Run(string[] commandLineArgs)
        {
            var svc = new TService();
            Console.WriteLine("{0} {1}, (c) David Sehnal", svc.GetName(), svc.GetVersion());
            Console.WriteLine();

            if (commandLineArgs.Length == 0 || commandLineArgs.Length > 3)
            {
                var appName = AppDomain.CurrentDomain.FriendlyName;
                Console.WriteLine("== Usage ==");
                Console.WriteLine("  {0} workingFolder standaloneConfig.json", appName);
                Console.WriteLine("  {0} --version", appName);
                Console.WriteLine("  {0} --help", appName);
                Console.WriteLine("  {0} --wiki-help output.txt", appName);
                Console.WriteLine();
                Console.WriteLine("Hosted Platform mode has been removed from this build.");
                return;
            }

            Thread.CurrentThread.CurrentCulture = svc.invariantCulture;
            Thread.CurrentThread.CurrentUICulture = svc.invariantCulture;

            var firstArg = commandLineArgs[0].ToLowerInvariant();
            if (firstArg == "--version")
            {
                Console.WriteLine(svc.GetVersion());
                return;
            }
            if (firstArg == "--help")
            {
                svc.ShowHelp();
                return;
            }
            if (firstArg == "--wiki-help")
            {
                File.WriteAllText(commandLineArgs[1], svc.MakeWikiHelp());
                Console.WriteLine("Help generated.");
                return;
            }
            if (firstArg == "--hosted")
            {
                Console.WriteLine("Hosted Platform mode has been removed from this build. Use standalone mode instead.");
                return;
            }

            svc.Timer = Stopwatch.StartNew();
            svc.LastUpdatedElapsed = TimeSpan.FromSeconds(0);
            svc.standaloneWorkingFolder = commandLineArgs[0];
            if (!Directory.Exists(svc.standaloneWorkingFolder)) Directory.CreateDirectory(svc.standaloneWorkingFolder);

            svc.Status = JsonComputationStatus.Open(Path.Combine(svc.standaloneWorkingFolder, "status.json"));
            svc.Status.ServiceVersion = svc.GetVersion().ToString();
            svc.Status.State = ComputationState.Running;
            svc.Status.Message = "Running...";
            svc.Status.StartedTime = DateTimeService.GetCurrentTime();
            svc.CustomState = new TCustomState();
            svc.StandaloneSettings = JsonHelper.ReadJsonFile<TStandaloneSettings>(commandLineArgs[1]);
            svc.SaveStatus();

            svc.ResultFolder = Path.Combine(svc.standaloneWorkingFolder, "result");
            if (!Directory.Exists(svc.ResultFolder)) Directory.CreateDirectory(svc.ResultFolder);
            svc.RunStandalone();
        }
    }
}
