namespace WebChemistry.Framework.Core.Pdb
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Type of the residue string.
    /// </summary>
    public enum ResidueStringType
    {
        Default = 0,
        OrderedCondensed,
        Short, 
        Counted,
        CountedShortAminoNames
    }
    
    /// <summary>
    /// Collection of residues.
    /// </summary>
    public class PdbResidueCollection : ReadOnlyCollection<PdbResidue>
    {
        /// <summary>
        /// A collection containing no residues.
        /// </summary>
        public static PdbResidueCollection Empty = new PdbResidueCollection(new PdbResidue[0]);

        /// <summary>
        /// Selection changed event.
        /// </summary>
        public event EventHandler SelectionChanged;

        void RaiseSelectionChanged()
        {
            var handler = SelectionChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        Dictionary<PdbResidueIdentifier, PdbResidue> byId;
        string[] uniqueResidueNames;
        Dictionary<string, int> residueCounts;
        
        /// <summary>
        /// Set of unique residues.
        /// </summary>
        public string[] UniqueResidueNames { get { return uniqueResidueNames; } }

        /// <summary>
        /// Residue counts.
        /// </summary>
        public Dictionary<string, int> ResidueCounts { get { return residueCounts; } }

        Lazy<string> orderedCondensedResidueString, residueString, shortResidueString, countedResidueString, countedShortAminoNames;

        /// <summary>
        /// String of the form ("Name" "Number" "Chain")*
        /// </summary>
        public string ResidueString
        {
            get { return residueString.Value; }
        }

        /// <summary>
        /// One letter abbreviations "ABC"
        /// </summary>
        public string ShortResidueString
        {
            get { return shortResidueString.Value; }
        }

        /// <summary>
        /// Ordered residue names (X-Y-Z)
        /// </summary>
        public string OrderedCondensedResidueString
        {
            get { return orderedCondensedResidueString.Value; }
        }
        
        /// <summary>
        /// Example: CYS*2-HIS*2
        /// </summary>
        public string CountedResidueString
        {
            get { return countedResidueString.Value; }
        }

        /// <summary>
        /// Example: 1xDMU-2xW-1xT
        /// </summary>
        public string CountedShortAminoNamesString
        {
            get { return countedShortAminoNames.Value; }
        }

        /// <summary>
        /// Concatenates the residue identifiers into a string.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string GetResidueString(ResidueStringType type = ResidueStringType.Default)
        {
            switch (type)
            {
                case ResidueStringType.Default: return ResidueString;
                case ResidueStringType.Short: return ShortResidueString;
                case ResidueStringType.OrderedCondensed: return OrderedCondensedResidueString;
                case ResidueStringType.Counted: return CountedResidueString;
                case ResidueStringType.CountedShortAminoNames: return CountedShortAminoNamesString;
            }
            return ResidueString;
        }
        
        /// <summary>
        /// Return a residue from an atom.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public PdbResidue FromAtom(IAtom atom)
        {
            return byId[atom.ResidueIdentifier()];
        }
                
        /// <summary>
        /// Get a residue from identifier.
        /// Returns null if the residue is not present.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public PdbResidue FromIdentifier(PdbResidueIdentifier id)
        {
            return byId.DefaultIfNotPresent(id);
        }
        
        /// <summary>
        /// Check if the collections contains a residue with the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ContainsResidueName(string name)
        {
            return residueCounts.ContainsKey(name);
        }

        /// <summary>
        /// Returns the counted residue string: DMU*2-ASP*3-ASN
        /// </summary>
        /// <param name="counts"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string GetCountedResidueString(IDictionary<string, int> counts, string separator = "-")
        {
            return string.Join(separator, counts
                        .OrderBy(g => g.Key)
                        .Select(g => g.Value > 1 ? g.Key + "*" + g.Value : g.Key));
        }

        /// <summary>
        /// Returns the counted residue string: DMU*2-ASP*3-ASN*1
        /// </summary>
        /// <param name="counts"></param>
        /// <param name="separator"
        /// <returns></returns>
        public static string GetExplicitlyCountedResidueString(IDictionary<string, int> counts, string separator = "-")
        {
            return string.Join(separator, counts
                        .OrderBy(g => g.Key)
                        .Select(g => g.Key + "*" + g.Value));
        }

        /// <summary>
        /// Returns the counted residue string: DMU*2-A*3-R*5
        /// </summary>
        /// <param name="counts"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string GetCountedShortAminoNamesString(IDictionary<string, int> counts, string separator = "-")
        {
            return string.Join(separator, counts
                        .OrderBy(r => PdbResidue.IsAminoName(r.Key) ? 1 : 0)
                        .ThenBy(r => PdbResidue.GetShortAminoName(r.Key))
                        .ThenBy(r => r.Value)
                        .Select(r => r.Value > 1 ? PdbResidue.GetShortAminoName(r.Key) + "*" + r.Value : PdbResidue.GetShortAminoName(r.Key)));
        }

        /// <summary>
        /// Residue identifiers separated by -.
        /// </summary>
        /// <param name="residues"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string GetIdentifierString(IEnumerable<PdbResidue> residues, string separator = "-")
        {
            return string.Join(separator, residues.OrderBy(r => r.Identifier).Select(r => r.ToString()));
        }

        void Init()
        {
            this.byId = this.ToDictionary(r => r.Identifier);
            
            residueString = Lazy.Create(() => string.Join(", ", this.Select(r => string.Format("{0} {1} {2}", r.Name, r.Number, r.ChainIdentifier))));
            shortResidueString = Lazy.Create(() => string.Concat(this.Select(r => r.ShortName)));
            orderedCondensedResidueString = Lazy.Create(() => string.Join("-", this.Select(r => r.Name).OrderBy(n => n)));
            countedResidueString = Lazy.Create(() => GetCountedResidueString(residueCounts));
            countedShortAminoNames = Lazy.Create(() => GetCountedShortAminoNamesString(residueCounts));

            var uniqueResidueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            residueCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var r in this)
            {
                if (uniqueResidueNames.Add(r.Name))
                {
                    residueCounts.Add(r.Name, 1);
                }
                else
                {
                    residueCounts[r.Name] = residueCounts[r.Name] + 1;
                }
            }

            this.uniqueResidueNames = uniqueResidueNames.ToArray();
        }

        /// <summary>
        /// Creates new residue collection.
        /// </summary>
        /// <param name="residues"></param>
        private PdbResidueCollection(IEnumerable<PdbResidue> residues)
            : base(residues.AsList())
        {
            Init();
        }

        /// <summary>
        /// Create the residue collection.
        /// </summary>
        /// <param name="residues"></param>
        /// <returns></returns>
        public static PdbResidueCollection Create(IEnumerable<PdbResidue> residues)
        {
            return new PdbResidueCollection(residues);
        }
    }
}