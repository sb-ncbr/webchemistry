namespace WebChemistry.Platform.MoleculeDatabase
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using WebChemistry.Framework.Core;
    
    /// <summary>
    /// Intex entry.
    /// </summary>
    public class DatabaseIndexEntry : DatabaseEntry
    {
        static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
        public const string DateFormat = "yyyy-M-d";
        public const char ValueSeparator = '¦';
        public const string ValueSeparatorString = "¦";

        XElement XmlElement, PropertiesElement, SrcTimestampElement;

        /// <summary>
        /// Get the source timestamp (Utc ticks)
        /// </summary>
        /// <returns></returns>
        public long? GetSourceTimestamp()
        {
            if (SrcTimestampElement == null) return null;
            return long.Parse(SrcTimestampElement.Value);
        }

        /// <summary>
        /// Get the integer property.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int? GetInt(string name)
        {
            var attr = PropertiesElement.Attribute(name);
            if (attr == null) return null;
            var val = attr.Value;
            return NumberParser.ParseIntFast(val, 0, val.Length);
        }

        /// <summary>
        /// Get long value.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public long? GetLong(string name)
        {
            var attr = PropertiesElement.Attribute(name);
            if (attr == null) return null;
            var val = attr.Value;
            return long.Parse(val);
        }

        /// <summary>
        /// Get the value.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public double? GetDouble(string name)
        {
            var attr = PropertiesElement.Attribute(name);
            if (attr == null) return null;
            var val = attr.Value;
            return NumberParser.ParseDoubleFast(val, 0, val.Length);
        }

        /// <summary>
        /// Get the string value.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetString(string name)
        {
            var attr = PropertiesElement.Attribute(name);
            if (attr == null) return null;
            return attr.Value;
        }

        /// <summary>
        /// Separates the string value using ¦
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string[] GetStringArray(string name)
        {
            var attr = PropertiesElement.Attribute(name);
            if (attr == null) return null;
            var val = attr.Value;
            return val.Split(ValueSeparator);
        }
        
        /// <summary>
        /// Number of atoms in the structure.
        /// </summary>
        public int AtomCount { get { return GetInt("AtomCount").Value; } }

        /// <summary>
        /// Number of atoms in the structure.
        /// </summary>
        public long SizeInBytes { get { return GetLong("SizeInBytes").Value; } }

        /// <summary>
        /// Convert to XML representation.
        /// </summary>
        /// <returns></returns>
        public XElement ToIndexXml()
        {
            if (XmlElement != null) return XmlElement;

            var entryXml = ToEntryXml();
            XmlElement = new XElement("IndexEntry",
                entryXml,
                SrcTimestampElement,
                PropertiesElement);

            return XmlElement;
        }

        /// <summary>
        /// Create an entry from XML element.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="dbInfo"></param>
        /// <returns></returns>
        public static DatabaseIndexEntry FromIndexXml(XElement e, DatabaseInfo database)
        {
            var entry = e.Element("Entry");
            var properties = e.Element("Properties");

            var ret = new DatabaseIndexEntry(database, e.Element("Entry"))
            {
                XmlElement = e,
                PropertiesElement = e.Element("Properties"),
                SrcTimestampElement = e.Element("SrcTimestamp")
            };

            return ret;
        }

        public const string RingFingerprintsPropertyName = "RingFingerprints";

        /// <summary>
        /// Construct an index entry from a structure. Structures alredy needs to be in the data folder.
        /// Also creates bonds file if appropriate.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="sourceFileInfo">Original file info.</param>
        /// <param name="version"></param>
        /// <param name="dbInfo"></param>
        /// <returns></returns>
        public static DatabaseIndexEntry CreateFromStructure(string filename, FileInfo sourceFileInfo, int version, DatabaseInfo dbInfo)
        {
            var fi = new FileInfo(filename);
            var sw = StructureReader.Read(filename);
            var s = sw.Structure;
            //s.WriteRings(filename + ".rgs");

            var properties = new XElement("Properties");
            Action<string, object> addAttribute = (name, value) =>
                {
                    if (value == null) return;
                    var val = value.ToString();
                    if (string.IsNullOrEmpty(val)) return;
                    properties.Add(new XAttribute(name, val));
                };
            Action<string, IEnumerable<object>> addArrayAttribute = (name, values) =>
                {
                    var val = values.JoinBy(ValueSeparatorString);
                    if (string.IsNullOrEmpty(val)) return;
                    properties.Add(new XAttribute(name, val));
                };

            addAttribute("AtomCount", s.Atoms.Count);
            addAttribute("SizeInBytes", fi.Length);
            addArrayAttribute("AtomTypes", s.Atoms.Select(a => a.ElementSymbol).Distinct().Select(e => e.ToString()));
            addArrayAttribute(RingFingerprintsPropertyName, s.Rings().Select(r => r.Fingerprint).Distinct(StringComparer.Ordinal));

            if (s.IsPdbStructure())
            {
                addAttribute("ResidueCount", s.PdbResidues().Count);
                addArrayAttribute("ResidueTypes", s.PdbResidues().Select(r => r.Name).Distinct(StringComparer.OrdinalIgnoreCase));
                var metadata = s.PdbMetadata();
                addAttribute("Weight", metadata.TotalWeightInKda.ToStringInvariant());
                if (metadata.Resolution.HasValue) addAttribute("Resolution", metadata.Resolution.Value.ToStringInvariant());
                if (metadata.Released.HasValue) addAttribute("ReleasedDate", metadata.Released.Value.ToString(DateFormat, Culture));
                if (metadata.LatestRevision.HasValue) addAttribute("LatestRevisionDate", metadata.LatestRevision.Value.ToString(DateFormat, Culture));

                addAttribute("ExperimentMethod", metadata.ExperimentMethod);
                addAttribute("PolymerType", metadata.PolymerType);
                addAttribute("ProteinStoichiometry", metadata.ProteinStoichiometry);
                addAttribute("ProteinStoichiometryString", metadata.ProteinStoichiometryString);

                addArrayAttribute("Authors", metadata.Authors);
                addArrayAttribute("Keywords", metadata.Keywords);
                addArrayAttribute("EcNumbers", metadata.EcNumbers);
                addArrayAttribute("HostOrganisms", metadata.HostOrganisms);
                addArrayAttribute("HostOrganismsGenus", metadata.HostOrganismsGenus);
                addArrayAttribute("HostOrganismsId", metadata.HostOrganismsId);
                addArrayAttribute("OriginOrganisms", metadata.OriginOrganisms);
                addArrayAttribute("OriginOrganismsGenus", metadata.OriginOrganismsGenus);
                addArrayAttribute("OriginOrganismsId", metadata.OriginOrganismsId);
            }

            return new DatabaseIndexEntry(dbInfo)
            {
                FilenameId = StructureReader.GetStructureIdFromFilename(filename).ToLowerInvariant(),
                Extension = fi.Extension.ToLower(),
                Version = version,
                PropertiesElement = properties,
                SrcTimestampElement = new XElement("SrcTimestamp", sourceFileInfo.LastWriteTimeUtc.Ticks)
            };
        }

        /// <summary>
        /// Return the entry properties as a dictionary.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetPropertiesAsDictionary()
        {
            return PropertiesElement.Attributes()
                .ToDictionary(e => e.Name.LocalName, e => e.Value, StringComparer.Ordinal);
        }

        /// <summary>
        /// Get the default entry exporter.
        /// </summary>
        /// <param name="xs"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static ListExporter<DatabaseIndexEntry> GetDefaultExporter(IEnumerable<DatabaseIndexEntry> xs, string separator = ",")
        {
            return xs.GetExporter(separator)
                .AddStringColumn(e => e.FilenameId, "Id")
                .AddNumericColumn(e => e.GetString("SizeInBytes"), "SizeInBytes")
                .AddNumericColumn(e => e.GetString("AtomCount"), "AtomCount")
                .AddNumericColumn(e => e.GetString("ResidueCount"), "ResidueCount")
                .AddNumericColumn(e => e.GetString("Weight"), "WeightInkDa")
                .AddNumericColumn(e => e.GetString("Resolution"), "ResolutionInAng")
                .AddStringColumn(e => e.GetString("ReleasedDate"), "ReleaseDate")
                .AddStringColumn(e => e.GetString("LatestRevisionDate"), "LatestRevisionDate")
                .AddStringColumn(e => e.GetString("ExperimentMethod"), "ExperimentMethod")
                .AddStringColumn(e => e.GetString("PolymerType"), "PolymerType")
                .AddStringColumn(e => e.GetString("ProteinStoichiometry"), "ProteinStoichiometry")
                .AddStringColumn(e => e.GetString("ProteinStoichiometryString"), "ProteinStoichiometryString")
                .AddStringColumn(e => e.GetString("Authors"), "Authors")
                .AddStringColumn(e => e.GetString("Keywords"), "Keywords")
                .AddStringColumn(e => e.GetString("EcNumbers"), "EcNumbers")
                .AddStringColumn(e => e.GetString("HostOrganisms"), "HostOrganisms")
                .AddStringColumn(e => e.GetString("HostOrganismsGenus"), "HostOrganismsGenus")
                .AddStringColumn(e => e.GetString("HostOrganismsId"), "HostOrganismsId")
                .AddStringColumn(e => e.GetString("OriginOrganisms"), "OriginOrganisms")
                .AddStringColumn(e => e.GetString("OriginOrganismsGenus"), "OriginOrganismsGenus")
                .AddStringColumn(e => e.GetString("OriginOrganismsId"), "OriginOrganismsId")
                .AddStringColumn(e => e.GetString("AtomTypes"), "AtomTypes")
                .AddStringColumn(e => e.GetString("ResidueTypes"), "ResidueTypes");
        }

        /// <summary>
        /// Construct an index entry.
        /// </summary>
        /// <param name="dbInfo"></param>
        private DatabaseIndexEntry(DatabaseInfo dbInfo)
            : base(dbInfo)
        {
        }

        private DatabaseIndexEntry(DatabaseInfo dbInfo, XElement baseEntry)
            : base(dbInfo, baseEntry)
        {
        }
    }

}
