namespace WebChemistry.Platform.Services
{
    using ICSharpCode.SharpZipLib.Zip;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using WebChemistry.Framework.Core.Collections;

    /// <summary>
    /// Wrapper class for an input entry.
    /// </summary>
    public class ServiceInputEntryProvider
    {
        /// <summary>
        /// Filename -- without directory.
        /// </summary>
        public string Filename { get; private set; }

        ///// <summary>
        ///// Filename without extension.
        ///// </summary>
        //private string FilenameWithoutExtension { get; set; }

        /// <summary>
        /// Id of the structure based on the filename.
        /// </summary>
        public string StructureFilenameId { get; set; }

        /// <summary>
        /// Extension include the .
        /// </summary>
        public string Extension { get; private set; }

        /// <summary>
        /// Generic Stream provider.
        /// </summary>
        public Func<Stream> StreamProvider { get; private set; }

        /// <summary>
        /// Gets the text reader.
        /// </summary>
        public Func<TextReader> TextProvider { get; private set; }

        /// <summary>
        /// Get the string content of the file.
        /// </summary>
        /// <returns></returns>
        public string GetContent()
        {
            using (var stream = StreamProvider())
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Create instance from a file.
        /// </summary>
        /// <param name="filename"></param>
        public ServiceInputEntryProvider(string filename)
            : this(filename, () => File.OpenRead(filename))
        {
        }

        /// <summary>
        /// INstance from a filename and stream.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="streamProvider"></param>
        public ServiceInputEntryProvider(string filename, Func<Stream> streamProvider)
        {
            var fi = new FileInfo(filename);
            Filename = fi.Name;
            Extension = fi.Extension;
            StructureFilenameId = WebChemistry.Framework.Core.StructureReader.GetStructureIdFromFilename(fi.Name);
            StreamProvider = streamProvider;
            {
                var _stream = streamProvider();
                TextProvider = () => new StreamReader(_stream);
            }
        }
    }
    
    /// <summary>
    /// Provides input wrappers for files and entries in zip files.
    /// </summary>
    public class ServiceInputProvider : IDisposable
    {
        PrefixTreeMap<ServiceInputEntryProvider> FilenamePrefixMap;

        List<ServiceInputEntryProvider> Entries;
        Stack<IDisposable> DisposeStack;

        /// <summary>
        /// Gets all entries that have a filename with a given prefix.
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public IList<ServiceInputEntryProvider> GetPrefixEntries(string prefix)
        {
            return FilenamePrefixMap.Match(prefix).Values.ToArray();
        }

        /// <summary>
        /// Gets all entries that are structures.
        /// </summary>
        /// <returns></returns>
        public IList<ServiceInputEntryProvider> GetStructureEntries()
        {
            return Entries.Where(e => WebChemistry.Framework.Core.StructureReader.IsStructureFilename(e.Filename)).ToArray();
        }

        /// <summary>
        /// Creates an input provider from a single zip file.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static ServiceInputProvider FromZip(string filename)
        {
            var ret = new ServiceInputProvider();
            ret.ProcessFile(filename);
            return ret;
        }

        /// <summary>
        /// Creates input provider from a folder.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ServiceInputProvider FromFolder(string path)
        {
            var ret = new ServiceInputProvider();
            foreach (var f in Directory.EnumerateFiles(path))
            {
                ret.ProcessFile(f);
            }
            return ret;
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {
            while (DisposeStack.Count > 0)
            {
                var top = DisposeStack.Pop();
                top.Dispose();
            }
        }

        void ProcessFile(string filename)
        {
            if (!filename.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var entry = new ServiceInputEntryProvider(filename);
                Entries.Add(entry);
                FilenamePrefixMap.Add(entry.Filename, entry);
                return;
            }

            // open the zip and process the entries
            Stream stream;
            ZipFile zip;
            try
            {
                stream = File.OpenRead(filename);
                DisposeStack.Push(stream);                
            }
            catch (Exception e)
            {
                throw new IOException(string.Format("Error opening '{0}': {1}", Path.GetFileName(filename), e.Message));
            }

            try
            {
                zip = new ZipFile(stream);
            }
            catch (Exception e)
            {
                throw new IOException(string.Format("Error opening zip file '{0}': {1}", Path.GetFileName(filename), e.Message));
            }

            foreach (ZipEntry e in zip)
            {
                if (e.IsDirectory) continue;

                ServiceInputEntryProvider entry;
                {
                    var _zip = zip;
                    entry = new ServiceInputEntryProvider(e.Name, () => _zip.GetInputStream(e));
                }
                Entries.Add(entry);
                FilenamePrefixMap.Add(entry.Filename, entry);
            }
        }

        private ServiceInputProvider()
        {
            FilenamePrefixMap = new PrefixTreeMap<ServiceInputEntryProvider>();
            Entries = new List<ServiceInputEntryProvider>();
            DisposeStack = new Stack<IDisposable>();
        }
    }
}
