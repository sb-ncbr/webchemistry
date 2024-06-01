namespace WebChemistry.Framework.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Geometry;
    using WebChemistry.Framework.Math;

    public static class AtomEx
    {
        static readonly Random rnd = new Random();

        public static string PositionToString(this IAtom atom, string delim = " ")
        {
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            return string.Format("{1,8}{0}{2,8}{0}{3,8}", delim, atom.Position.X.ToString("0.0000", ci), atom.Position.Y.ToString("0.0000", ci), atom.Position.Z.ToString("0.0000", ci));
        }

        public static Vector3D GeometricalCenter(this IEnumerable<IAtom> atoms)
        {
            Vector3D cv = atoms.Aggregate<IAtom, Vector3D>(new Vector3D(), (c, a) => c + (Vector3D)a.Position);
            Vector3D center = (Vector3D)Vector3D.Multiply(1.0 / atoms.Count(), cv);
            return center;
        }
        
        public static IAtomCollection ToAtomCollection(this IEnumerable<IAtom> atoms)
        {
            return AtomCollection.Create(atoms);
        }

        public static double GetVdwRadius(this IAtom a)
        {
            return ElementAndBondInfo.GetElementInfo(a.ElementSymbol).VdwRadius;
        }

        public static ElementColor GetElementColor(this IAtom atom)
        {
            return ElementAndBondInfo.GetElementInfo(atom.ElementSymbol).Color;
        }

        /// <summary>
        /// Converts the atom collection to a KDTree.
        /// The atoms are first randomly shuffled.
        /// </summary>
        /// <param name="atoms"></param>
        /// <returns></returns>
        public static KDAtomTree ToInvariantKDTree(this IAtomCollection atoms)
        {
            return new KDAtomTree(atoms, keySelector: a => a.InvariantPosition, method: K3DPivotSelectionMethod.Average);
        }
    }
}