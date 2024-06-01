namespace WebChemistry.Platform
{
    using ICSharpCode.SharpZipLib.Zip;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Zip utilies.
    /// </summary>
    public static class ZipUtils
    {
        /// <summary>
        /// Zip file context.
        /// </summary>
        public class ZipContext
        {
            ZipEntry currentEntry = null;

            /// <summary>
            /// The stream.
            /// </summary>
            public ZipOutputStream ZipStream { get; internal set; }

            /// <summary>
            /// Do not forget to flush!
            /// </summary>
            public StreamWriter TextWriter { get; internal set; }

            /// <summary>
            /// Start new zip entry.
            /// </summary>
            /// <param name="name"></param>
            public void BeginEntry(string name)
            {
                name = ZipArchiveInterface.NormalizePath(name);
                currentEntry = new ZipEntry(name);
                ZipStream.PutNextEntry(currentEntry);
            }

            /// <summary>
            /// End entry.
            /// </summary>
            public void EndEntry()
            {
                if (currentEntry != null)
                {
                    TextWriter.Flush();
                    ZipStream.Flush();
                    ZipStream.CloseEntry();
                    currentEntry = null;
                }
            }

            /// <summary>
            /// Add's a new entry with a specified content.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="content"></param>
            public void AddEntry(string name, string content)
            {
                BeginEntry(name);
                TextWriter.Write(content);
                EndEntry();
            }

            /// <summary>
            /// Add entry using a text writer action.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="writer"></param>
            public void AddEntry(string name, Action<TextWriter> writer)
            {
                BeginEntry(name);
                writer(TextWriter);
                EndEntry();
            }

            /// <summary>
            /// Adds a new entry by copying it from a stream.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="streamProvider"></param>
            public void AddEntry(string name, Func<Stream> streamProvider)
            {
                using (var stream = streamProvider())
                {
                    BeginEntry(name);
                    stream.CopyTo(ZipStream);
                    EndEntry();
                }
            }

            /// <summary>
            /// Write a string.
            /// </summary>
            /// <param name="value"></param>
            public void WriteString(string value)
            {
                TextWriter.Write(value);
                TextWriter.Flush();
            }
        }

        /// <summary>
        /// Wraps a zip file with functions for adding entries.
        /// </summary>
        public sealed class ZipWrapper : IDisposable
        {
            bool disposed;

            FileStream stream;
            ZipOutputStream zip;
            StreamWriter writer;

            /// <summary>
            /// The context of this file.
            /// </summary>
            public ZipContext Context { get; private set; }

            /// <summary>
            /// Add's a new entry with a specified content.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="content"></param>
            public void AddEntry(string name, string content)
            {
                Context.AddEntry(name, content);
            }

            /// <summary>
            /// Add entry using a text writer.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="content"></param>
            public void AddEntry(string name, Action<TextWriter> content)
            {
                Context.AddEntry(name, content);
            }

            /// <summary>
            /// Adds a new entry by copying it from a stream.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="streamProvider"></param>
            public void AddEntry(string name, Func<Stream> streamProvider)
            {
                Context.AddEntry(name, streamProvider);
            }

            /// <summary>
            /// 
            /// </summary>
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;

                Context.EndEntry();

                if (writer != null) writer.Dispose();
                if (zip != null) zip.Dispose();
                if (stream != null) stream.Dispose();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="filename"></param>
            public ZipWrapper(string filename)
            {
                stream = File.Create(filename);
                zip = new ZipOutputStream(stream);
                writer = new StreamWriter(zip);
                Context = new ZipContext { ZipStream = zip, TextWriter = writer };
            }
        }

        /// <summary>
        /// Create a zip archive.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="action"></param>
        public static void CreateZip(string filename, Action<ZipContext> action)
        {
            using (var stream = File.Create(filename))
            using (var zip = new ZipOutputStream(stream))
            using (var writer = new StreamWriter(zip))
            {
                var context = new ZipContext { ZipStream = zip, TextWriter = writer };
                action(context);
                context.EndEntry();
            }
        }

        /// <summary>
        /// Creates a wrapper for a zip file (Disposable).
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static ZipWrapper CreateZip(string filename)
        {
            return new ZipWrapper(filename);
        }

        /// <summary>
        /// Return the zip entry as a string. Can return null.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetEntryAsString(string filename, string name)
        {
            using (var zip = new ZipFile(filename))
            {
                var e = zip.GetEntry(name);
                if (e == null) return null;
                using (var stream = new StreamReader(zip.GetInputStream(e)))
                {
                    return stream.ReadToEnd();
                };
            }
        }
    }

    /// <summary>
    /// Interface for accessing entries in zip files. 
    /// Until the object is disposed, the filename is kept open in Read/Delete share mode.
    /// </summary>
    public class ZipArchiveInterface : IDisposable
    {
        Stream DataStream;
        ZipFile DataZip;

        // Contains map of entries in a zip file for fast lookup.
        Dictionary<string, long> EntryMap;
        object Sync;

        internal static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        void Init(Func<Stream> streamProvider)
        {
            Sync = new object();

            try
            {
                DataStream = streamProvider();
                DataZip = new ZipFile(DataStream);

                EntryMap = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                foreach (ZipEntry e in DataZip)
                {
                    if (e.IsDirectory) continue;
                    EntryMap[NormalizePath(e.Name)] = e.ZipFileIndex;
                }
            }
            catch
            {
                if (DataStream != null)
                {
                    DataStream.Dispose();
                    DataStream = null;
                }

                if (DataZip != null)
                {
                    DataZip.Close();
                    DataZip = null;
                }

                throw;
            }
        }

        /// <summary>
        /// Get names of all entries.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetEntryNames()
        {
            return EntryMap.Keys;
        }

        /// <summary>
        /// Check of presence of an entry.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool HasEntry(string path)
        {
            return EntryMap.ContainsKey(NormalizePath(path));
        }

        /// <summary>
        /// Get string content of an entry.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string GetEntryString(string path)
        {
            long e;
            path = NormalizePath(path);
            if (!EntryMap.TryGetValue(path, out e)) return null;
            string ret;
            lock (Sync)
            {
                using (var reader = new StreamReader(DataZip.GetInputStream(e)))
                {
                    ret = reader.ReadToEnd();
                }
            }
            return ret;
        }

        /// <summary>
        /// Initialize the interface. Until the object is disposed, the filename is kept open in Read/Delete share mode.
        /// </summary>
        /// <param name="filename"></param>
        private ZipArchiveInterface()
        {

        }

        /// <summary>
        /// create the interface from a file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="inMemory"></param>
        /// <returns></returns>
        public static ZipArchiveInterface FromFile(string filename, bool inMemory = false)
        {
            var ret = new ZipArchiveInterface();

            if (inMemory)
            {
                ret.Init(() => 
                {
                    var stream = new MemoryStream(new byte[new FileInfo(filename).Length]);
                    using (var file = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.Read))
                    {
                        file.CopyTo(stream);
                        stream.Flush();
                    }
                    stream.Position = 0;
                    return stream;
                });
            }
            else
            {
                ret.Init(() => File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.Read));
            }
            return ret;
        }

        /// <summary>
        /// Create the interface from a stream;
        /// </summary>
        /// <param name="streamProvider"></param>
        /// <returns></returns>
        public static ZipArchiveInterface FromStream(Func<Stream> streamProvider)
        {
            var ret = new ZipArchiveInterface();
            ret.Init(streamProvider);
            return ret;
        }

        /// <summary>
        /// Release the file held.
        /// </summary>
        public void Dispose()
        {
            if (EntryMap != null)
            {
                EntryMap.Clear();
                EntryMap = null;
            }
            if (DataZip != null)
            {
                DataZip.Close();
                DataZip = null;
                DataStream.Dispose();
                DataStream = null;
            }
        }
    }
}