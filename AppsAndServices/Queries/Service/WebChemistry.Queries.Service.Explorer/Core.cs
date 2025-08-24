namespace WebChemistry.Queries.Service.Explorer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using WebChemistry.Framework.Core;
    using WebChemistry.Platform;

    public partial class QueriesExplorerInstance : PersistentObjectBase<QueriesExplorerInstance>, IDisposable
    {
        /// <summary>
        /// App version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Name of the session.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Who created this?
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Max number of structures allowed in the session.
        /// </summary>
        public int StructureLimit { get; set; }

        /// <summary>
        /// Max number of atoms allowed in the session.
        /// </summary>
        public int StructureAtomLimit { get; set; }

        /// <summary>
        /// Max number of patterns in a result.
        /// </summary>
        public int PatternLimit { get; set; }

        /// <summary>
        /// Max number of atoms in a result.
        /// </summary>
        public int PatternAtomLimit { get; set; }

        /// <summary>
        /// Max degreen of parallelism
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; }

        /// <summary>
        /// Queries entered.
        /// </summary>
        public List<string> QueryHistory { get; set; }

        FileStream LogFile;
        StreamWriter LogStream;

        /// <summary>
        /// Create instance of the app.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="maxDegreeOfParallelism"></param>
        /// <param name="structureLimit"></param>
        /// <param name="structureAtomLimit"></param>
        /// <param name="patternLimit"></param>
        /// <param name="patternAtomLimit"></param>
        /// <returns></returns>
        public static QueriesExplorerInstance Create(EntityId id, string name, string source, int maxDegreeOfParallelism = 4, int structureLimit = 100, int structureAtomLimit = 500000, int patternLimit = 5000, int patternAtomLimit = 100000)
        {
            return CreateAndSave(id, app =>
            {
                app.Version = new AppVersion(1, 0, 21, 12, 14, 'a').ToString();
                app.Name = name;
                app.Source = source;
                app.MaxDegreeOfParallelism = maxDegreeOfParallelism;
                app.StructureLimit = structureLimit;
                app.StructureAtomLimit = structureAtomLimit;
                app.PatternLimit = patternLimit;
                app.PatternAtomLimit = patternAtomLimit;
                app.QueryHistory = new List<string>();
            });
        }

        /// <summary>
        /// Try to read the app.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static QueriesExplorerInstance TryRead(EntityId id)
        {
            var ret = QueriesExplorerInstance.TryLoad(id);
            if (ret == null) return null;
            ret.Init();
            return ret;
        }
        
        public AppState GetState()
        {
            return Guarded(() =>
            {
                QueryResult latest = null;
                if (File.Exists(Path.Combine(CurrentDirectory, "latest.json"))) latest = JsonHelper.ReadJsonFile<QueryResult>(Path.Combine(CurrentDirectory, "latest.json"));
                return new AppState
                {
                    Structures = GetStructuresInfo(),
                    QueryHistory = QueryHistory.ToArray(),
                    LatestResult = latest
                };
            });
        }

        void Init()
        {
            try
            {
                LogFile = File.Open(Path.Combine(CurrentDirectory, "log.txt"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Delete | FileShare.ReadWrite);
                LogFile.Seek(0, SeekOrigin.End);
                LogStream = new StreamWriter(LogFile);
            }
            catch
            {

            }
            Guarded(() => 
            {
                Log("Starting initialization...");
                ReadStructures();
                Log("Finished initialization.");
                return 0; 
            });
        }

        object Sync = new object(), LogSync = new object();
        bool InUse;

        T Guarded<T>(Func<T> func)
        {
            lock (Sync)
            {
                if (InUse) throw new InvalidOperationException("The session is currently in use. Maybe it's open in another tab or some other user has access to it.");
                InUse = true;
            }

            try
            {
                return func();
            }
            finally
            {
                if (LogStream != null)
                {
                    LogStream.Flush();
                }

                lock (Sync)
                {
                    InUse = false;
                }
            }
        }

        string Log(string format, params object[] args)
        {
            var msg = string.Format(format, args);
            if (LogStream == null) return msg;

            lock (LogSync)
            {
                LogStream.WriteLine("[{0}] {1}", DateTimeService.GetCurrentTime(), msg);
            }
            return msg;
        }

        public void Dispose()
        {
            Log("Disposing...");
            if (Structures != null)
            {
                Structures = null;
            }

            if (LogStream != null)
            {
                LogStream.Dispose();
                LogStream = null;
            }

            if (LogFile != null)
            {
                LogFile.Dispose();
                LogFile = null;
            }
        }
    }
}
