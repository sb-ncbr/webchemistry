////using System;
////using System.Collections.Generic;
////using System.IO;
////using System.Linq;
////using System.Text;

////namespace WebChemistry.Platform.Cache
////{
////    /// <summary>
////    /// Persistent object cache for handling JSON and XML files.
////    /// </summary>
////    public static class PersistentObjectStringCache
////    {
////        const int RowSize = 256;
////        static readonly long MaxSizeInChars = 1024 * 1024 * 1024;
////        static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

////        static bool RequiresWatcherSync = false;
////        static long CurrentSize;
////        static object Sync = new object();
////        static bool Initialized = false;
////        static string[] Paths;
////        static Dictionary<string, string>[][] Cache;
////        static LinkedList<Dictionary<string, string>> Dictionaries;
////        static Dictionary<string, FileSystemWatcher> Watchers;

////        ////static object logLock = new object();
////        ////static void Log(string format, params object[] args)
////        ////{
////        ////    lock (logLock)
////        ////    {
////        ////        File.AppendAllText("i:/test/WebChemPlatform/watcher.txt", string.Format(format + "\n", args));
////        ////    }
////        ////}

////        static void Remove(string name)
////        {
////            lock (Sync)
////            {
////                var dict = GetDictionary(name);
////                if (dict != null)
////                {
////                    dict.Remove(name);
////                    ////if (dict.Remove(name))
////                    ////{
////                    ////    Log("[Removed] {0}", name);
////                    ////}
////                }
////            }
////        }

////        static Dictionary<string, string> GetDictionary(string name)
////        {
////            var hash = Comparer.GetHashCode(name);
////            var i = hash & 0xFFFF;
////            var row = Cache[i];
////            if (row == null) return null;
////            var k = (hash >> 16) & 0xFF;
////            return row[k];
////        }

////        static Dictionary<string, string> GetOrCreateDictionary(string name)
////        {
////            var hash = Comparer.GetHashCode(name);

////            var i = hash & 0xFFFF;
////            var k = (hash >> 16) & 0xFF;

////            var row = Cache[i];

////            Dictionary<string, string> ret;

////            if (row == null)
////            {
////                row = new Dictionary<string, string>[RowSize];
////                Cache[i] = row;
////                ret = new Dictionary<string, string>(Comparer);
////                Dictionaries.AddLast(ret);
////                row[k] = ret;
////                return ret;
////            }

////            ret = row[k];

////            if (ret == null)
////            {
////                ret = new Dictionary<string, string>(Comparer);
////                Dictionaries.AddLast(ret);
////                row[k] = ret;
////            }

////            return ret;
////        }

////        static string Read(string name)
////        {
////            using (var reader = new StreamReader(File.Open(name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
////            {
////                return reader.ReadToEnd();
////            }
////        }

////        static void CheckCapacity(Dictionary<string, string> last, string name, string value)
////        {
////            if (CurrentSize > MaxSizeInChars)
////            {
////                Dictionary<string, string> pivot = null;
////                for (var d = Dictionaries.First; d != null; d = d.Next)
////                {
////                    if (!object.ReferenceEquals(d, last) && d.Value.Count > 0)
////                    {
////                        pivot = d.Value;
////                        break;
////                    }
////                }
////                if (pivot == null)
////                {
////                    last.Clear();
////                    last.Add(name, value);
////                }
////                else
////                {
////                    pivot.Clear();
////                }
////            }
////        }
        
////        static void OnChanged(object source, FileSystemEventArgs e)
////        {
////           // Log("{0}: {1}", e.ChangeType, e.FullPath);
////            Remove(e.FullPath);
////        }

////        static void OnRenamed(object source, RenamedEventArgs e)
////        {
////          //  Log("{0}: {1} -> {2}\n", e.ChangeType, e.OldFullPath, e.FullPath);
////            Remove(e.OldFullPath);
////        }

////        static void OnDisposed(object sender, EventArgs e)
////        {
////            lock (Sync)
////            {
////                var watcher = (FileSystemWatcher)sender;
////                watcher.Changed -= OnChanged;
////                watcher.Created -= OnChanged;
////                watcher.Deleted -= OnChanged;
////                watcher.Renamed -= OnRenamed;
////                watcher.Error -= OnError;

////              //  Log("Disposed: {0}", watcher.Path);

////                Watchers.Remove(watcher.Path);
////                RequiresWatcherSync = true;
////            }
////        }

////        static void OnError(object sender, ErrorEventArgs e)
////        {
////            var watcher = (FileSystemWatcher)sender;
////          //  Log("Error: {0}", watcher.Path);
////            watcher.Dispose();
////        }

////        static void ClearCache()
////        {
////            Dictionaries = new LinkedList<Dictionary<string, string>>();
////            Cache = new Dictionary<string, string>[RowSize * RowSize][];
////            CurrentSize = 0;
////        }

////        static void SyncWatchers()
////        {
////            if (Paths == null) return;

////            lock (Sync)
////            {
////                try
////                {
////                    bool changed = false;
////                    foreach (var path in Paths)
////                    {
////                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

////                        if (!Watchers.ContainsKey(path))
////                        {
////                            changed = true;
////                            var fsw = new FileSystemWatcher(path)
////                            {
////                                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
////                                IncludeSubdirectories = true,
////                                Filter = "*.json"
////                            };
////                            fsw.Changed += OnChanged;
////                            fsw.Created += OnChanged;
////                            fsw.Deleted += OnChanged;
////                            fsw.Renamed += OnRenamed;
////                            fsw.Disposed += OnDisposed;
////                            fsw.Error += OnError;
////                            fsw.EnableRaisingEvents = true;
////                            Watchers.Add(path, fsw);
////                        }
////                    }
////                    if (changed) ClearCache();
////                    RequiresWatcherSync = false;
////                }
////                catch
////                {
////                    RequiresWatcherSync = true;
////                }
////            }
////        }
        
////        /// <summary>
////        /// Get file's content.
////        /// </summary>
////        /// <param name="filename"></param>
////        /// <returns></returns>
////        public static string GetContent(string filename)
////        {
////            if (!Initialized || !filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) return Read(filename);
////            if (RequiresWatcherSync) SyncWatchers();
////            filename = Path.GetFullPath(filename);
////            lock (Sync)
////            {
////                var dict = GetOrCreateDictionary(filename);
////                string value;
////                if (dict.TryGetValue(filename, out value)) return value;
////                value = Read(filename);
////                CurrentSize += value.Length;
////                CheckCapacity(dict, filename, value);
////                dict[filename] = value;
////                return value;
////            }
////        }


////        /// <summary>
////        /// Initialize the cache.
////        /// </summary>
////        /// <param name="serverPaths"></param>
////        public static void Init(params string[] serverPaths)
////        {
////            Dictionaries = new LinkedList<Dictionary<string, string>>();
////            Watchers = new Dictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);
////            Paths = serverPaths.ToArray();
////            SyncWatchers();
////            Initialized = true;
////        }
////    }
////}
