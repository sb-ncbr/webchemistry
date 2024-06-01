namespace WebChemistry.Silverlight.Common
{
    using ICSharpCode.SharpZipLib.Zip;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using WebChemistry.Framework.Core;
    using WebChemistry.Silverlight.Common.DataModel;

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
                var e = new ZipEntry(name);
                ZipStream.PutNextEntry(e);
            }

            /// <summary>
            /// End entry.
            /// </summary>
            public void EndEntry()
            {
                TextWriter.Flush();
                ZipStream.CloseEntry();
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
        /// Create a zip archive.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="action"></param>
        public static async Task CreateZip(Stream s, Func<ZipContext, Task> action)
        {
            using (var zip = new ZipOutputStream(s))
            using (var writer = new StreamWriter(zip))
            {
                var context = new ZipContext { ZipStream = zip, TextWriter = writer };
                await action(context);
            }
        }

        /// <summary>
        /// Compress a string to a byte array.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] CompressString(string value)
        {
            using (var ms = new MemoryStream())
            {
                var zip = new ZipOutputStream(ms);
                var writer = new StreamWriter(zip);
                {
                    zip.PutNextEntry(new ZipEntry("value"));
                    writer.Write(value);
                    writer.Flush();
                    zip.CloseEntry();
                }
                writer.Close();
                zip.Close();
                return ms.GetBuffer();
            }
        }

        /// <summary>
        /// Decompress a string from a byte array.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DecompressString(byte[] value)
        {
            using (var ms = new MemoryStream(value, false))
            using (var zip = new ZipInputStream(ms))
            using (var reader = new StreamReader(zip))
            {                
                var entry = zip.GetNextEntry();
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Enumerate files in a zip archive:
        /// Tuple(Filename, Content of the file)
        /// </summary>
        /// <param name="ms"></param>
        /// <param name="filter"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<string, string>> EnumerateZipContent(MemoryStream ms, Func<string, bool> filter, ComputationProgress progress)
        {
            using (var zip = new ZipFile(ms))
            {
                foreach (ZipEntry entry in zip)
                {
                    if (entry.IsDirectory) continue;

                    progress.ThrowIfCancellationRequested();
                    if (filter(entry.Name))
                    {
                        using (var reader = new StreamReader(zip.GetInputStream(entry)))
                        {
                            yield return Tuple.Create(entry.Name, reader.ReadToEnd());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Write original structures to a ZipStream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="folder"></param>
        /// <param name="structures"></param>
        /// <param name="source"></param>
        public static void WriteStructuresToZipStream<T>(ZipOutputStream stream, string folder, IEnumerable<T> structures, Func<T, string> source)
            where T : StructureWrapBase<T>, new()
        {
            var writer = new StreamWriter(stream);
            foreach (var s in structures)
            {
                var entry = new ZipEntry(ZipEntry.CleanName(folder + "\\" + s.Filename));
                stream.PutNextEntry(entry);
                var src = source(s);
                writer.Write(src);
                writer.Flush();
                stream.CloseEntry();
            }
        }
    }
}
