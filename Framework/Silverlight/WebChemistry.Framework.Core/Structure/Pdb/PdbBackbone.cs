namespace WebChemistry.Framework.Core.Pdb
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a C-alpha backbone of a protein.
    /// </summary>
    public class PdbBackbone
    {
        /// <summary>
        /// Empty backbone.
        /// </summary>
        public static readonly PdbBackbone Empty = new PdbBackbone { ProteinBackbone = Structure.Empty, DnaBackbone = Structure.Empty };

        /// <summary>
        /// The protein backbone.
        /// </summary>
        public IStructure ProteinBackbone { get; private set; }
        
        /// <summary>
        /// The DNA backbone.
        /// </summary>
        public IStructure DnaBackbone { get; private set; }


        /// <summary>
        /// Create the backbone.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static PdbBackbone Create(IStructure structure)
        {
            return new PdbBackbone { DnaBackbone = GetDnaBackbone(structure), ProteinBackbone = GetProteinBackbone(structure) };
        }

        static readonly string[] dnaBackboneArrangement = new string[] { "P", "O5'", "C5'", "C4'", "C3'", "O3'" };
        static readonly string[] proteinBackboneArrangement = new string[] { "N", "CA", "C" };
        
        public static bool IsBackboneBond(string[] arrangement, IAtom a, IAtom b)
        {
            int aIndex = Array.IndexOf(arrangement, a.PdbName());
            int bIndex = Array.IndexOf(arrangement, b.PdbName());

            int residueDistance = b.PdbResidueSequenceNumber() - a.PdbResidueSequenceNumber();

            switch (residueDistance)
            {
                case 0:
                    if (bIndex - aIndex == 1) return true;
                    break;

                case 1:
                    if (aIndex - bIndex == 2) return true;
                    break;

                default:
                    break;
            }

            return false;
        }

        static IStructure GetDnaBackbone(IStructure structure)
        {
            ElementSymbol c = ElementSymbols.C;
            ElementSymbol o = ElementSymbols.O;
            ElementSymbol p = ElementSymbols.P;
                        
            var atoms = structure
                .Atoms
                .Where(_a => 
                {
                    var a = _a as PdbAtom;
                    if (a == null) return false;

                    if (!a.RecordName.EqualOrdinal("ATOM")) return false;
                    if (a.ElementSymbol == c) return a.Name.EqualOrdinalIgnoreCase("C5'") || a.Name.EqualOrdinalIgnoreCase("C4'") || a.Name.EqualOrdinalIgnoreCase("C3'");
                    else if (a.ElementSymbol == p) return a.Name.EqualOrdinalIgnoreCase("P");
                    else if (a.ElementSymbol == o) return a.Name.EqualOrdinalIgnoreCase("O5'") || a.Name.EqualOrdinalIgnoreCase("O3'");
                    return false;
                })
                .ToList();

            var bonds = new List<IBond>(atoms.Count);
            int count = atoms.Count - 1;
            var arr = dnaBackboneArrangement;
            for (int i = 0; i < count; i++)
            {
                if (IsBackboneBond(arr, atoms[i], atoms[i + 1])) bonds.Add(Bond.Create(atoms[i], atoms[i + 1], BondType.Single));
            }

            return Structure.Create(structure.Id + "_DNA_Backbone", AtomCollection.Create(atoms), BondCollection.Create(bonds));
        }

        static IStructure GetProteinBackbone(IStructure structure)
        {
            ElementSymbol c = ElementSymbols.C;
            ElementSymbol n = ElementSymbols.N;
            
            var atoms = structure
                .Atoms
                .Where(_a =>
                {
                    var a = _a as PdbAtom;
                    if (a == null) return false;

                    if (!a.RecordName.EqualOrdinal("ATOM")) return false;
                    if (a.ElementSymbol == c) return a.Name.EqualOrdinalIgnoreCase("CA") || a.Name.EqualOrdinalIgnoreCase("C");
                    else if (a.ElementSymbol == n) return a.Name.EqualOrdinalIgnoreCase("N");
                    return false;
                })
                .ToList();

            var bonds = new List<IBond>(atoms.Count);
            int count = atoms.Count - 1;
            var arr = proteinBackboneArrangement;
            for (int i = 0; i < count; i++)
            {
                if (IsBackboneBond(arr, atoms[i], atoms[i + 1])) bonds.Add(Bond.Create(atoms[i], atoms[i + 1], BondType.Single));
            }

            return Structure.Create(structure.Id + "_Backbone", AtomCollection.Create(atoms), BondCollection.Create(bonds));
        }

        private PdbBackbone()
        {
        }
    }
}
