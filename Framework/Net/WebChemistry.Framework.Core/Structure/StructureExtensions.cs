namespace WebChemistry.Framework.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.Framework.Geometry;
    using WebChemistry.Framework.Math;

    /// <summary>
    /// Extensions for IStructure.
    /// </summary>
    public static class StructureExtensions
    {
        /// <summary>
        /// Returns a new structure without hydrogen atoms.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static IStructure WithoutHydrogens(this IStructure structure)
        {
            var newAtoms = structure.Atoms.Where(a => a.ElementSymbol != ElementSymbols.H).Select(a => a.Clone()).ToDictionary(a => a.Id);
            var newBonds = structure.Bonds.Where(b => newAtoms.ContainsKey(b.A.Id) && newAtoms.ContainsKey(b.B.Id)).Select(b => Bond.Create(newAtoms[b.A.Id], newAtoms[b.B.Id], b.Type));
            var newStructure = Structure.Create(structure.Id, AtomCollection.Create(newAtoms.Values), BondCollection.Create(newBonds));
            //newStructure.ClonePropertiesFrom(structure);
            if (structure.IsPdbStructure()) newStructure = newStructure.AsPdbStructure(structure.PdbModifiedResidues());
            return newStructure;
        }

        /// <summary>
        /// Created a substructure induced by the atoms. Does not clone structure properties.
        /// Conserves "incudeAtomOrder".
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="orderedAtomSet"></param>
        /// <param name="cloneAtoms"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static IStructure InducedSubstructure(this IStructure structure, string id, IEnumerable<IAtom> orderedAtomSet, bool cloneAtoms = true)
        {
            var oldAtoms = structure.Atoms;
            var _newAtoms = orderedAtomSet.Where(a => oldAtoms.Contains(a));
            if (cloneAtoms) _newAtoms = _newAtoms.Select(a => a.Clone());
            var orderedNew = _newAtoms.ToArray();
            var newAtoms = orderedNew.ToDictionary(a => a.Id);
            var newBonds = structure.Bonds.Where(b => newAtoms.ContainsKey(b.A.Id) && newAtoms.ContainsKey(b.B.Id)).Select(b => Bond.Create(newAtoms[b.A.Id], newAtoms[b.B.Id], b.Type));
            var ret = Structure.Create(id, AtomCollection.Create(orderedNew), BondCollection.Create(newBonds));

            if (structure.IsPdbStructure())
            {
                ret = ret.AsPdbStructure(structure.PdbModifiedResidues());
            }

            return ret;
        }

        /// <summary>
        /// If the structure is not a PDB structure, convert it to one.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="modifiedResidues"></param>
        /// <returns></returns>
        public static IStructure AsPdbStructure(this IStructure structure, IList<PdbResidue> modifiedResidues)
        {
            if (structure.IsPdbStructure()) return structure;
            return Pdb.PdbStructure.AsPdbStructure(structure, modifiedResidues);
        }

        /////// <summary>
        /////// Clone a PDB structure.
        /////// </summary>
        /////// <param name="structure"></param>
        /////// <returns></returns>
        ////public static IStructure ClonePdb(this IStructure structure)
        ////{
        ////    if (!structure.IsPdbStructure()) throw new ArgumentException("The argument must be a PDB structure");
        ////    return PdbStructure.ClonePdb(structure);
        ////}

        /// <summary>
        /// Returns the neighbors of a particular atom.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static IEnumerable<IAtom> GetAtomNeighbors(this IStructure s, IAtom atom)
        {
            return s.Bonds[atom].Select(b => b.B);
        }

        /// <summary>
        /// Expands the selection by n levels.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="levels"></param>
        public static void ExpandSelection(this IStructure s, int levels = 1)
        {
            for (int i = 0; i < levels; i++)
            {
                s.Atoms
                    .Where(a => a.IsSelected).ToArray()
                    .ForEach(a => s.Bonds[a].ForEach(b => b.B.IsSelected = true));
            }
        }

        /// <summary>
        /// Sets atom selection.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="isSelected"></param>
        public static void SelectAllAtoms(this IStructure s, bool isSelected = true)
        {
            s.Atoms.ForEach(a => a.IsSelected = isSelected);
        }
        
        /// <summary>
        /// Computes geometrical center of the structure.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static GeometricalCenterInfo GeometricalCenterAndRadius(this IStructure structure)
        {
            if (structure.Atoms.Count == 0) return new GeometricalCenterInfo();

            Vector3D cv = structure.Atoms.Aggregate<IAtom, Vector3D>(new Vector3D(), (c, a) => c + (Vector3D)a.Position);
            Vector3D center = Vector3D.Multiply(1.0 / structure.Atoms.Count, cv);
            double radius = System.Math.Sqrt(structure.Atoms.Max(a => a.Position.DistanceToSquared(center)));
            return new GeometricalCenterInfo(center, radius);
        }

        /// <summary>
        /// Makes the geometrical center the origin and returns the molecule radius.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns>Radius of the molecule and the coordinates of the center</returns>
        public static GeometricalCenterInfo ToCentroidCoordinates(this IStructure structure)
        {
            if (structure.Atoms.Count == 0) return new GeometricalCenterInfo();

            var c = structure.GeometricalCenterAndRadius();
            //structure.Translate(-1.0 * (Vector3D)c.Center);
            structure.TransformAtomPositions(a => a.Position - c.Center);

            return c;
        }

        /// <summary>
        /// Translates the structure by the given vector.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="by"></param>
        public static void TransformAtomPositions(this IStructure structure, Func<IAtom, Vector3D> newPosition)
        {
            foreach (Atom a in structure.Atoms)
            {
                a.Position = newPosition(a);
            }
        }

        private static readonly PropertyDescriptor<KDAtomTree> InvariantKdAtomTreeProperty
            = PropertyHelper.OfType<KDAtomTree>("InvariantKdAtomTreeProperty", category: "StructureGeometry", autoClone: false);
        /// <summary>
        /// 3D kD-Tree of atom centers. Lazy.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static KDAtomTree InvariantKdAtomTree(this IStructure structure)
        {
            var tree = structure.GetProperty(InvariantKdAtomTreeProperty, null);
            if (tree != null) return tree;
            
            tree = structure.Atoms.ToInvariantKDTree();
            structure.SetProperty(InvariantKdAtomTreeProperty, tree);
            return tree;
        }

        /// <summary>
        /// Checks if the tree was created.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static bool HasInvariantKdAtomTree(this IStructure structure)
        {
            return structure.GetProperty(InvariantKdAtomTreeProperty, null) != null;
        }

        private static readonly PropertyDescriptor<IList<IAtom>> ChiralAtomsProperty
            = PropertyHelper.OfType<IList<IAtom>>("ChiralAtoms", category: "StructureGeometry", autoClone: false);

        /// <summary>
        /// Get all chiral atoms.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>        
        public static IAtom[] ChiralAtoms(this IStructure structure)
        {
            var atoms = structure.GetProperty(ChiralAtomsProperty, null);
            if (atoms != null) return atoms.ToArray();

            atoms = ChiralityAnalyzer.GetChiralAtoms(structure);
            structure.SetProperty(ChiralAtomsProperty, atoms);
            return atoms.ToArray();
        }

        private static readonly PropertyDescriptor<RingCollection> RingsProperty
            = PropertyHelper.OfType<RingCollection>("Rings", category: "StructureGeometry", autoClone: false);

        /// <summary>
        /// Collection of rings of length 8 or lower.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static RingCollection Rings(this IStructure structure)
        {
            var rings = structure.GetProperty(RingsProperty, null);
            if (rings != null) return rings;

            rings = RingCollection.Create(structure);
            structure.SetProperty(RingsProperty, rings);
            return rings;
        }

        /////// <summary>
        /////// Write rings to a file.
        /////// </summary>
        /////// <param name="s"></param>
        /////// <param name="filename"></param>
        ////public static void WriteRings(this IStructure s, string filename)
        ////{
        ////    using (var f = File.CreateText(filename)) s.Rings().Write(f);
        ////}

        /////// <summary>
        /////// Read rings from a file.
        /////// </summary>
        /////// <param name="s"></param>
        /////// <param name="filename"></param>
        ////public static void ReadRings(this IStructure s, string filename)
        ////{
        ////    var rings = s.GetProperty(RingsProperty, null);
        ////    if (rings != null) return;

        ////    using (var f = File.OpenText(filename)) rings = RingCollection.Read(s, f);

        ////    s.SetProperty(RingsProperty, rings);
        ////}

        /// <summary>
        /// Write bonds to a file.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="filename"></param>
        public static void WriteBonds(this IStructure s, string filename)
        {
            using (var f = File.CreateText(filename)) ElementAndBondInfo.SerializeBonds(s, f);
        }

        /// <summary>
        /// Read bonds from a file.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="filename"></param>
        public static void ReadBonds(this IStructure s, string filename)
        {
            using (var f = File.OpenText(filename)) ElementAndBondInfo.ReadBonds(s, f);
        }
    }
}
