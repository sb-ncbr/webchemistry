namespace WebChemistry.Platform.MoleculeDatabase
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using WebChemistry.Framework.Core;

    /// <summary>
    /// Entry.
    /// </summary>
    public class DatabaseEntry
    {
        XElement xml;
        public DatabaseInfo Database { get; private set; }
        
        /// <summary>
        /// Filename Id (= filename without extension).
        /// This is unique per database.
        /// </summary>
        public string FilenameId { get; protected set; }

        /// <summary>
        /// Structure extension with a dot.
        /// </summary>
        public string Extension { get; protected set; }
        
        /// <summary>
        /// Structure type.
        /// </summary>
        public StructureType StructureType { get { return StructureReader.GetStructureType(FilenameId + Extension); } }
        
        /// <summary>
        /// In which version of the DB was the structure added.
        /// </summary>
        public int Version { get; protected set; }

        /// <summary>
        /// Read the structure this entry refers to.
        /// </summary>
        /// <returns></returns>
        public StructureReaderResult ReadStructure()
        {
            var filename = Path.Combine(Database.GetDataFolderPath(), FilenameId + Extension);
            var ret = StructureReader.Read(filename, customId: FilenameId);
            //if (ret.Structure != null) ret.Structure.ReadRings(filename + ".rgs");
            return ret;
        }

        /// <summary>
        /// Does File.OpenRead on the structure file.
        /// </summary>
        /// <returns></returns>
        public Stream GetSourceStream()
        {
            var filename = Path.Combine(Database.GetDataFolderPath(), FilenameId + Extension);
            return File.OpenRead(filename);
        }

        /// <summary>
        /// Reads the source text of the structure.
        /// </summary>
        /// <returns></returns>
        public string ReadSource()
        {
            var filename = Path.Combine(Database.GetDataFolderPath(), FilenameId + Extension);
            return File.ReadAllText(filename);
        }

        /////// <summary>
        /////// Mark the entry as obsolete.
        /////// </summary>
        ////internal void MarkObsolete()
        ////{
        ////    this.IsObsolete = true;
        ////    if (this.xml != null)
        ////    {
        ////        this.xml.Attribute("IsObsolete").SetValue(IsObsolete);
        ////    }
        ////}

        /// <summary>
        /// Convert to XML representation.
        /// </summary>
        /// <returns></returns>
        public XElement ToEntryXml()
        {
            if (xml != null) return xml;

            xml = new XElement("Entry",
                new XAttribute("FilenameId", FilenameId),
                new XAttribute("Extension", Extension),
                new XAttribute("Version", Version));

            return xml;
        }

        /// <summary>
        /// Create an entry from XML element.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="dbInfo"></param>
        /// <returns></returns>
        public static DatabaseEntry FromEntryXml(XElement e, DatabaseInfo database)
        {
            return new DatabaseEntry(database, e);
        }

        protected DatabaseEntry(DatabaseInfo database, XElement entry)
        {
            Database = database;
            xml = entry;
            FilenameId = entry.Attribute("FilenameId").Value;
            Extension = entry.Attribute("Extension").Value;
            var ver = entry.Attribute("Version").Value;
            Version = NumberParser.ParseIntFast(ver, 0, ver.Length);
        }

        protected DatabaseEntry(DatabaseInfo database)
        {
            Database = database;
        }
    }

}
