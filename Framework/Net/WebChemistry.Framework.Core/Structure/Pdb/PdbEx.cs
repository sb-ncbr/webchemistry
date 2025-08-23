
namespace WebChemistry.Framework.Core
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.Framework.Math;

    /// <summary>
    /// Pdb structure extensions.
    /// </summary>
    public static class PdbEx
    {       
        /// <summary>
        /// C alpha by any chance?
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static bool IsCAlpha(this IAtom atom)
        {
            return atom.PdbName().EqualOrdinalIgnoreCase("CA");
        }

        /// <summary>
        /// Is this a water atom?
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static bool IsWater(this IAtom atom)
        {
            return atom.PdbResidueName().EqualOrdinalIgnoreCase("HOH");
        }

        /// <summary>
        /// HETATM || !AminoName (20 AKM)
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static bool IsHetAtom(this IAtom atom)
        {
            return atom.PdbRecordName().EqualOrdinalIgnoreCase("HETATM") || !PdbResidue.IsAminoName(atom.PdbResidueName());
        }

        /// <summary>
        /// Sctricly HETATM.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static bool IsHetAtomStrict(this IAtom atom)
        {
            return atom.PdbRecordName().EqualOrdinalIgnoreCase("HETATM");
        }

        static HashSet<string> backboneNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            { "C", "N", "O", "H", "CA", "P", "O1P", "O2P", "OP1", "OP2", "O5'", "C5'", "C4'", "O4'", "C1'", "C2'", "C3'", "O3'", "O2'" };

        /// <summary>
        /// Determines whether the atom is an Amino or DNA/RNA backbone atom.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static bool IsBackboneAtom(this IAtom atom)
        {
            return backboneNames.Contains(atom.PdbName());
            //return name.EqualOrdinalIgnoreCase("C") || name.EqualOrdinalIgnoreCase("N") || name.EqualOrdinalIgnoreCase("O") || name.EqualOrdinalIgnoreCase("H");
        }

        /// <summary>
        /// PDB residues are uniquely identified by chain identifier and seq. number.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns>  </returns>
        public static PdbResidueIdentifier ResidueIdentifier(this IAtom atom)
        {
            return PdbResidueIdentifier.FromAtom(atom);
        }

        /// <summary>
        /// Residue string Name Number Chain
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static string ResidueString(this IAtom atom)
        {
            var pdbAtom = atom as PdbAtom;
            if (pdbAtom != null)
            {
                var id = PdbResidueIdentifier.Create(pdbAtom.ResidueSequenceNumber, pdbAtom.ChainIdentifier, pdbAtom.InsertionResidueCode);
                return pdbAtom.ResidueName + " " + id.ToString();
            }
            var molAtom = atom as Mol2Atom;
            if (molAtom != null)
            {
                var id = molAtom.ResidueIdentifier;
                return molAtom.ResidueName + " " + id.ToString();
            }
            return "UNK 0";
        }

        /// <summary>
        /// Chain id.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static string PdbChainIdentifier(this IAtom atom)
        {
            var a = atom as PdbAtom;
            if (a != null) return a.ChainIdentifier;
            var b = atom as Mol2Atom;
            if (b != null) return b.ResidueIdentifier.ChainIdentifier;
            return "";
        }

        /// <summary>
        /// Get the entity identifier. Default id 1 for non PDB atoms.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static int PdbEntityId(this IAtom atom)
        {
            var a = atom as PdbAtom;
            if (a != null) return a.EntityId;
            return 1;
        }

        /// <summary>
        /// Insertion code.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static char PdbInsertionResidueCode(this IAtom atom)
        {
            var a = atom as PdbAtom;
            if (a != null) return a.InsertionResidueCode;
            return ' ';
        }

        /// <summary>
        /// ALt loc.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static char PdbAlternateLocationIdentifier(this IAtom atom)
        {
            var a = atom as PdbAtom;
            if (a != null) return a.AlternateLocaltionIdentifier;
            return ' ';
        }

        /// <summary>
        /// Occupancy of the atom.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static double PdbOccupancy(this IAtom atom)
        {
            var a = atom as PdbAtom;
            if (a != null) return a.Occupancy;
            return 0.0;
        }

        /// <summary>
        /// Temp factor.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static double PdbTemperatureFactor(this IAtom atom)
        {
            var a = atom as PdbAtom;
            if (a != null) return a.TemperatureFactor;
            return 1.0;
        }

        /// <summary>
        /// Residues seq. number. FOr non PDB atoms, returns 0.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static int PdbResidueSequenceNumber(this IAtom atom)
        {
            var a = atom as PdbAtom;
            if (a != null) return a.ResidueSequenceNumber;
            var b = atom as Mol2Atom;
            if (b != null) return b.ResidueIdentifier.Number;
            return 0;
        }

        /// <summary>
        /// Residue name. For non PDB atoms, returns UNK
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static string PdbResidueName(this IAtom atom)
        {
            var a = atom as PdbAtom;
            if (a != null) return a.ResidueName;
            var b = atom as Mol2Atom;
            if (b != null) return b.ResidueName;
            return "UNK";
        }

        /// <summary>
        /// Name. If the atom is not PDB, return ElementSymbol.ToString.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static string PdbName(this IAtom atom)
        {
            var a = atom as PdbAtom;
            if (a != null) return a.Name;
            var b = atom as Mol2Atom;
            if (b != null) return b.Name;
            return atom.ElementSymbol.ToString();
        }

        /// <summary>
        /// Record name. If the atom is not PDB, return "HETATM"
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static string PdbRecordName(this IAtom atom)
        {
            var a = atom as PdbAtom;
            if (a != null) return a.RecordName;
            var b = atom as Mol2Atom;
            if (b != null && PdbResidue.IsAminoName(b.ResidueName)) return "ATOM";
            return "HETATM";
        }

        /// <summary>
        /// Pdb charge - another useless crap.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static string PdbCharge(this IAtom atom)
        {
            var a = atom as PdbAtom;
            if (a != null) return a.Charge;            
            return string.Empty;
        }

        /// <summary>
        /// Segment identifier. Whatever that is.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static string PdbSegmentIdentifier(this IAtom atom)
        {
            var a = atom as PdbAtom;
            if (a != null) return a.SegmentIdentifier;
            return string.Empty;
        }

        /// <summary>
        /// Serial number of the atom. If this is not a PDB atom, the serial number is == atom.Id
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static int PdbSerialNumber(this IAtom atom)
        {
            var a = atom as PdbAtom;
            if (a != null) return a.SerialNumber;
            return atom.Id;
        }

        /// <summary>
        /// PDB Metadata.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static PdbMetadata PdbMetadata(this IStructure structure)
        {
            var ret = structure.GetProperty(PdbStructure.MetadataProperty, null);
            if (ret == null)
            {
                ret = new PdbMetadata();
                structure.SetProperty(PdbStructure.MetadataProperty, ret);
            }
            return ret;
        }
        
        /// <summary>
        /// Is this a PDB structure?
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static bool IsPdbStructure(this IStructure structure)
        {
            return structure.GetProperty(PdbStructure.IsPdbStructureProperty, false);
        }
        
        /// <summary>
        /// Determines if the structure is a PDB component structure.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static bool IsPdbComponentStructure(this IStructure structure)
        {
            return structure.GetProperty(PdbStructure.IsPdbComponentStructureProperty, false);
        }

        /// <summary>
        /// Is this a PRQ structure?
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static bool PqrContainsCharges(this IStructure structure)
        {
            return structure.GetProperty(PdbStructure.PqrContainsChargesProperty, false);
        }

        /// <summary>
        /// The spine.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static PdbBackbone PdbBackbone(this IStructure structure)
        {
            if (!structure.IsPdbStructure()) return Pdb.PdbBackbone.Empty;

            var backbone = structure.GetProperty(PdbStructure.BackboneProperty, null);
            if (backbone != null) return backbone;

            backbone = Pdb.PdbBackbone.Create(structure);
            structure.SetProperty(PdbStructure.BackboneProperty, backbone);
            return backbone;
        }
        
        /// <summary>
        /// Residues in the protein.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static PdbResidueCollection PdbResidues(this IStructure structure)
        {
            return structure.GetProperty(PdbStructure.ResiduesProperty, PdbResidueCollection.Empty);
        }


        /// <summary>
        /// Get the list of modified residues.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static IList<PdbResidue> PdbModifiedResidues(this IStructure structure)
        {
            var xs = structure.GetProperty(PdbStructure.ModifiedResiduesProperty, null);
            if (xs != null) return xs;
            xs = new ReadOnlyCollection<PdbResidue>(structure.PdbResidues().Where(r => r.IsModified).ToList());
            structure.SetProperty(PdbStructure.ModifiedResiduesProperty, xs);
            return xs;
        }

        /// <summary>
        /// Chains.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static IDictionary<string, PdbChain> PdbChains(this IStructure structure)
        {
            var value = structure.GetProperty(PdbStructure.ChainsProperty, null);
            if (value == null) return new Dictionary<string, PdbChain>(0);
            return value;
        }

        static ReadOnlyCollection<PdbHelix> emptyHelices = new ReadOnlyCollection<PdbHelix>(new PdbHelix[0]);
        /// <summary>
        /// Helices.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static ReadOnlyCollection<PdbHelix> PdbHelices(this IStructure structure)
        {
            return structure.GetProperty(PdbStructure.HelicesProperty, emptyHelices);
        }

        static ReadOnlyCollection<PdbSheet> emptySheets = new ReadOnlyCollection<PdbSheet>(new PdbSheet[0]);
        /// <summary>
        /// Sheets.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static ReadOnlyCollection<PdbSheet> PdbSheets(this IStructure structure)
        {
            return structure.GetProperty(PdbStructure.SheetsProperty, emptySheets);
        }

        /// <summary>
        /// PQR File format charge.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static double PqrCharge(this IAtom atom)
        {
            var a = atom as PdbAtom;
            if (a != null) return a.Occupancy;
            return 0.0;
        }

        /// <summary>
        /// PQR File format charge.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static double PqrRadius(this IAtom atom)
        {
            var a = atom as PdbAtom;
            if (a != null /*&& a.TemperatureFactor > 0*/) return a.TemperatureFactor;
            return ElementAndBondInfo.GetVdwRadius(atom);
        }

        /// <summary>
        /// Chirality of PDB comp atom. If not PdbCompAtom, None is returned.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static AtomChiralityRS PdbCompAtomChirality(this IAtom atom)
        {
            var ca = atom as PdbCompAtom;
            if (ca == null) return AtomChiralityRS.None;
            return ca.Chirality;
        }

        /// <summary>
        /// Model atom position of the atom.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static Vector3D? PdbCompAtomModelPosition(this IAtom atom)
        {
            var ca = atom as PdbCompAtom;
            if (ca == null) return null;
            return ca.ModelPosition;
        }

        /// <summary>
        /// Model atom position of the atom.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static Vector3D? PdbCompAtomIdealPosition(this IAtom atom)
        {
            var ca = atom as PdbCompAtom;
            if (ca == null) return null;
            return ca.IdealPosition;
        }
        
        /// <summary>
        /// Check if the structure is a valid component with model coordinates.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static bool IsValidComponentModel(this IStructure structure)
        {
            if (!structure.IsPdbComponentStructure()) return false;
            return structure.Atoms.All(a => a.PdbCompAtomModelPosition().HasValue);
        }

        /// <summary>
        /// Check if the structure is a valid component with ideal coordinates.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static bool IsValidComponentIdeal(this IStructure structure)
        {
            if (!structure.IsPdbComponentStructure()) return false;
            return structure.Atoms.All(a => a.PdbCompAtomIdealPosition().HasValue);
        }

        /// <summary>
        /// Get the component with model coordiantes.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static IStructure GetComponentModelStructure(this IStructure structure)
        {
            if (!structure.IsPdbComponentStructure()) throw new ArgumentException("Structure must be a component.");

            var newAtoms = structure.Atoms.Where(a => a.PdbCompAtomModelPosition().HasValue).Select(a => (a as PdbCompAtom).ToModelAtom()).ToDictionary(a => a.Id);
            var newBonds = structure.Bonds.Where(b => newAtoms.ContainsKey(b.A.Id) && newAtoms.ContainsKey(b.B.Id)).Select(b => Bond.Create(newAtoms[b.A.Id], newAtoms[b.B.Id], b.Type));
            var newStructure = Structure.Create(structure.Id, AtomCollection.Create(newAtoms.Values), BondCollection.Create(newBonds));
            return newStructure.AsPdbStructure(structure.PdbModifiedResidues());
        }

        /// <summary>
        /// Get the component with model coordiantes.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static IStructure GetComponentIdealStructure(this IStructure structure)
        {
            if (!structure.IsPdbComponentStructure()) throw new ArgumentException("Structure must be a component.");

            var newAtoms = structure.Atoms.Where(a => a.PdbCompAtomIdealPosition().HasValue).Select(a => (a as PdbCompAtom).ToIdealAtom()).ToDictionary(a => a.Id);
            var newBonds = structure.Bonds.Where(b => newAtoms.ContainsKey(b.A.Id) && newAtoms.ContainsKey(b.B.Id)).Select(b => Bond.Create(newAtoms[b.A.Id], newAtoms[b.B.Id], b.Type));
            var newStructure = Structure.Create(structure.Id, AtomCollection.Create(newAtoms.Values), BondCollection.Create(newBonds));
            return newStructure.AsPdbStructure(structure.PdbModifiedResidues());
        }

        /////// <summary>
        /////// All but the first model.
        /////// </summary>
        /////// <param name="structure"></param>
        /////// <returns></returns>
        ////public static ReadOnlyCollection<IStructure> PdbModels(this IStructure structure)
        ////{
        ////    return structure.GetProperty(PdbStructure.ModelsProperty);
        ////}
    }
}
