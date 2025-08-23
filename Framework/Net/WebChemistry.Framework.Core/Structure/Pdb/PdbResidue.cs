namespace WebChemistry.Framework.Core.Pdb
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text.RegularExpressions;
    using WebChemistry.Framework.Math;
    
    /// <summary>
    /// Residue secondary structure type.
    /// </summary>
    public enum SecondaryStructureType
    {
        /// <summary>
        /// Not known.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Sheet
        /// </summary>
        Sheet = 1,

        /// <summary>
        /// Helix
        /// </summary>
        Helix = 2
    }

    /// <summary>
    /// Charge info taken from 
    /// Textbook of Structural Biology
    /// A. Liljas, L Lijas, J Piskur, G Lindblom, O Nissen, M Kjeldgaard
    /// Chapter 2.1.1, Figure 2.2 (page 13)
    /// </summary>
    public enum ResidueChargeType
    {
        Unknown = 0,
        Positive,
        Negative,
        Aromatic,
        Polar,
        NonPolar
    }

    static class PdbResidueIdentifierHelper
    {
        const string regex = @"^\s*(?<n>[0-9]+)((\s+(?!(i[:]))(?<c>[_,.;:""&<>()/\{}'`~!@#$%A-Za-z0-9*|+-]*)){0,1})((\s+i[:](?<ic>[a-zA-Z0-9])){0,1})\s*$";
        public static Regex Parser = new Regex(regex, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        static StringComparer OrdinalComparer = StringComparer.Ordinal;
        public static int CompareOrdinal(string a, string b)
        {
            return OrdinalComparer.Compare(a, b);
        }
    }
    
    /// <summary>
    /// An unique residue identifier.
    /// </summary>
    [Newtonsoft.Json.JsonConverter(typeof(WebChemistry.Framework.Core.Json.PdbResidueIdentifierJsonConverter))]
    public struct PdbResidueIdentifier : IEquatable<PdbResidueIdentifier>, IComparable<PdbResidueIdentifier>, IComparable
    {
        private readonly int Hash;

        /// <summary>
        /// PDB ResidueSerialNumber.
        /// </summary>
        public readonly int Number;

        /// <summary>
        /// Chain the residue is on.
        /// </summary>
        public readonly string ChainIdentifier;

        /// <summary>
        /// Insertion residue code.
        /// </summary>
        public readonly char InsertionResidueCode;

        ////public override string ToString()
        ////{
        ////    return ToString(false);
        ////}

        public override string ToString()
        {
            var emptyChain = string.IsNullOrWhiteSpace(ChainIdentifier);
            if (emptyChain && InsertionResidueCode == ' ')// || ignoreInsertionCode))
            {
                return Number.ToString();
            }
            if (emptyChain)
            {
                return string.Format("{0} i:{1}", Number, InsertionResidueCode);
            }
            if (InsertionResidueCode == ' ')// || ignoreInsertionCode)
            {
                return string.Format("{0} {1}", Number, ChainIdentifier);
            }            
            return string.Format("{0} {1} i:{2}", Number, ChainIdentifier, InsertionResidueCode);
        }

        /// <summary>
        /// Returns the hash code computed as a hash of a 64bit id.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Hash;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (!(obj is PdbResidueIdentifier)) return false;
            var other = (PdbResidueIdentifier)obj;
            if (other == null) return false;

            return this.Number == other.Number 
                && this.ChainIdentifier.EqualOrdinal(other.ChainIdentifier)
                && this.InsertionResidueCode == other.InsertionResidueCode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(PdbResidueIdentifier other)
        {
            return this.Number == other.Number
                && this.ChainIdentifier.EqualOrdinal(other.ChainIdentifier)
                && this.InsertionResidueCode == other.InsertionResidueCode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(PdbResidueIdentifier a, PdbResidueIdentifier b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(PdbResidueIdentifier a, PdbResidueIdentifier b)
        {
            return !a.Equals(b);
        }
        
        /// <summary>
        /// Creates the identifier from atom.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static PdbResidueIdentifier FromAtom(IAtom atom)
        {
            var pdbAtom = atom as PdbAtom;
            if (pdbAtom != null)
            {
                return new PdbResidueIdentifier(pdbAtom.ResidueSequenceNumber, pdbAtom.ChainIdentifier, pdbAtom.InsertionResidueCode);
            }
            return new PdbResidueIdentifier(atom.PdbResidueSequenceNumber(), atom.PdbChainIdentifier(), atom.PdbInsertionResidueCode());
        }

        /// <summary>
        /// Create a new identifier.
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="chainId"></param>
        /// <param name="insertionResidueCode"></param>
        /// <returns></returns>
        public static PdbResidueIdentifier Create(int sequenceNumber, string chainId, char insertionResidueCode)
        {
            return new PdbResidueIdentifier(sequenceNumber, chainId, insertionResidueCode);
        }

        static void ThrowInvalidFormat(string value)
        {
            throw new ArgumentException(string.Format("'{0}' is not a valid residue identifier. The format is 'NUMBER [CHAIN] [i:INSERTIONCODE]' (parameters in [] are optional, for example '175 i:12' or '143 B').", value), "value");
        }

        /// <summary>
        /// Parse the identifier from a value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static PdbResidueIdentifier Parse(string value)
        {
            var match = PdbResidueIdentifierHelper.Parser.Match(value);
            if (!match.Success) ThrowInvalidFormat(value);
            int number = int.Parse(match.Groups["n"].Value);
            var g = match.Groups["c"];
            string chain = g.Success ? g.Value : "";
            g = match.Groups["ic"];
            char ic = g.Success ? g.Value[0] : ' ';
            return Create(number, chain, ic);
        }

        
        /// <summary>
        /// Creates the identifier.
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="chainId"></param>
        /// <param name="insertionResidueCode"></param>
        public PdbResidueIdentifier(int sequenceNumber, string chainId, char insertionResidueCode)
        {
            if (string.IsNullOrWhiteSpace(chainId)) ChainIdentifier = "";
            else ChainIdentifier = chainId;
            Number = sequenceNumber;
            InsertionResidueCode = insertionResidueCode;

            int hash = 31;
            hash = (23 * hash) + ChainIdentifier.GetHashCode();
            hash = (23 * hash) + Number.GetHashCode();
            hash = (23 * hash) + InsertionResidueCode.GetHashCode();
            Hash = hash;
        }

        /// <summary>
        /// Compare the IDs lexicographically based on (Chain, Number, InsertionResidueCode)
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(PdbResidueIdentifier other)
        {
            if (this.ChainIdentifier == other.ChainIdentifier)
            {
                if (this.Number == other.Number)
                {
                    return this.InsertionResidueCode.CompareTo(other.InsertionResidueCode);
                }
                return this.Number.CompareTo(other.Number);
            }
            return PdbResidueIdentifierHelper.CompareOrdinal(this.ChainIdentifier, other.ChainIdentifier);
        }

        /// <summary>
        /// Compares identifiers.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (obj is PdbResidueIdentifier)
            {
                return CompareTo((PdbResidueIdentifier)obj);
            }
            throw new ArgumentException("Cannot compare PdbResidueIdentifier to object of type " + obj.GetType().Name + ".");
        }
    }

    /// <summary>
    /// PDB residue.
    /// </summary>
    public class PdbResidue : InteractivePropertyObject, IEquatable<PdbResidue>, IInteractive, IComparable, IComparable<PdbResidue>
    {
        private ReadOnlyCollection<IAtom> atoms;
        
        /// <summary>
        /// Atoms the residue consists of.
        /// </summary>
        public IList<IAtom> Atoms { get { return atoms; } }

        /// <summary>
        /// Name of the residue.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// PDB ResidueSerialNumber.
        /// </summary>
        public int Number { get { return Identifier.Number; } }

        /// <summary>
        /// Chain the residue is on.
        /// </summary>
        public string ChainIdentifier { get { return Identifier.ChainIdentifier; } }

        /// <summary>
        /// Insertion residue code.
        /// </summary>
        public char InsertionResidueCode { get { return Identifier.InsertionResidueCode; } }

        /// <summary>
        /// Gets unique identifier of the form ChainID left 24 | ResId
        /// </summary>
        public PdbResidueIdentifier Identifier { get; private set; }
        
        /// <summary>
        /// Secondary structure type.
        /// </summary>
        public SecondaryStructureType SecondaryType { get; internal set; }

        /// <summary>
        /// The parent residue name. Null or empty if not applicable.
        /// </summary>
        public string ModifiedFrom { get; internal set; }

        /// <summary>
        /// Was the residue modified?
        /// </summary>
        public bool IsModified { get { return !string.IsNullOrEmpty(ModifiedFrom); } }

        /// <summary>
        /// Returns a string of the form "name id"
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name + " " + Identifier.ToString();
        }

        /////// <summary>
        /////// Returns a string of the form "name id"
        /////// </summary>
        /////// <param name="ignoreInsertionCode"></param>
        /////// <returns></returns>
        ////public string ToString(bool ignoreInsertionCode = false)
        ////{
        ////    return Name + " " + Identifier.ToString(ignoreInsertionCode);
        ////}

        /// <summary>
        /// Returns the hash code.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }
        
        /// <summary>
        /// Creates a residue from atoms and structure.
        /// </summary>
        /// <param name="atoms"></param>
        private PdbResidue(ReadOnlyCollection<IAtom> atoms)
        {
            this.atoms = atoms;
            var a = this.atoms[0];
            Identifier = PdbResidueIdentifier.FromAtom(a);
            Name = a.PdbResidueName();
        }

        /// <summary>
        /// Create the residue.
        /// </summary>
        /// <param name="atoms"></param>
        /// <returns></returns>
        public static PdbResidue Create(IEnumerable<IAtom> atoms)
        {
            return new PdbResidue(new ReadOnlyCollection<IAtom>(atoms.AsList()));
        }

        /// <summary>
        /// Gets the distance of the closest atoms.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double Distance(PdbResidue a, PdbResidue b)
        {
            double min = double.MaxValue;

            var xs = a.Atoms;
            var ys = b.Atoms;

            for (int i = 0; i < xs.Count; i++)
            {
                var x = xs[i];
                for (int j = 0; j < ys.Count; j++)
                {
                    var y = ys[j];
                    var d = x.Position.DistanceTo(y.Position);
                    if (d < min) min = d;
                }
            }

            return min;
        }
        
        /// <summary>
        /// Select every atom on the residue.
        /// </summary>
        protected override void OnSelectedChanged()
        {
            foreach (var a in Atoms)
            {
                a.IsSelected = this.IsSelected;
            }
        }

        /// <summary>
        /// Highlight every atom on the residue.
        /// </summary>
        protected override void  OnHighlightedChanged()
        {
            foreach (var a in Atoms)
            {
                a.IsHighlighted = this.IsHighlighted;
            }
        }
        
        /// <summary>
        /// Check the equality using identifiers.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(PdbResidue other)
        {
            return this.Identifier == other.Identifier;
        }

        /// <summary>
        /// Gets the carbonyl oxygen is there is one.
        /// </summary>
        /// <returns></returns>
        public IAtom GetCarbonylOxygen()
        {
            return Atoms.FirstOrDefault(a => a.PdbName().EqualIgnoreCase("O"));
        }

        /// <summary>
        /// Gets the C-alpha atom if there is one.
        /// </summary>
        /// <returns></returns>
        public IAtom GetCAlpha()
        {
            if (!IsAmino) return null;
            return Atoms.FirstOrDefault(a => a.PdbName().EqualIgnoreCase("CA"));
        }
        
        bool? _isAmino;
        /// <summary>
        /// Checks if the residue is an amino acid.
        /// </summary>
        public bool IsAmino 
        { 
            get 
            {
                if (_isAmino == null) _isAmino = IsAminoName(Name);
                return _isAmino.Value;
            } 
        }

        bool? _isNucleotide;
        /// <summary>
        /// Checks if the residue is a nucleotide.
        /// </summary>
        public bool IsNucleotide
        {
            get
            {
                if (_isNucleotide == null) _isNucleotide = IsNucleotideName(Name);
                return _isNucleotide.Value;
            }
        }
        
        bool? _isWater;
        public bool IsWater { 
            get 
            {
                return Name.EqualOrdinalIgnoreCase("HOH");
            } 
        }


        /// <summary>
        /// HOH, SOL, WAT
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsWaterName(string name)
        {
            return name.EqualOrdinalIgnoreCase("HOH");
        }

        static HashSet<string> aminoNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ALA", "ARG", "ASP", "CYS", "GLN", "GLU", "GLY", "HIS", "ILE", "LEU", "LYS", "MET", "PHE", "PRO", "SER", "THR", "TRP", "TYR", "VAL", "ASN"
        };

        static string[] aminoNamesList = new [] 
        {
            "ALA", "ARG", "ASP", "CYS", "GLN", "GLU", "GLY", "HIS", "ILE", "LEU", "LYS", "MET", "PHE", "PRO", "SER", "THR", "TRP", "TYR", "VAL", "ASN"
        };

        /// <summary>
        /// The list of amino names.
        /// </summary>
        public static IEnumerable<string> AminoNamesList 
        {
            get { return aminoNamesList; }
        }

        static HashSet<string> nucleotideNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "A", "C", "G", "T", "U", "DA", "DC", "DG", "DT", "DU"
        };

        static string[] nucleotideNamesList = new[] 
        {
            "A", "C", "G", "T", "U", "DA", "DC", "DG", "DT", "DU"
        };

        /// <summary>
        /// The list of nucleotide names.
        /// </summary>
        public static IEnumerable<string> NucleotideNamesList
        {
            get { return nucleotideNamesList; }
        }

        /////// <summary>
        /////// Charge info taken from 
        /////// Textbook of Structural Biology
        /////// A. Liljas, L Lijas, J Piskur, G Lindblom, O Nissen, M Kjeldgaard
        /////// Chapter 2.1.1, Figure 2.2 (page 13)
        /////// </summary>
        ////static HashSet<string> positiveChargeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        ////{
        ////    "LYS", "ARG", "HIS"
        ////};

        ////static HashSet<string> negativeChargeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        ////{
        ////    "ASP", "GLU"
        ////};

        ////static HashSet<string> polarChargeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        ////{
        ////    "CYS", "SER", "THR", "ASN", "GLN", "TYR", "TRP"
        ////};

        ////static HashSet<string> nonPolarChargeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        ////{
        ////    "PHE", "MET", "LEU", "VAL", "ILE", "ALA", "GLY", "PRO"
        ////};

        // taken from http://sbrs.cm.utexas.edu/WS/aa.pdf
        static HashSet<string> positiveChargeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LYS", "ARG", "HIS"
        };

        static HashSet<string> negativeChargeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ASP", "GLU"
        };

        static HashSet<string> aromaticChargeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PHE", "TYR", "TRP"
        };

        static HashSet<string> polarChargeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CYS", "SER", "THR", "ASN", "GLN"
        };

        static HashSet<string> nonPolarChargeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MET", "LEU", "VAL", "ILE", "ALA", "GLY", "PRO"
        };


        /////// <summary>
        /////// Charge info taken from 
        /////// Textbook of Structural Biology
        /////// A. Liljas, L Lijas, J Piskur, G Lindblom, O Nissen, M Kjeldgaard
        /////// Chapter 2.1.1, Figure 2.2 (page 13)
        /////// </summary>
        /////// 

        /// <summary>
        /// From http://sbrs.cm.utexas.edu/WS/aa.pdf
        /// </summary>
        public ResidueChargeType ChargeType
        {
            get
            {
                return GetChargeType(Name);
            }
        }

        /// <summary>
        /// From http://sbrs.cm.utexas.edu/WS/aa.pdf
        /// </summary>
        public static ResidueChargeType GetChargeType(string name)
        {
            if (positiveChargeNames.Contains(name)) return ResidueChargeType.Positive;
            if (negativeChargeNames.Contains(name)) return ResidueChargeType.Negative;
            if (aromaticChargeNames.Contains(name)) return ResidueChargeType.Aromatic;
            if (polarChargeNames.Contains(name)) return ResidueChargeType.Polar;
            if (nonPolarChargeNames.Contains(name)) return ResidueChargeType.NonPolar;
            return ResidueChargeType.Unknown;
        }
        
        static Dictionary<string, string> shortAminoNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ALA", "A" },
            { "ARG", "R" },
            { "ASN", "N" },
            { "ASP", "D" },
            { "CYS", "C" },
            { "GLN", "Q" },
            { "GLU", "E" },
            { "GLY", "G" },
            { "HIS", "H" },
            { "ILE", "I" },
            { "LEU", "L" },
            { "LYS", "K" },
            { "MET", "M" },
            { "PHE", "F" },
            { "PRO", "P" },
            { "SER", "S" },
            { "THR", "T" },
            { "TRP", "W" },
            { "TYR", "Y" },
            { "VAL", "V" }
        };

        static Dictionary<string, string> shortNucleotideNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "A", "A" },
            { "C", "C" },
            { "G", "G" },
            { "T", "T" },
            { "U", "U" },            
            { "DA", "A" },
            { "DC", "C" },
            { "DG", "G" },
            { "DT", "T" },
            { "DU", "U" },
        };

        /// <summary>
        /// Checks if the name is an amino acid.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsAminoName(string name)
        {
            return aminoNames.Contains(name);
        }

        /// <summary>
        /// Check if the residue is a nucleotide.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsNucleotideName(string name)
        {
            return nucleotideNames.Contains(name);
        }

        /// <summary>
        /// Gets short amino name. If the residue is not an amino acid, returns the original name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetShortAminoName(string name)
        {
            string ret;
            if (shortAminoNames.TryGetValue(name, out ret)) return ret;
            return name;
        }

        /// <summary>
        /// Gets short nucleotide name. If the residue is not a nucleotide, returns the original name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetShortNucleotideName(string name)
        {
            string ret;
            if (shortNucleotideNames.TryGetValue(name, out ret)) return ret;
            return name;
        }
        
        string _shortName = null;
        /// <summary>
        /// Single letter name of the AminoAcid. If not amino acid, the 1st letter.
        /// </summary>
        public string ShortName
        {
            get
            {
                if (_shortName == null)                {

                    switch (Name.ToUpper())
                    {
                        case "ALA": _shortName = "A"; break;
                        case "ARG": _shortName = "R"; break;
                        case "ASN": _shortName = "N"; break;
                        case "ASP": _shortName = "D"; break;
                        case "CYS": _shortName = "C"; break;
                        case "GLN": _shortName = "Q"; break;
                        case "GLU": _shortName = "E"; break;
                        case "GLY": _shortName = "G"; break;
                        case "HIS": _shortName = "H"; break;
                        case "ILE": _shortName = "I"; break;
                        case "LEU": _shortName = "L"; break;
                        case "LYS": _shortName = "K"; break;
                        case "MET": _shortName = "M"; break;
                        case "PHE": _shortName = "F"; break;
                        case "PRO": _shortName = "P"; break;
                        case "SER": _shortName = "S"; break;
                        case "THR": _shortName = "T"; break;
                        case "TRP": _shortName = "W"; break;
                        case "TYR": _shortName = "Y"; break;
                        case "VAL": _shortName = "V"; break;
                        default: _shortName = string.IsNullOrEmpty(Name) ? "X" : Name.Substring(0, 1); break;
                    }
                }

                return _shortName;
            }
        }
                
        /// <summary>
        /// Compares two atom residue identifiers (-1 -> a is smaller than b, 0 -> equal, 1 -> a is greater than b)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int Compare(IAtom a, IAtom b)
        {
            return a.ResidueIdentifier().CompareTo(b.ResidueIdentifier());
        }

        /// <summary>
        /// Compares the residue identifiers.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(PdbResidue other)
        {
            return this.Identifier.CompareTo(other.Identifier);
        }

        /// <summary>
        /// Compares the residue identifiers.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            var other = obj as PdbResidue;
            if (other != null) return CompareTo(other);
            return 1;
        }
    }
}
