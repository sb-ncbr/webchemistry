namespace WebChemistry.Queries.Core.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Pdb;

    /// <summary>
    /// Catalitic Site Atlas wrapper
    /// </summary>
    public static class CatalyticSiteAtlas
    {
        class Entry
        {
            public string PdbID { get; set; }
            public int SiteNumber { get; set; }
            public string ChainID { get; set; }
            public int ResidueNumber { get; set; }
        }

        /// <summary>
        /// Site info.
        /// </summary>
        public class Info
        {
            public string PdbID { get; set; }
            public int SiteNumber { get; set; }
            public ReadOnlyCollection<PdbResidue> Residues { get; set; }
        }

        static bool initialized = false;
        static Dictionary<string, Entry[]> activeSites;

        static void InitInternal(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new ArgumentException(string.Format("CSA file '{0}' does not exist.", filename));
            }

            var split = new char[] { ',' };
            activeSites = File.ReadLines(filename)
                .Skip(1)
                .Select(l => l.Split(split, StringSplitOptions.RemoveEmptyEntries))
                .Where(f => f.Length == 8)
                .Select(f => new Entry
                {
                    PdbID = f[0],
                    SiteNumber = int.Parse(f[1]),
                    ChainID = f[3],
                    ResidueNumber = int.Parse(f[4])
                })
                .GroupBy(e => e.PdbID)
                .ToDictionary(e => e.Key.ToUpper(), g => g.ToArray(), StringComparer.InvariantCultureIgnoreCase);

            initialized = true;
        }

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="filename"></param>
        public static void Init(string filename)
        {
            InitInternal(filename);
        }

        static IEnumerable<Info> FromEntries(IEnumerable<Entry> entries, IStructure structure)
        {
            var aarr = entries.ToArray();
            var residues = structure.PdbResidues();
            return entries
                .GroupBy(e => e.SiteNumber)
                .Where(g => residues.FromIdentifier(PdbResidueIdentifier.Create(g.First().ResidueNumber, g.First().ChainID, ' ')) != null)
                .Select(e => CreateInfo(e.OrderBy(r => r.ChainID).ThenBy(r => r.ResidueNumber), structure))
                .ToArray();
        }

        static Info CreateInfo(IEnumerable<Entry> entries, IStructure structure)
        {
            var fe = entries.First();
            return new Info
            {
                PdbID = fe.PdbID,
                SiteNumber = fe.SiteNumber,
                Residues = new ReadOnlyCollection<PdbResidue>(entries.Select(r => structure.PdbResidues().FromIdentifier(PdbResidueIdentifier.Create(r.ResidueNumber, r.ChainID, ' '))).ToList())
            };
        }

        /// <summary>
        /// Get active sites for a given structure.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static IEnumerable<Info> GetActiveSites(IStructure structure)
        {
            if (!initialized) throw new InvalidOperationException("CSA database is not initialized.");
            if (!activeSites.ContainsKey(structure.Id)) return Enumerable.Empty<Info>();
            return FromEntries(activeSites[structure.Id], structure);
        }

        /// <summary>
        /// Get number of structure entries in the atlas.
        /// </summary>
        /// <returns></returns>
        public static int GetSize()
        {
            if (!initialized) throw new InvalidOperationException("CSA database is not initialized.");
            return activeSites.Count;
        }
    }
}
